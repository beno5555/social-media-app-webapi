using System.Linq.Expressions;
using aspnetproject.Common.ProjectConstants.Enums;
using aspnetproject.Data.Models;
using aspnetproject.Data.Repositories.Base;
using aspnetproject.Data.Repositories.Dtos;
using aspnetproject.Infrastructure.Dtos.Users;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace aspnetproject.Data.Repositories;

public class UserRepository : BaseEntityRepository<User>
{
    public UserRepository(ApplicationDbContext dbContext) : base(dbContext)
    {

    }

    public async Task<User?> GetUserByIdAsyncNoFilter(int id)
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
        return await _dbSet
            .Include(user => user.UserRoles)
            .ThenInclude(userRole => userRole.Role)
            .FirstOrDefaultAsync(user => user.Email    == uniqueIdentifier ||
                                         user.Username == uniqueIdentifier);
    }

    public async Task<User?> GetDeactivatedByIdAsync(int id)
    {
        return await GetSingleByIgnoringQueryFilterAsync(user => user.Id                   == id &&
                                                                 user.AccountDeactivatedAt != null);
    }
    public async Task<User?> GetDeactivatedByEmailAsync(string email)
    {
        return await GetSingleByIgnoringQueryFilterAsync(user => user.Email                == email &&
                                                                 user.AccountDeactivatedAt != null);
    }
    public async Task<User?> GetDeactivatedAccountByActivationTokenHashAsync(string activationTokenHash)
    {
        return await GetSingleByIgnoringQueryFilterAsync(user => user.ResetTokenHash       == activationTokenHash &&
                                                                 user.ResetTokenExpiresAt  > DateTime.UtcNow      &&
                                                                 user.AccountDeactivatedAt != null);
    }
    
    public async Task<User?> GetUserByIdAsync(int id)
    {
        return await _dbSet
            .Include(user => user.UserRoles)
                .ThenInclude(userRole => userRole.Role)
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
    /// fetches friends with whom the user has or does not have a conversation, based on shouldHaveConversation parameter
    /// </summary>
    public async Task<List<User>> GetFriendsByConversationStatusAsync(int userId, bool shouldHaveConversation, int? pageNumber, int? pageSize)
    {
        Expression<Func<User, bool>> areFriendsAndHaveConversation = user =>
            _dbContext.Friendships.Any(friendship =>
                ((friendship.AddresseeUserId == userId  && friendship.RequesterUserId == user.Id) ||
                 (friendship.AddresseeUserId == user.Id && friendship.RequesterUserId == userId)) &&
                friendship.FriendshipStatus == FriendshipStatus.Accepted)
            &&
            shouldHaveConversation == _dbContext.Messages.Any(message =>
                (message.SenderUserId == userId  && message.ReceiverUserId == user.Id) ||
                (message.SenderUserId == user.Id && message.ReceiverUserId == userId));

        Func<IQueryable<User>, IOrderedQueryable<User>>? latest = shouldHaveConversation
            ? query => query.OrderByDescending(u =>
                _dbContext.Messages
                    .Where(m =>
                        (m.SenderUserId == userId && m.ReceiverUserId == u.Id) ||
                        (m.SenderUserId == u.Id   && m.ReceiverUserId == userId))
                    .Max(m => m.CreatedAt)) // last messaged sent in each conversation
            : null;

        return await GetWhereAsync(
            areFriendsAndHaveConversation,
            pageNumber,
            pageSize,
            latest
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

    public async Task<int> DeleteSoftDeletedUsersAsync(DateTime cutoff, int batchSize, CancellationToken cancellationToken)
    {
        int deletedCount = 0;
        var userIds = await _dbSet
            .IgnoreQueryFilters()
            .Where(user => user.AccountDeletedAt != null && user.AccountDeletedAt < cutoff)
            .Take(batchSize)
            .Select(user => user.Id)
            .ToListAsync(cancellationToken);

        if (userIds.Count > 0)
        {
            deletedCount = await _dbSet
                .IgnoreQueryFilters()
                .Where(user => userIds.Contains(user.Id))
                .ExecuteDeleteAsync(cancellationToken);
        }
        
        return deletedCount;
    }
}