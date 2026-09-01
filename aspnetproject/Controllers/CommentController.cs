using aspnetproject.Common.Responses;
using aspnetproject.Controllers.Base;
using aspnetproject.Extensions;
using aspnetproject.Infrastructure.Dtos.Comments;
using aspnetproject.Infrastructure.Queries;
using aspnetproject.Infrastructure.Services.BusinessLogic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace aspnetproject.Controllers;

[Authorize]
[Route("api/comments")]
public class CommentController : BaseController
{
    private readonly CommentService _commentService;

    public CommentController(CommentService commentService)
    {
        _commentService = commentService;
    }

    [HttpPost]
    [Route("posts/{postId:int}")]
    [EnableRateLimiting(RateLimitConfig.Policies.CreateComment)]
    public async Task<ActionResult<ApplicationResponse<FullCommentDto>>> CreateComment([FromRoute] int postId, [FromBody] CreateCommentDto createCommentDto)
    {
        var userId   = GetUserId();
        var response = await _commentService.AddCommentAsync(userId, postId, createCommentDto);

        if (response.Succeeded)
        {
            return CreatedAtAction(nameof(GetById), new { response.Data!.Id }, response);
        }

        return BadRequest(response);
    }

    [HttpGet]
    [Route("mine")]
    [EnableRateLimiting(RateLimitConfig.Policies.GetOwnComments)]
    public async Task<ActionResult<ListResponse<StandardCommentDto>>> GetOwnComments([FromQuery] PageQuery query)
    {
        var userId   = GetUserId();
        var response = await _commentService.GetByUserAsync(userId, query.PageNumber, query.PageSize);

        if (response.Succeeded)
        {
            return Ok(response);
        }

        return BadRequest(response);
    }
    
    [HttpGet]
    [Route("post/{postId:int}")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitConfig.Policies.GetCommentsByPost)]
    public async Task<ActionResult<ListResponse<StandardCommentDto>>> GetByPost(int postId, [FromQuery] PageQuery query)
    {
        var response = await _commentService.GetByPostAsync(postId, query.PageNumber, query.PageSize);

        if (response.Succeeded)
        {
            return Ok(response);
        }

        return BadRequest(response);
    }
    
    [HttpGet]
    [Route("{id:int}")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitConfig.Policies.GetCommentById)]
    public async Task<ActionResult<ApplicationResponse<FullCommentDto>>> GetById(int id)
    {
        var response = await _commentService.GetCommentByIdAsync(id);

        if (response.Succeeded)
        {
            return Ok(response);
        }

        return NotFound(response);
    }

    [HttpPut]
    [Route("{id:int}")]
    [EnableRateLimiting(RateLimitConfig.Policies.EditComment)]
    public async Task<ActionResult<ApplicationResponse<FullCommentDto>>> EditComment(int id, [FromBody] EditCommentDto editCommentDto)
    {
        var userId   = GetUserId();
        var response = await _commentService.EditCommentAsync(userId, id, editCommentDto);

        if (response.Succeeded)
        {
            return Ok(response);
        }

        return BadRequest(response);
    }

    [HttpDelete]
    [Route("{id:int}")]
    [EnableRateLimiting(RateLimitConfig.Policies.DeleteComment)]
    public async Task<ActionResult<ApplicationResponse>> DeleteComment(int id)
    {
        int  userId   = GetUserId();
        bool isAdmin  = IsAdministrator();
        var  response = await _commentService.DeleteCommentAsync(userId, isAdmin, id);

        if (response.Succeeded)
        {
            return Ok(response);
        }

        return BadRequest(response);
    }
}