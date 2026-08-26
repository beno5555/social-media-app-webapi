using aspnetproject.BusinessLogic.Services;
using aspnetproject.BusinessLogic.Services.Main;
using aspnetproject.Common.Dtos.Comments;
using aspnetproject.Common.Dtos.Common;
using aspnetproject.Common.Responses;
using aspnetproject.Controllers.Base;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
    [Route("")]
    public async Task<ActionResult<ApplicationResponse<FullCommentDto>>> CreateComment([FromBody] CreateCommentDto createCommentDto)
    {
        var userId   = GetUserId();
        var response = await _commentService.AddCommentAsync(userId, createCommentDto);

        if (response.Succeeded)
        {
            return CreatedAtAction(nameof(GetById), new { response.Data!.Id }, response);
        }

        return BadRequest(response);
    }

    [HttpGet]
    [Route("mine")]
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
    public async Task<ActionResult<ApplicationResponse<FullCommentDto>>> EditComment(int id, [FromBody] EditCommentDto editCommentDto)
    {
        var response = await _commentService.EditCommentAsync(id, editCommentDto);

        if (response.Succeeded)
        {
            return Ok(response);
        }

        return BadRequest(response);
    }

    [HttpDelete]
    [Route("{id:int}")]
    public async Task<ActionResult<ApplicationResponse>> DeleteComment(int id)
    {
        int userId   = GetUserId();
        var response = await _commentService.DeleteCommentAsync(userId, id);

        if (response.Succeeded)
        {
            return Ok(response);
        }

        return BadRequest(response);
    }
}