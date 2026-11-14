using aspnetproject.Common.ProjectConstants.Enums;
using aspnetproject.Data.Models;
using aspnetproject.Data.Repositories.Base;
using aspnetproject.Data.Repositories.Visibility;
using Microsoft.EntityFrameworkCore;

namespace aspnetproject.Data.Repositories;

// Every read takes the caller, so a comment the caller may not read (or one on a post they may not read) is never returned.
public class CommentRepository : BaseEntityRepository<Comment>
{
    public CommentRepository(ApplicationDbContext dbContext) : base(dbContext)
    {

    }

    public async Task<Comment> AddCommentAsync(Comment comment)
    {
        await AddAsync(comment);
        return (await GetFirstAsync(commentFromDb => commentFromDb.Id == comment.Id))!;
    }

    // callerId is null for a guest.
    public async Task<Comment?> GetCommentByIdAsync(int id, int? callerId, bool isAdmin)
    {
        return await Query()
            .Where(comment => comment.Id == id)
            .Where(ModerationVisibility.CommentAndPostReadableBy(callerId, isAdmin))
            .FirstOrDefaultAsync();
    }

    public async Task<Comment?> GetCommentWithCommenterUserAndPostById(int id, int callerId, bool isAdmin)
    {
        return await _dbSet
            .Include(comment => comment.CommenterUser)
            .Include(comment => comment.Post)
            .Where(comment => comment.Id == id)
            .Where(ModerationVisibility.CommentAndPostReadableBy(callerId, isAdmin))
            .FirstOrDefaultAsync();
    }

    // The caller must already be allowed to read the post; this filters only the comments themselves.
    public async Task<List<Comment>> GetByPostIdAsync(int postId, int? callerId, bool isAdmin, int? pageNumber, int? pageSize)
    {
        var query = Query()
            .Where(comment => comment.PostId == postId)
            .Where(ModerationVisibility.CommentReadableBy(callerId, isAdmin));

        return await Page(query, pageNumber, pageSize).ToListAsync();
    }

    // The caller's own comments, so only a hidden post can take one away.
    public async Task<List<Comment>> GetByUserIdAsync(int userId, bool isAdmin, int? pageNumber, int? pageSize)
    {
        var query = Query()
            .Where(comment => comment.CommenterUserId == userId)
            .Where(ModerationVisibility.CommentAndPostReadableBy(userId, isAdmin));

        return await Page(query, pageNumber, pageSize).ToListAsync();
    }

    // The moderation queue: every flagged comment with its post, oldest first.
    // Id breaks ties between comments written in the same millisecond.
    public async Task<List<Comment>> GetFlaggedAsync(int pageNumber, int pageSize)
    {
        var query = Query(track: false)
            .Include(comment => comment.Post)
            .Where(comment => comment.ModerationStatus == ModerationStatus.Flagged)
            .OrderBy(comment => comment.CreatedAt)
            .ThenBy(comment => comment.Id);

        return await Page(query, pageNumber, pageSize).ToListAsync();
    }

    public async Task DeletePostCommentsAsync(int postId)
    {
        await DeleteWhereAsync(comment => comment.PostId == postId);
    }

    protected override IQueryable<Comment> Query(bool track = true)
    {
        var query = _dbSet
            .Include(comment => comment.CommenterUser);

        return track ? query : query.AsNoTracking();
    }
}
