using System.Linq.Expressions;
using aspnetproject.Data.Models;
using aspnetproject.Data.Repositories.Base;
using aspnetproject.Data.Repositories.Dtos;
using Microsoft.EntityFrameworkCore;

namespace aspnetproject.Data.Repositories;

public class PostRepository : BaseEntityRepository<Post>
{
    public PostRepository(ApplicationDbContext dbContext) : base(dbContext)
    {
        
    }

    public async Task<Post> AddPostAsync(Post post)
    {
        await AddAsync(post);
        return (await _dbSet
            .Include(postFromDb => postFromDb.User)
            .Include(postFromDb => postFromDb.Images)
            .FirstOrDefaultAsync(postFromDb => postFromDb.Id == post.Id))!;
    }

    // The post is tracked, so a caller may change it and save.
    public async Task<PostWithStatsProjection?> GetPostByIdAsync(int id, int? callerId)
    {
        return await _dbSet.Include(post => post.User)
            .Include(post => post.Images)
            .Include(post => post.Comments)
                .ThenInclude(comment => comment.CommenterUser)
            .AsSplitQuery()
            .Where(post => post.Id == id)
            .Select(WithStats(callerId))
            .FirstOrDefaultAsync();
    }

    public async Task<List<PostWithStatsProjection>> GetByUserIdAsync(int userId, int? callerId, int? pageNumber = null, int? pageSize = null)
    {
        return await BuildWhereQuery(p => p.UserId == userId, pageNumber, pageSize)
            .Select(WithStats(callerId))
            .ToListAsync();
    }

    /// <summary>
    /// fetches the latest posts of the users that are included in the integer list containing userIds.
    /// </summary>
    public async Task<List<PostWithStatsProjection>> GetFeedAsync(List<int> friendIds, int callerId, int? pageNumber, int? pageSize)
    {
        return await BuildWhereQuery(post => friendIds.Contains(post.UserId), pageNumber, pageSize,
                query => query.OrderByDescending(post => post.CreatedAt))
            .Select(WithStats(callerId))
            .ToListAsync();
    }

    protected override IQueryable<Post> Query(bool track = true)
    {
        var query = _dbSet
            .Include(post => post.User)
            .Include(post => post.Images);

        return track ? query : query.AsNoTracking();
    }

    // Counted in SQL as subqueries of the post query, so a page of posts costs one query, not one per post.
    // A guest (no caller id) never liked anything.
    private Expression<Func<Post, PostWithStatsProjection>> WithStats(int? callerId)
    {
        return post => new PostWithStatsProjection
        {
            Post          = post,
            LikeCount     = post.Likes.Count(),
            CommentCount  = post.Comments.Count(),
            LikedByCaller = callerId != null && post.Likes.Any(like => like.UserId == callerId)
        };
    }
}
