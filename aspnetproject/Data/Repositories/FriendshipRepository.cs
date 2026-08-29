using aspnetproject.Common.ProjectConstants.Enums;
using aspnetproject.Data.Models;
using aspnetproject.Data.Repositories.Base;
using Microsoft.EntityFrameworkCore;

namespace aspnetproject.Data.Repositories;

public class FriendshipRepository : BaseRepository<Friendship>
{
    public FriendshipRepository(ApplicationDbContext dbContext) : base(dbContext)
    {
        
    }

    protected override IQueryable<Friendship> Query(bool track = true)
    {
        var query = _dbSet
            .Include(friendShip => friendShip.RequesterUser)
            .Include(friendship => friendship.AddresseeUser);

        return track ? query : query.AsNoTracking();
    }

    public async Task<List<Friendship>> GetPendingRequestsAsync(int userId, int? pageNumber = null, int? pageSize = null)
    {
        return await GetAsync(userId, FriendshipStatus.Pending, pageNumber, pageSize);
    }

    public async Task<List<Friendship>> GetSentRequestsAsync(int userId, int? pageNumber = null, int? pageSize = null)
    {
        return await GetWhereAsync(friendship =>
            friendship.RequesterUserId == userId && friendship.FriendshipStatus == FriendshipStatus.Pending, pageNumber, pageSize);
    }

    /// <summary>
    /// fetches the friendships that got sent to userId with optional status filter
    /// </summary>
    private async Task<List<Friendship>> GetAsync(
        int userId,
        FriendshipStatus? status,
        int? pageNumber = null,
        int? pageSize = null
        )
    {
        return await GetWhereAsync(friendship => 
            friendship.AddresseeUserId == userId && (!status.HasValue || friendship.FriendshipStatus == status), // only checks for status if the parameter has value
            pageNumber, pageSize); 
    }

    /// <summary>
    /// fetches the friendships of userId which have been accepted (from either)
    /// </summary>
    public async Task<List<Friendship>> GetFriendshipsAsync(int userId, int? pageNumber = null, int? pageSize = null)
    {
        return await GetWhereAsync(friendship =>
            (friendship.RequesterUserId == userId || friendship.AddresseeUserId == userId) &&
            friendship.FriendshipStatus == FriendshipStatus.Accepted,
            pageNumber, pageSize);
    }

    public async Task<List<int>> GetFriendIdsAsync(int userId)
    {
        return await _dbSet
            .Where(friendship =>
                (friendship.RequesterUserId == userId || friendship.AddresseeUserId == userId) &&
                friendship.FriendshipStatus == FriendshipStatus.Accepted)
            .Select(friendship =>
                friendship.RequesterUserId == userId ? friendship.AddresseeUserId : friendship.RequesterUserId)
            .ToListAsync();
    }
    
    public async Task<Friendship?> GetRelationshipAsync(int requesterId, int addresseeId, bool orderMatters = false)
    {
        return await GetFirstAsync(friendship => orderMatters
            ? friendship.RequesterUserId == requesterId && friendship.AddresseeUserId == addresseeId
            
            : (friendship.RequesterUserId == requesterId || friendship.RequesterUserId == addresseeId) &&
              (friendship.AddresseeUserId == requesterId || friendship.AddresseeUserId == addresseeId)
        );
    }

    public async Task<Friendship?> GetAcceptedFriendshipAsync(int requesterId, int addresseeId,
        bool                                                      orderMatters = false)
    {
        return await GetFirstAsync(friendship => orderMatters
            ? friendship.RequesterUserId == requesterId && friendship.AddresseeUserId == addresseeId && friendship.FriendshipStatus == FriendshipStatus.Accepted
            
            : (friendship.RequesterUserId == requesterId || friendship.RequesterUserId == addresseeId) &&
              (friendship.AddresseeUserId == requesterId || friendship.AddresseeUserId == addresseeId) &&
              (friendship.FriendshipStatus == FriendshipStatus.Accepted)
        );
    
    }

    public async Task<bool> ExistsAsync(int userIdA, int userIdB, FriendshipStatus? status)
    {
        return await ExistsAsync(friendship => 
                (friendship.RequesterUserId == userIdA || friendship.RequesterUserId == userIdB) &&
                (friendship.AddresseeUserId == userIdA || friendship.AddresseeUserId == userIdB) &&
                (friendship.FriendshipStatus == status || !status.HasValue));
    }

    public async Task UpdateStatusAsync(Friendship friendship, FriendshipStatus status)
    {
        friendship.FriendshipStatus = status;
        friendship.LastUpdatedAt = DateTime.UtcNow;
        
        // await _dbContext.SaveChangesAsync();
    }

    public async Task DeleteUserFriendshipsAsync(int userId)
    {
        await _dbContext.Database.ExecuteSqlRawAsync(
            "DELETE FROM Friendships WHERE RequesterUserId = {0} OR AddresseeUserId = {0}", userId);
        // await DeleteWhereAsync(friendship => friendship.RequesterUserId == userId ||
        //                                      friendship.AddresseeUserId == userId);
    }
}