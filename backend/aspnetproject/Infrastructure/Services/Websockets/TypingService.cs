using aspnetproject.Common.ProjectConstants.Enums;
using aspnetproject.Data.Repositories;
using aspnetproject.Hubs;
using aspnetproject.Infrastructure.Dtos.WebsocketsTransfer;
using Microsoft.AspNetCore.SignalR;

namespace aspnetproject.Infrastructure.Services.Websockets;

// Relays a typing signal to a friend. Nothing is stored or logged, and a signal that may not be relayed is dropped silently.
public class TypingService
{
    private readonly IHubContext<MessageHub> _hubContext;
    private readonly FriendshipRepository    _friendshipRepository;

    public TypingService(
        IHubContext<MessageHub> hubContext,
        FriendshipRepository friendshipRepository)
    {
        _hubContext = hubContext;
        _friendshipRepository = friendshipRepository;
    }

    public async Task RelayTypingAsync(int senderId, int receiverId)
    {
        // No friendship links a user to themselves, so a signal to oneself is dropped here too.
        bool areFriends = await _friendshipRepository.ExistsAsync(senderId, receiverId, FriendshipStatus.Accepted);

        if (areFriends)
        {
            var userTypingDto = new UserTypingDto { UserId = senderId };
            await _hubContext.Clients.Group(receiverId.ToString()).SendAsync("UserTyping", userTypingDto);
        }
    }
}
