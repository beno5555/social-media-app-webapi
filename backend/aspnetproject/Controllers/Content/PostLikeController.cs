using aspnetproject.Common.Domain;
using aspnetproject.Controllers.Base;
using aspnetproject.Extensions;
using aspnetproject.Infrastructure.Dtos.Users;
using aspnetproject.Infrastructure.Queries;
using aspnetproject.Infrastructure.Services.BusinessLogic.Content;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace aspnetproject.Controllers.Content;

[Authorize]
[Route("api/posts/{postId:int}/likes")]
public class PostLikeController : BaseController
{
    private readonly PostLikeService _postLikeService;

    public PostLikeController(PostLikeService postLikeService)
    {
        _postLikeService = postLikeService;
    }

    [HttpPost]
    [EnableRateLimiting(RateLimitConfig.Policies.LikePost)]
    public async Task<ActionResult<ApplicationResponse>> Like([FromRoute] int postId)
    {
        var userId   = GetUserId();
        var response = await _postLikeService.LikePostAsync(userId, postId);

        if (response.Succeeded)
        {
            return Ok(response);
        }

        // Mixed meanings (unknown post, already liked), so one status for both.
        return BadRequest(response);
    }

    [HttpDelete]
    [EnableRateLimiting(RateLimitConfig.Policies.UnlikePost)]
    public async Task<ActionResult<ApplicationResponse>> Unlike([FromRoute] int postId)
    {
        var userId   = GetUserId();
        var response = await _postLikeService.UnlikePostAsync(userId, postId);

        if (response.Succeeded)
        {
            return Ok(response);
        }

        return NotFound(response);
    }

    [HttpGet]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitConfig.Policies.ReadPostLikes)]
    public async Task<ActionResult<ListResponse<MinimalUserDto>>> GetLikers([FromRoute] int postId, [FromQuery] PageQuery query)
    {
        var response = await _postLikeService.GetLikersAsync(postId, query.PageNumber, query.PageSize);

        if (response.Succeeded)
        {
            return Ok(response);
        }

        return NotFound(response);
    }
}
