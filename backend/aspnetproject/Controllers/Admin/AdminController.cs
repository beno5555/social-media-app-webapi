using aspnetproject.Common.Domain;
using aspnetproject.Common.ProjectConstants.Enums;
using aspnetproject.Controllers.Base;
using aspnetproject.Extensions;
using aspnetproject.Infrastructure.Dtos.DatabaseLogs;
using aspnetproject.Infrastructure.Dtos.Users;
using aspnetproject.Infrastructure.Queries;
using aspnetproject.Infrastructure.Services.BusinessLogic;
using aspnetproject.Infrastructure.Services.BusinessLogic.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace aspnetproject.Controllers.Admin;

[Route("api/admin")]
[Authorize(Roles = nameof(RoleName.Administrator))]
public class AdminController : BaseController
{
    private readonly LogService  _logService;
    private readonly UserService _userService;

    public AdminController(LogService logService, UserService userService)
    {
        _logService  = logService;
        _userService = userService;
    }

    [HttpGet]
    [Route("logs")]
    [EnableRateLimiting(RateLimitConfig.Policies.AdminReadLogs)]
    public async Task<ActionResult<ListResponse<LogDisplayDto>>> GetLogs([FromQuery] LogQueryDto logQuery)
    {
        var response = await _logService.GetLogsAsync(logQuery);

        if (response.Succeeded)
        {
            return Ok(response);
        }

        return BadRequest(response);
    }

    // Not in the documentation's endpoint list: the admin panel needs it to find a deactivated account to restore.
    [HttpGet]
    [Route("users")]
    [EnableRateLimiting(RateLimitConfig.Policies.AdminListAccounts)]
    public async Task<ActionResult<ListResponse<AdminUserDto>>> GetAccounts([FromQuery] AccountListQuery accountQuery)
    {
        var response = await _userService.GetAccountsAsync(accountQuery);

        if (response.Succeeded)
        {
            return Ok(response);
        }

        return BadRequest(response);
    }
}
