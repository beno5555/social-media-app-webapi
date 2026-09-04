using aspnetproject.Data.Models;
using aspnetproject.Data.Repositories.Base;
using Microsoft.EntityFrameworkCore;

namespace aspnetproject.Data.Repositories;

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

    public async Task<Comment?> GetCommentByIdAsync(int id)
    {
        return await GetFirstAsync(comment => comment.Id == id);
    }
    public async Task<Comment?> GetCommentWithCommenterUserAndPostById(int id)
    {
        return await _dbSet
            .Include(comment => comment.CommenterUser)
            .Include(comment => comment.Post)
            .FirstOrDefaultAsync(comment => comment.Id == id);
    }

    public async Task<List<Comment>> GetByPostIdAsync(int postId, int? pageNumber, int? pageSize)
    {
        return await GetWhereAsync(comment => comment.PostId == postId, pageNumber, pageSize);
    }
    public async Task<List<Comment>> GetByUserIdAsync(int userId, int? pageNumber, int? pageSize)
    {
        return await GetWhereAsync(comment => comment.CommenterUserId == userId, pageNumber, pageSize);
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