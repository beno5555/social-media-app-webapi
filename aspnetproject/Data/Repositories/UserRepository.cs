using System.Linq.Expressions;
using aspnetproject.Data.Repositories.Base;
using aspnetproject.Models;
using aspnetproject.ProjectConstants.Enums;
using Microsoft.EntityFrameworkCore;

namespace aspnetproject.Data.Repositories;

public class UserRepository : BaseEntityRepository<User>
{
    public UserRepository(ApplicationDbContext dbContext) : base(dbContext)
    {
        
    }
    public async Task<User?> GetByUniqueIdentifierAsync(string uniqueIdentifier)
    {
        return await _dbSet.FirstOrDefaultAsync(user => user.Email    == uniqueIdentifier ||
                                                        user.Username == uniqueIdentifier);
    }

    public async Task<bool> ExistsByUsernameAsync(string username)
    {
        return await ExistsAsync(user => user.Username == username);
    }

    public async Task<bool> ExistsByEmailAsync(string email)
    {
        return await ExistsAsync(user => user.Email == email);
    }
    
    public async Task<List<User>> GetUsersAsync(int excludedUserId, int? pageNumber, int? pageSize)
    {
        return await GetWhereAsync(user => user.Id != excludedUserId, pageNumber, pageSize);
    }

    public async Task<List<User>> SearchByUsernameAsync(string usernameInput, int? pageNumber, int? pageSize)
    {
        return await GetWhereAsync(user => user.Username.Contains(usernameInput), pageNumber, pageSize);
    }

    /// <summary>
    /// fetches friends with whom the user has conversations if shouldHaveConversation is true
    /// fetches friends with whom the user does not have a conversation if shouldHaveConversation is false
    /// </summary>
    public async Task<List<User>> GetFriendsByConversationStatusAsync(int userId, bool shouldHaveConversation, int? pageNumber, int? pageSize)
    {
        Expression<Func<User, bool>> areFriendsAndHaveConversationPredicate = user =>
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
            areFriendsAndHaveConversationPredicate,
            pageNumber,
            pageSize,
            latest
        );
    }

    public async Task UpdateBioAsync(User userToUpdate, string bio)
    {
        userToUpdate.Bio = bio;
        await _dbContext.SaveChangesAsync();
    }
}