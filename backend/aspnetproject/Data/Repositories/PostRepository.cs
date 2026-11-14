using System.Data;
using System.Linq.Expressions;
using aspnetproject.Common.ProjectConstants;
using aspnetproject.Common.ProjectConstants.Enums;
using aspnetproject.Data.Models;
using aspnetproject.Data.Repositories.Base;
using aspnetproject.Data.Repositories.Dtos;
using aspnetproject.Data.Repositories.Visibility;
using Microsoft.Data.SqlClient;
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

    // The post is tracked, so a caller may change it and save. Only the comments the caller may read are loaded.
    // callerId is null for a guest.
    public async Task<PostWithStatsProjection?> GetPostByIdAsync(int id, int? callerId, bool isAdmin)
    {
        var commentReadableFilter = ModerationVisibility.CommentReadableBy(callerId, isAdmin);

        return await _dbSet.Include(post => post.User)
            .Include(post => post.Images)
            .Include(post => post.Comments.AsQueryable().Where(commentReadableFilter))
                .ThenInclude(comment => comment.CommenterUser)
            .AsSplitQuery()
            .Where(post => post.Id == id)
            .Where(ModerationVisibility.PostReadableBy(callerId, isAdmin))
            .Select(WithStats(callerId, isAdmin))
            .FirstOrDefaultAsync();
    }

    // The post alone, tracked, if it exists and the caller may read it.
    public async Task<Post?> GetReadablePostAsync(int id, int? callerId, bool isAdmin)
    {
        return await _dbSet
            .Where(post => post.Id == id)
            .Where(ModerationVisibility.PostReadableBy(callerId, isAdmin))
            .FirstOrDefaultAsync();
    }

    public async Task<bool> IsReadableAsync(int id, int? callerId, bool isAdmin)
    {
        return await _dbSet
            .Where(post => post.Id == id)
            .AnyAsync(ModerationVisibility.PostReadableBy(callerId, isAdmin));
    }

    public async Task<List<PostWithStatsProjection>> GetByUserIdAsync(int userId, int? callerId, bool isAdmin, int? pageNumber = null, int? pageSize = null)
    {
        // Newest first. Id breaks ties between posts written in the same millisecond, so paging is deterministic.
        var query = Query()
            .Where(post => post.UserId == userId)
            .Where(ModerationVisibility.PostReadableBy(callerId, isAdmin))
            .OrderByDescending(post => post.CreatedAt)
            .ThenByDescending(post => post.Id);

        return await Page(query, pageNumber, pageSize)
            .Select(WithStats(callerId, isAdmin))
            .ToListAsync();
    }

    /// <summary>
    /// Ranks every post the caller did not write by the <see cref="FeedRanking"/> score, highest first.
    /// One query ranks and pages, returning only the page's ids; a second loads those posts with their stats.
    /// Candidates are the posts the caller may read, so an administrator's feed includes hidden posts.
    /// </summary>
    public async Task<List<PostWithStatsProjection>> GetFeedAsync(int callerId, bool isAdmin, int pageNumber, int pageSize)
    {
        var pageIds = await _dbContext.Database
            .SqlQueryRaw<int>(RankedFeedPageSql, RankedFeedPageParameters(callerId, isAdmin, pageNumber, pageSize))
            .ToListAsync();

        var posts = await Query(track: false)
            .Where(post => pageIds.Contains(post.Id))
            .Select(WithStats(callerId, isAdmin))
            .ToListAsync();

        // The second query returns the posts in no set order, so the ranked order of the ids is put back.
        return posts.OrderBy(post => pageIds.IndexOf(post.Post.Id)).ToList();
    }

    protected override IQueryable<Post> Query(bool track = true)
    {
        var query = _dbSet
            .Include(post => post.User)
            .Include(post => post.Images);

        return track ? query : query.AsNoTracking();
    }

    // The score formula is described on FeedRanking. Each CTE names one input of it, and the final SELECT
    // combines them. Raw SQL skips the global User filter, so the author's account state is checked by hand.
    // The moderation rule of ModerationVisibility is repeated by hand: a hidden post is a candidate only for an
    // administrator (the author never gets their own posts), and a hidden comment adds activity only for a caller who may read it.
    // A shared friend counts even if their own account is deactivated or soft-deleted.
    // SQL Server cannot wrap a CTE in a subquery, so EF cannot compose this query: it pages itself.
    private const string RankedFeedPageSql = """
        WITH CallerFriends AS (
            SELECT CASE WHEN f.RequesterUserId = @callerId THEN f.AddresseeUserId ELSE f.RequesterUserId END AS UserId
            FROM Friendships f
            WHERE f.FriendshipStatus = @accepted
              AND @callerId IN (f.RequesterUserId, f.AddresseeUserId)
        ),
        FriendsOfCallerFriends AS (
            SELECT CASE WHEN f.RequesterUserId = cf.UserId THEN f.AddresseeUserId ELSE f.RequesterUserId END AS UserId
            FROM Friendships f
            JOIN CallerFriends cf ON cf.UserId IN (f.RequesterUserId, f.AddresseeUserId)
            WHERE f.FriendshipStatus = @accepted
        ),
        CandidatePosts AS (
            SELECT
                p.Id,
                CASE
                    WHEN p.UserId IN (SELECT UserId FROM CallerFriends)          THEN @friendWeight
                    WHEN p.UserId IN (SELECT UserId FROM FriendsOfCallerFriends) THEN @mutualFriendWeight
                    ELSE @otherUserWeight
                END AS ConnectionWeight,
                (SELECT COUNT(*) FROM PostLikes l WHERE l.PostId = p.Id)
                    + (SELECT COUNT(*) FROM Comments c
                       WHERE c.PostId = p.Id
                         AND (c.ModerationStatus <> @hidden OR @isAdmin = 1 OR c.CommenterUserId = @callerId)) AS Activity,
                CASE
                    WHEN p.CreatedAt > @now THEN 0
                    ELSE DATEDIFF(MINUTE, p.CreatedAt, @now) / @minutesPerHour
                END AS AgeHours
            FROM Posts p
            JOIN Users author ON author.Id = p.UserId
            WHERE p.UserId <> @callerId
              AND (p.ModerationStatus <> @hidden OR @isAdmin = 1)
              AND author.AccountDeactivatedAt IS NULL
              AND author.AccountDeletedAt IS NULL
        )
        SELECT Id AS Value
        FROM CandidatePosts
        ORDER BY (ConnectionWeight + Activity) / POWER(AgeHours + @ageOffsetHours, @ageGravity) DESC, Id DESC
        OFFSET @skip ROWS FETCH NEXT @take ROWS ONLY
        """;

    private SqlParameter[] RankedFeedPageParameters(int callerId, bool isAdmin, int pageNumber, int pageSize)
    {
        return
        [
            new SqlParameter("@callerId", callerId),
            new SqlParameter("@accepted", nameof(FriendshipStatus.Accepted)),
            new SqlParameter("@hidden", nameof(ModerationStatus.Hidden)),
            new SqlParameter("@isAdmin", isAdmin),
            new SqlParameter("@friendWeight", FeedRanking.FriendWeight),
            new SqlParameter("@mutualFriendWeight", FeedRanking.MutualFriendWeight),
            new SqlParameter("@otherUserWeight", FeedRanking.OtherUserWeight),
            new SqlParameter("@ageOffsetHours", FeedRanking.AgeOffsetHours),
            new SqlParameter("@ageGravity", FeedRanking.AgeGravity),
            new SqlParameter("@minutesPerHour", Constants.MinutesPerHour),
            new SqlParameter("@now", SqlDbType.DateTime2) { Value = DateTime.UtcNow },
            new SqlParameter("@skip", (pageNumber - Constants.FirstPageNumber) * pageSize),
            new SqlParameter("@take", pageSize)
        ];
    }

    // Counted in SQL as subqueries of the post query, so a page of posts costs one query, not one per post.
    // A guest (no caller id) never liked anything. Only the comments the caller may read are counted.
    private Expression<Func<Post, PostWithStatsProjection>> WithStats(int? callerId, bool isAdmin)
    {
        var commentReadableFilter = ModerationVisibility.CommentReadableBy(callerId, isAdmin);

        return post => new PostWithStatsProjection
        {
            Post          = post,
            LikeCount     = post.Likes.Count(),
            CommentCount  = post.Comments.AsQueryable().Count(commentReadableFilter),
            LikedByCaller = callerId != null && post.Likes.Any(like => like.UserId == callerId)
        };
    }
}
