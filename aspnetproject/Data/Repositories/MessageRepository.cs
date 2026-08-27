using aspnetproject.Data.Repositories.Base;
using aspnetproject.Models;
using Microsoft.EntityFrameworkCore;

namespace aspnetproject.Data.Repositories;

public class MessageRepository : BaseEntityRepository<Message>
{
    public MessageRepository(ApplicationDbContext dbContext) : base(dbContext)
    {
        
    }

    public async Task<Message?> GetMessageByIdAsync(int id)
    {
        return await GetFirstAsync(message => message.Id == id);
    }

    public async Task<Message> AddMessageAsync(Message messageToAdd)
    {
        await AddAsync(messageToAdd);
        return (await _dbSet.FirstOrDefaultAsync(message => message.Id == messageToAdd.Id))!;
    }

    protected override IQueryable<Message> Query(bool track = true)
    {
        var query = _dbSet
            .Include(message => message.SenderUser)
            .Include(message => message.ReceiverUser);

        return track ? query : query.AsNoTracking();
    }

    public async Task<List<Message>> GetConversationAsync(
        int userA,
        int userB,
        int? pageNumber = null,
        int? pageSize = null )
    {
        return await GetWhereAsync(
            message => (message.SenderUserId == userA && message.ReceiverUserId == userB) ||
                       (message.SenderUserId == userB && message.ReceiverUserId == userA),
            pageNumber,
            pageSize,
            orderBy: query => query.OrderByDescending(message => message.CreatedAt));
    }

    public async Task DeleteUserMessagesAsync(int userId)
    {
        await _dbContext.Database.ExecuteSqlRawAsync(
            "DELETE FROM Messages WHERE SenderUserId = {0} OR ReceiverUserId = {0}", userId);
        // await DeleteWhereAsync(message => message.SenderUserId   == userId ||
        //                                   message.ReceiverUserId == userId);
    }

    public async Task DeleteConversationAsync(int userA, int userB)
    {
        await DeleteWhereAsync(message => (message.SenderUserId == userA && message.ReceiverUserId == userB) ||
                                          (message.SenderUserId == userB && message.ReceiverUserId == userA));
    }

    public async Task<List<Message>> GetUnreadAsync(int senderId, int receiverId)
    {
        return await GetWhereAsync(message =>
            message.SenderUserId == senderId && message.ReceiverUserId == receiverId && !message.IsRead);
    }

    /// <summary>
    /// use this when the unread messages have already been loaded
    /// </summary>
    public async Task MarkAsReadAsync(List<Message> unreadMessages)
    {
        foreach (var message in unreadMessages)
        {
            message.IsRead = true;
        }

        await _dbContext.SaveChangesAsync();
    }

    /// <summary>
    /// use this when the messages have not been loaded
    /// </summary>
    public async Task MarkConversationAsReadAsync(int senderId, int receiverId)
    {
        await _dbSet.Where(message =>
                message.SenderUserId   == senderId   &&
                message.ReceiverUserId == receiverId && 
                !message.IsRead)
            .ExecuteUpdateAsync(setter => setter.SetProperty(message => message.IsRead, true));
    }

    public async Task<int> GetUnreadConversationsCount(int userId)
    {
        return await _dbSet
            .Where(message => message.ReceiverUserId == userId && !message.IsRead)
            .Select(message => message.SenderUserId)
            .Distinct()
            .CountAsync();
    }

    public async Task<bool> HaveMessages(int userAId, int userBId)
    {
        return await ExistsAsync(message => (message.SenderUserId == userAId && message.ReceiverUserId == userBId) ||
                                            (message.SenderUserId == userBId && message.ReceiverUserId == userAId));
    }
}