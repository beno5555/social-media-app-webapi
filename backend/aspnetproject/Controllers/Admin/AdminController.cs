using aspnetproject.Common.Domain;
using aspnetproject.Common.ProjectConstants.Enums;
using aspnetproject.Controllers.Base;
using aspnetproject.Extensions;
using aspnetproject.Infrastructure.Dtos.DatabaseLogs;
using aspnetproject.Infrastructure.Dtos.Moderation;
using aspnetproject.Infrastructure.Dtos.Users;
using aspnetproject.Infrastructure.Queries;
using aspnetproject.Infrastructure.Services.BusinessLogic;
using aspnetproject.Infrastructure.Services.BusinessLogic.Content;
using aspnetproject.Infrastructure.Services.BusinessLogic.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace aspnetproject.Controllers.Admin;

[Route("api/admin")]
[Authorize(Roles = nameof(RoleName.Administrator))]
public class AdminController : BaseController
{
    private readonly LogService        _logService;
    private readonly UserService       _userService;
    private readonly ModerationService _moderationService;

    public AdminController(LogService logService, UserService userService, ModerationService moderationService)
    {
        _logService        = logService;
        _userService       = userService;
        _moderationService = moderationService;
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

    [HttpPut]
    [Route("posts/{id:int}/moderation")]
    [EnableRateLimiting(RateLimitConfig.Policies.AdminModeratePost)]
    public async Task<ActionResult<ApplicationResponse>> SetPostStatus(int id, [FromBody] SetModerationStatusDto setModerationStatusDto)
    {
        var response = await _moderationService.SetPostStatusAsync(id, setModerationStatusDto.Status!.Value);

        if (response.Succeeded)
        {
            return Ok(response);
        }

        return NotFound(response);
    }

    [HttpPut]
    [Route("comments/{id:int}/moderation")]
    [EnableRateLimiting(RateLimitConfig.Policies.AdminModerateComment)]
    public async Task<ActionResult<ApplicationResponse>> SetCommentStatus(int id, [FromBody] SetModerationStatusDto setModerationStatusDto)
    {
        var response = await _moderationService.SetCommentStatusAsync(id, setModerationStatusDto.Status!.Value);

        if (response.Succeeded)
        {
            return Ok(response);
        }

        return NotFound(response);
    }

    // One content type per call: posts and comments live in separate tables, so there is no single page across both.
    [HttpGet]
    [Route("moderation-queue")]
    [EnableRateLimiting(RateLimitConfig.Policies.AdminReadModerationQueue)]
    [ProducesResponseType<ListResponse<PostSnapshotDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ListResponse<CommentSnapshotDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ApplicationResponse>> GetModerationQueue([FromQuery] ModerationQueueQuery queueQuery)
    {
        ApplicationResponse response = queueQuery.Type == ModerationContentType.Post
            ? await _moderationService.GetFlaggedPostsAsync(queueQuery)
            : await _moderationService.GetFlaggedCommentsAsync(queueQuery);

        if (response.Succeeded)
        {
            return Ok(response);
        }

        return BadRequest(response);
    }
}
