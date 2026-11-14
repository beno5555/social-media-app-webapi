using System.Linq.Expressions;
using aspnetproject.Common.ProjectConstants;
using aspnetproject.Common.ProjectConstants.Enums;
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
        // Newest first. Id breaks ties between posts written in the same millisecond, so paging is deterministic.
        return await BuildWhereQuery(p => p.UserId == userId, pageNumber, pageSize,
                query => query.OrderByDescending(post => post.CreatedAt).ThenByDescending(post => post.Id))
            .Select(WithStats(callerId))
            .ToListAsync();
    }

    /// <summary>
    /// Ranks every post the caller did not write by <see cref="FeedScore"/>, highest first, and pages the result,
    /// all in one query. Newest id breaks ties, so equal scores keep the same order on every request.
    /// </summary>
    public async Task<List<PostWithStatsProjection>> GetFeedAsync(int callerId, int? pageNumber, int? pageSize)
    {
        // post.User is null when the global filter hides the author, so deactivated and soft-deleted authors drop out.
        Expression<Func<Post, bool>> isCandidate = post => post.UserId != callerId && post.User != null;
        var score = FeedScore(callerId, DateTime.UtcNow);

        return await BuildWhereQuery(isCandidate, pageNumber, pageSize,
                query => query.OrderByDescending(score).ThenByDescending(post => post.Id))
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

    // The formula and its weights are described next to the constants. Every part translates to SQL:
    // the connection tier and the counts become subqueries, and the age comes from DATEDIFF against now.
    private Expression<Func<Post, double>> FeedScore(int callerId, DateTime now)
    {
        var callerFriendIds = _dbContext.Friendships
            .Where(friendship =>
                friendship.FriendshipStatus == FriendshipStatus.Accepted &&
                (friendship.RequesterUserId == callerId || friendship.AddresseeUserId == callerId))
            .Select(friendship =>
                friendship.RequesterUserId == callerId ? friendship.AddresseeUserId : friendship.RequesterUserId);

        return post =>
            ((callerFriendIds.Contains(post.UserId)
                    ? Constants.FeedFriendWeight
                    // The author has an accepted friend who is also one of the caller's friends.
                    : _dbContext.Friendships.Any(friendship =>
                        friendship.FriendshipStatus == FriendshipStatus.Accepted &&
                        ((friendship.RequesterUserId == post.UserId && callerFriendIds.Contains(friendship.AddresseeUserId)) ||
                         (friendship.AddresseeUserId == post.UserId && callerFriendIds.Contains(friendship.RequesterUserId))))
                        ? Constants.FeedMutualFriendWeight
                        : Constants.FeedOtherUserWeight)
                + post.Likes.Count()
                + post.Comments.Count())
            / Math.Pow(
                // A post dated after now counts as brand new, so the base of the power never goes below the offset.
                (post.CreatedAt > now ? 0 : EF.Functions.DateDiffMinute(post.CreatedAt, now)) / Constants.MinutesPerHour
                    + Constants.FeedAgeOffsetHours,
                Constants.FeedAgeGravity);
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
