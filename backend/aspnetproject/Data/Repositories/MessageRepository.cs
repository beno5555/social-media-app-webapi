using aspnetproject.Data.Models;
using aspnetproject.Data.Repositories.Base;
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
        return (await _dbSet
            .Include(message => message.SenderUser)
            .FirstOrDefaultAsync(message => message.Id == messageToAdd.Id))!;
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
            orderBy: query => query.OrderByDescending(message => message.CreatedAt),
            ignoreQueryFilters: true);
    }

    public async Task DeleteConversationAsync(int userA, int userB)
    {
        await DeleteWhereAsync(message => (message.SenderUserId == userA && message.ReceiverUserId == userB) ||
                                          (message.SenderUserId == userB && message.ReceiverUserId == userA));
    }

    public async Task<(bool marked, DateTime seenAt)> MarkConversationAsReadAsync(int readerId, int otherUserId)
    {
        bool executedSeen = false;
        var  seenAt       = DateTime.UtcNow;
        
        var lastMessage = await _dbSet
            .Where(message => (message.SenderUserId == otherUserId && message.ReceiverUserId == readerId) ||
                              (message.SenderUserId == readerId && message.ReceiverUserId == otherUserId))
            .OrderByDescending(message => message.CreatedAt)
            .FirstOrDefaultAsync();
        

        if (lastMessage is not null &&
            lastMessage.SenderUserId == otherUserId &&
            lastMessage.ReceiverUserId == readerId &&
            !lastMessage.Seen)
        {
            await _dbSet
                .Where(message =>
                    message.SenderUserId   == otherUserId &&
                    message.ReceiverUserId == readerId &&
                    !message.Seen
                )
                .ExecuteUpdateAsync(setter => setter
                    .SetProperty(message => message.Seen,          true)
                    .SetProperty(message => message.SeenAt,        seenAt)
                    .SetProperty(message => message.LastUpdatedAt, seenAt));
            
            executedSeen = true;
        }

        return (executedSeen, seenAt);
    }

    public async Task<int> GetUnreadConversationsCount(int userId)
    {
        return await _dbSet
            .Where(message => message.ReceiverUserId == userId && !message.Seen)
            .Select(message => message.SenderUserId)
            .Distinct()
            .CountAsync();
    }
}