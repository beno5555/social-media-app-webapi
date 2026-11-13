using System.Security.Claims;
using aspnetproject.Infrastructure.Services.Helpers;
using aspnetproject.Infrastructure.Services.Websockets;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace aspnetproject.Hubs;

[Authorize]
public class MessageHub : Hub
{
    private readonly UserConnectionTracker _userConnectionTracker;
    private readonly PresenceService       _presenceService;
    private readonly TypingService         _typingService;

    public MessageHub(
        UserConnectionTracker userConnectionTracker,
        PresenceService presenceService,
        TypingService typingService
        )
    {
        _userConnectionTracker = userConnectionTracker;
        _presenceService = presenceService;
        _typingService = typingService;
    }
    
    private int GetUserId()
    {
        var userIdRaw = Context.User!.FindFirst(ClaimTypes.NameIdentifier)!.Value;
        var userId    = int.Parse(userIdRaw);

        return userId;
    }

    public override async Task OnConnectedAsync()
    {
        int userId = GetUserId();
        await Groups.AddToGroupAsync(Context.ConnectionId, userId.ToString());

        var isFirstConnection = _userConnectionTracker.AddConnection(userId);
        if (isFirstConnection)
        {
            await _presenceService.NotifyUserOnlineAsync(userId);
        }
        
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = GetUserId();
        
        var wasLastConnection = _userConnectionTracker.RemoveConnection(userId);
        if (wasLastConnection)
        {
            await _presenceService.NotifyUserOfflineAsync(userId);
        }
        
        await base.OnDisconnectedAsync(exception);
    }

    // Called by the client while its user writes to receiverId. The service decides whether the signal is relayed.
    public async Task SendTyping(int receiverId)
    {
        await _typingService.RelayTypingAsync(GetUserId(), receiverId);
    }
}