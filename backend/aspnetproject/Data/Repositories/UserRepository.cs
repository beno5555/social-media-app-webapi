using System.Linq.Expressions;
using aspnetproject.Common.ProjectConstants.Enums;
using aspnetproject.Data.Models;
using aspnetproject.Data.Repositories.Base;
using aspnetproject.Data.Repositories.Dtos;
using aspnetproject.Infrastructure.Dtos.Users;
using aspnetproject.Infrastructure.Services.BackgroundJobs.Common;
using aspnetproject.Infrastructure.Services.BackgroundJobs.Configuration;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace aspnetproject.Data.Repositories;

public class UserRepository : BaseEntityRepository<User>
{
    public UserRepository(ApplicationDbContext dbContext) : base(dbContext)
    {

    }
    
    #region Business Logic calls

    public async Task<User> AddUserAsync(User user)
    {
        await AddAsync(user);
        return (await GetByUniqueIdentifierWithRolesAsync(user.Username))!;
    }

    public async Task<User?> GetUserByIdNoFilterAsync(int id)
    {
        return await GetSingleByIgnoringQueryFilterAsync(user => user.Id == id);
    }

    public async Task<User?> GetByUniqueIdentifierAsync(string uniqueIdentifier)
    {
        return await _dbSet
            .FirstOrDefaultAsync(user => user.Email    == uniqueIdentifier ||
                                         user.Username == uniqueIdentifier);
    }

    public async Task<User?> GetByUniqueIdentifierWithRolesAsync(string uniqueIdentifier)
    {
        return await IncludeRoles()
            .FirstOrDefaultAsync(user => user.Email    == uniqueIdentifier ||
                                         user.Username == uniqueIdentifier);
    }

    public async Task<User?> GetDeactivatedByIdAsync(int id)
    {
        return await GetDeactivatedAccountAsync(user => user.Id == id);
    }
    public async Task<User?> GetDeactivatedByEmailAsync(string email)
    {
        return await GetDeactivatedAccountAsync(user => user.Email == email);
    }
    public async Task<User?> GetDeactivatedAccountByActivationTokenHashAsync(string activationTokenHash)
    {
        return await GetDeactivatedAccountAsync(user => user.ResetTokenHash      == activationTokenHash &&
                                                        user.ResetTokenExpiresAt > DateTime.UtcNow);
    }

    private async Task<User?> GetDeactivatedAccountAsync(Expression<Func<User, bool>> predicate)
    {
        return await IncludeRoles()
            .IgnoreQueryFilters()
            .Where(user => user.AccountDeactivatedAt != null && user.AccountDeletedAt == null)
            .FirstOrDefaultAsync(predicate);
    }
    
    private IQueryable<User> IncludeRoles()
    {
        return _dbSet.Include(user => user.UserRoles).ThenInclude(userRole => userRole.Role);
    }
    
    public async Task<User?> GetUserByIdAsync(int id)
    {
        return await IncludeRoles()
            .FirstOrDefaultAsync(user => user.Id == id);
    }
    
    public async Task<User?> GetUserByEmailAsync(string email)
    {
        return await GetSingleByIgnoringQueryFilterAsync(user => user.Email == email);
    }

    public async Task<User?> GetByPasswordResetTokenHash(string passwordResetTokenHash)
    {
        return await _dbSet.FirstOrDefaultAsync(user => user.ResetTokenHash == passwordResetTokenHash &&
                                                        user.ResetTokenExpiresAt > DateTime.UtcNow);
    }

    public async Task<bool> ExistsByUsernameAsync(string username)
    {
        return await ExistsByIgnoringQueryFilterAsync(user => user.Username == username.ToLower());
    }

    public async Task<bool> OtherUserByUsernameExistsAsync(string username, int currentUserId)
    {
        return await ExistsByIgnoringQueryFilterAsync(user => user.Username == username.ToLower() && user.Id != currentUserId);
    }
    public async Task<List<User>> SearchByUsernameAsync(string usernameInput, int? pageNumber, int? pageSize)
    {
        return await GetWhereAsync(user => user.Username.Contains(usernameInput), pageNumber, pageSize);
    }

    // ignores query filters automatically which is fine for now. we want to fetch conversations with deactivated/soft deleted users
    public async Task<List<ConversationFriendProjection>> GetConversationFriendsAsync(int userId, int? pageNumber, int? pageSize)
    {
        const string sql = """
           SELECT 
                u.Id AS FriendId,
                u.Username AS FriendUsername,
                u.LastOnlineAt AS FriendLastActiveAt,
                CAST(CASE WHEN u.AccountDeletedAt IS NOT NULL THEN 1 ELSE 0 END AS bit) AS IsDeleted,
                CAST(CASE WHEN u.AccountDeactivatedAt IS NOT NULL THEN 1 ELSE 0 END AS bit) AS IsDeactivated,
                lm.MessageContent AS LastMessageContent,
                lm.CreatedAt AS LastMessageSentAt,
                lm.SenderUserId AS LastMessageSenderId,
                (SELECT COUNT(*) FROM Messages um
                WHERE um.SenderUserId = u.Id AND um.ReceiverUserId = @userId AND um.Seen = 0) AS UnreadCount
           FROM Users u
           JOIN Friendships f
                ON (f.AddresseeUserId = @userId AND f.RequesterUserId = u.Id)
                OR (f.RequesterUserId = @userId AND f.AddresseeUserId = u.Id)
           CROSS APPLY (
                SELECT TOP 1 m.MessageContent, m.CreatedAt, m.SenderUserId
                FROM Messages m
                WHERE (m.SenderUserId = @userId AND m.ReceiverUserId = u.Id)
                    OR (m.ReceiverUserId = @userId AND m.SenderUserId = u.Id)
                ORDER BY m.CreatedAt DESC
           ) lm
           WHERE f.FriendshipStatus = 'Accepted'
           """;

        var query = _dbContext.Database
            .SqlQueryRaw<ConversationFriendProjection>(sql, new SqlParameter("@userId", userId))
            .OrderByDescending(friendProj => friendProj.LastMessageSentAt);

        if (pageNumber.HasValue && pageSize.HasValue)
        {
            var list = await query
                .Skip((pageNumber.Value - 1) * pageSize.Value)
                .Take(pageSize.Value)
                .ToListAsync();

            return list;
        }

        return await query.ToListAsync();
    }

    /// <summary>
    /// fetches friends with whom the user does not have a conversation
    /// </summary>
    public async Task<List<User>> GetNonConversationFriendsAsync(int userId, int? pageNumber, int? pageSize)
    {
        // areFriends && haveNoConversation
        Expression<Func<User, bool>> shouldFetch = user =>
            _dbContext.Friendships.Any(friendship =>
                (friendship.AddresseeUserId == userId && friendship.RequesterUserId == user.Id) ||
                (friendship.AddresseeUserId == user.Id && friendship.RequesterUserId == userId) &&
                friendship.FriendshipStatus == FriendshipStatus.Accepted)
            &&
            !_dbContext.Messages.Any(message =>
                (message.SenderUserId == userId  && message.ReceiverUserId == user.Id) ||
                (message.SenderUserId == user.Id && message.ReceiverUserId == userId));
        
        return await GetWhereAsync(
            shouldFetch,
            pageNumber,
            pageSize
        );
    }

    public async Task EditUserProfileAsync(User userToUpdate, EditUserDto editUserDto)
    {
        var lastUpdatedAt = DateTime.UtcNow;
        userToUpdate.Bio = editUserDto.Bio;
        userToUpdate.DateOfBirth = editUserDto.DateOfBirth;

        userToUpdate.LastUpdatedAt = lastUpdatedAt;
        if (!string.Equals(userToUpdate.Username, editUserDto.Username, StringComparison.OrdinalIgnoreCase))
        {
            userToUpdate.UsernameLastChangedAt = lastUpdatedAt;
        }
        
        userToUpdate.Username = editUserDto.Username.ToLower();
        
        await _dbContext.SaveChangesAsync();
    }

    public async Task MarkActiveAsync(int userId)
    {
        await _dbSet.Where(user => user.Id == userId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(user => user.LastOnlineAt, (DateTime?)null));
    }

    public async Task MarkUserOfflineAsync(int userId, DateTime lastActiveAt)
    {
        await _dbSet.Where(user => user.Id == userId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(user => user.LastOnlineAt, lastActiveAt)); 
    }

    public void UpdatePassword(User user, string hash, string salt)
    {
        user.PasswordHash = hash;
        user.PasswordSalt = salt;

        user.ResetTokenHash = null;
        user.ResetTokenExpiresAt = null;

        user.LastUpdatedAt = DateTime.UtcNow;
    }
    
    public async Task ActivateAccountAsync(User user)
    {
        user.AccountDeactivatedAt = null;
        user.AccountDeletedAt = null;
        user.ResetTokenExpiresAt = null;
        user.ResetTokenHash = null;
        user.LastUpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();
    }
    
    #endregion
    
    #region Background job calls

    public async Task<SoftDeleteCleanupResult> DeleteSoftDeletedUsersBatchAsync(DateTime cutoff, SoftDeletedUsersCleanupConfiguration config, CancellationToken cancellationToken)
    {
        var result = new SoftDeleteCleanupResult();
        
        var userIds = await _dbSet
            .IgnoreQueryFilters()
            .Where(user => user.AccountDeletedAt != null && user.AccountDeletedAt < cutoff)
            .Take(config.BatchSize)
            .Select(user => user.Id)
            .ToListAsync(cancellationToken);

        if (userIds.Count > 0)
        {
            result.MessagesDeletedCount += await DeleteUsersMessagesAsync(userIds, config.NestedBatchSize, cancellationToken);
            result.FriendshipsDeletedCount += await DeleteUsersFriendshipsAsync(userIds, config.NestedBatchSize, cancellationToken);
            result.PostsDeletedCount += await DeleteUsersPostsAsync(userIds, config.NestedBatchSize, cancellationToken);
            
            result.UsersDeletedCount = await _dbSet
                .IgnoreQueryFilters()
                .Where(user => userIds.Contains(user.Id))
                .ExecuteDeleteAsync(cancellationToken);
        }
        
        return result;
    }

    private async Task<int> DeleteUsersMessagesAsync(List<int> userIds, int batchSize, CancellationToken cancellationToken)
    {
        int totalDeleted = 0;
        int deletedThisBatch = 0;

        do
        {
            var messageIds = await _dbContext.Messages
                .Where(message => userIds.Contains(message.ReceiverUserId) || userIds.Contains(message.SenderUserId))
                .OrderBy(message => message.Id)
                .Take(batchSize)
                .Select(message => message.Id)
                .ToListAsync(cancellationToken);

            deletedThisBatch = await _dbContext.Messages
                .Where(message => messageIds.Contains(message.Id))
                .ExecuteDeleteAsync(cancellationToken);

            totalDeleted += deletedThisBatch;
        } while (deletedThisBatch > 0);

        return totalDeleted;
    }
    
    private async Task<int> DeleteUsersPostsAsync(List<int> userIds, int batchSize, CancellationToken cancellationToken)
    {
        int totalDeleted     = 0;
        int deletedThisBatch = 0;

        do
        {
            var postIds = await _dbContext.Posts
                .Where(post => userIds.Contains(post.UserId))
                .OrderBy(post => post.Id)
                .Take(batchSize)
                .Select(post => post.Id)
                .ToListAsync(cancellationToken);

            deletedThisBatch = await _dbContext.Posts
                .Where(post => postIds.Contains(post.Id))
                .ExecuteDeleteAsync(cancellationToken);

            totalDeleted += deletedThisBatch;
        } while (deletedThisBatch > 0);

        return totalDeleted;
    }
    
    private async Task<int> DeleteUsersFriendshipsAsync(List<int> userIds, int batchSize, CancellationToken cancellationToken)
    {
        int totalDeleted     = 0;
        int deletedThisBatch = 0;

        do
        {
            var postIds = await _dbContext.Friendships
                .Where(friendship => userIds.Contains(friendship.AddresseeUserId) || userIds.Contains(friendship.RequesterUserId))
                .OrderBy(friendship => friendship.Id)
                .Take(batchSize)
                .Select(friendship => friendship.Id)
                .ToListAsync(cancellationToken);

            deletedThisBatch = await _dbContext.Friendships
                .Where(friendship => postIds.Contains(friendship.Id))
                .ExecuteDeleteAsync(cancellationToken);

            totalDeleted += deletedThisBatch;
        } while (deletedThisBatch > 0);

        return totalDeleted;
    }
    
    #endregion
}