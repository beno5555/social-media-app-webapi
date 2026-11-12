using aspnetproject.Data.Repositories;
using aspnetproject.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace aspnetproject.Infrastructure.Services.Websockets;

public class PresenceService
{
    private readonly IHubContext<MessageHub> _hubContext;
    private readonly UserRepository          _userRepository;
    private readonly FriendshipRepository    _friendshipRepository;

    public PresenceService(
        IHubContext<MessageHub> hubContext,
        UserRepository userRepository,
        FriendshipRepository friendshipRepository)
    {
        _hubContext = hubContext;
        _userRepository = userRepository;
        _friendshipRepository = friendshipRepository;
    }

    public async Task NotifyUserOnlineAsync(int userId)
    {
        await _userRepository.MarkActiveAsync(userId);

        var friendIds = await _friendshipRepository.GetFriendIdsAsync(userId);
        foreach (var friendId in friendIds)
        {
            await _hubContext.Clients.Group(friendId.ToString()).SendAsync("UserOnline", new { UserId = userId });
        }
    }

    public async Task NotifyUserOfflineAsync(int userId)
    {
        var now = DateTime.UtcNow;
        await _userRepository.MarkUserOfflineAsync(userId, now);
        
        var friendIds = await _friendshipRepository.GetFriendIdsAsync(userId);

        foreach (var friendId in friendIds)
        {
            await _hubContext.Clients.Group(friendId.ToString()).SendAsync("UserOffline", new { UserId = userId, LastActiveAt = now });
        }
    }
}