using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace aspnetproject.Controllers.Base;

[ApiController]
public abstract class BaseController : ControllerBase
{
    protected int GetUserId()
    {
        var userIdRaw = User.FindFirst(ClaimTypes.NameIdentifier)!.Value;
        return int.Parse(userIdRaw);
    }
    protected string GetUsername()
    {
        var username = User.FindFirst(ClaimTypes.Name)!.Value;
        return username;
    }
}