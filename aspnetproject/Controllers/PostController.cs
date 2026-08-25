using aspnetproject.BusinessLogic.Dtos.Common;
using aspnetproject.BusinessLogic.Dtos.Posts;
using aspnetproject.BusinessLogic.Responses;
using aspnetproject.BusinessLogic.Services.Main;
using aspnetproject.Common.Responses;
using aspnetproject.Controllers.Base;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace aspnetproject.Controllers;

[Route("api/posts")]
// [Authorize]
public class PostController : BaseController
{
    private readonly PostService _postService;

    public PostController(PostService postService)
    {
        _postService = postService;
    }

    [HttpGet]
    [Route("{id:int}")]
    [AllowAnonymous]
    public async Task<ActionResult<ApplicationResponse<DetailedPostDisplayDto>>> GetPostById(int id)
    {
        var response = await _postService.GetPostByIdAsync(id);

        if (response.Succeeded)
        {
            return Ok(response);
        }

        return NotFound(response);
    }

    [HttpGet]
    public async Task<ActionResult<ListResponse<SummarizedPostDisplayDto>>> GetAll([FromQuery] PageQuery query)
    {
        var result = await _postService.GetAllPostsAsync(query.PageNumber, query.PageSize);
        return Ok(result);
    }

    [HttpGet]
    [Route("feed")]
    public async Task<ActionResult<ListResponse<SummarizedPostDisplayDto>>> GetFeed([FromQuery] PageQuery query)
    {
        var userId = GetUserId();
        if (userId is not null)
        {
            // var result = await _postService.GetFeedAsync(userId.Value, query.PageNumber, query.PageSize);
            // return Ok(result);
            return Ok(userId);
        }

        return Unauthorized("userId: " + userId);
    }
    //
    // [HttpPost]
    // [Route("create")]
    // public async Task<ActionResult<ApplicationResponse<DetailedPostDisplayDto>>> CreatePost(
    //     [FromBody] CreatePostDto createPostDto)
    // {
    //     var userId   = GetUserId();
    //     if (userId is not null)
    //     {
    //         var response = await _postService.UploadPost(userId.Value, createPostDto);
    //
    //         if (response.Succeeded)
    //         {
    //             return CreatedAtAction(nameof(GetPostById), new { response.Data!.Id }, response);
    //         }
    //
    //         return BadRequest(response);
    //     }
    //
    //     return Unauthorized();
    // }
    //
    // [HttpPut]
    // [Route("{id:int}")]
    // public async Task<ActionResult<ApplicationResponse<DetailedPostDisplayDto>>> UpdatePost(int id, [FromBody] UpdatePostDto updatePostDto)
    // {
    //     var userId   = GetUserId();
    //     if (userId is not null)
    //     {
    //         var response = await _postService.UpdatePost(id, userId.Value, updatePostDto);
    //
    //         if (response.Succeeded)
    //         {
    //             return Ok(response);
    //         }
    //
    //         return BadRequest(response);
    //     }
    //
    //     return Unauthorized();
    // }
    //
    // [HttpDelete]
    // [Route("{id:int}")]
    // public async Task<ActionResult<ApplicationResponse>> DeletePost(int id)
    // {
    //     var userId   = GetUserId();
    //     if (userId is not null)
    //     {
    //         var response = await _postService.DeletePostAsync(userId.Value, id);
    //     
    //         if (response.Succeeded)
    //         {
    //             return Ok(response);
    //         }
    //
    //         return BadRequest(response);
    //     }
    //
    //     return Unauthorized();
    // }
}