using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace aspnetproject.Hubs;

[Authorize]
public class MessageHub : Hub
{
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
        await base.OnConnectedAsync();
    }
}