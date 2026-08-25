using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace aspnetproject.Controllers.Base;

[ApiController]
public abstract class BaseController : ControllerBase
{
    protected string? GetUserId()
    {
        var userIdRaw = User.FindFirst(ClaimTypes.NameIdentifier)!.Value;
        return userIdRaw;
        
        if (int.TryParse(userIdRaw, out int userId))
        {
            // return userId;
        }
        
        return null;
    }
}