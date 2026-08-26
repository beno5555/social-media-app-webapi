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
}