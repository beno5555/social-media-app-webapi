using aspnetproject.Data.Models;
using aspnetproject.Data.Repositories.Base;
using Microsoft.EntityFrameworkCore;

namespace aspnetproject.Data.Repositories;

public class PostLikeRepository : BaseEntityRepository<PostLike>
{
    public PostLikeRepository(ApplicationDbContext dbContext) : base(dbContext)
    {

    }

    public async Task<PostLike> AddLikeAsync(PostLike like)
    {
        await AddAsync(like);
        return (await GetFirstAsync(likeFromDb => likeFromDb.Id == like.Id))!;
    }

    public async Task<bool> HasUserLikedPostAsync(int postId, int userId)
    {
        return await ExistsAsync(like => like.PostId == postId && like.UserId == userId);
    }

    public async Task<int> CountByPostIdAsync(int postId)
    {
        return await _dbSet.CountAsync(like => like.PostId == postId);
    }

    // Returns the number of likes removed: 1, or 0 when the user had not liked the post.
    public async Task<int> DeleteLikeAsync(int postId, int userId)
    {
        return await _dbSet
            .Where(like => like.PostId == postId && like.UserId == userId)
            .ExecuteDeleteAsync();
    }

    // Newest first. Id breaks ties between likes written in the same millisecond, so paging is deterministic.
    public async Task<List<PostLike>> GetByPostIdAsync(int postId, int? pageNumber, int? pageSize)
    {
        return await GetWhereAsync(like => like.PostId == postId, pageNumber, pageSize,
            query => query
                .OrderByDescending(like => like.CreatedAt)
                .ThenByDescending(like => like.Id));
    }

    protected override IQueryable<PostLike> Query(bool track = true)
    {
        var query = _dbSet
            .Include(like => like.User);

        return track ? query : query.AsNoTracking();
    }
}
