using aspnetproject.Common.Dtos.Posts;
using aspnetproject.Common.Responses;
using aspnetproject.Controllers.Base;
using aspnetproject.Infrastructure.Dtos.Posts;
using aspnetproject.Infrastructure.Queries;
using aspnetproject.Infrastructure.Services.BusinessLogic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace aspnetproject.Controllers;

[Route("api/posts")]
[Authorize]
public class PostController : BaseController
{
    private readonly PostService _postService;

    public PostController(PostService postService)
    {
        _postService = postService;
    }
    
    [HttpPost]
    public async Task<ActionResult<ApplicationResponse<FullPostDisplayDto>>> CreatePost([FromBody] CreatePostDto createPostDto)
    {
        if (string.IsNullOrEmpty(createPostDto.PostTitle))
        {
            return BadRequest(createPostDto);
        }
        var userId   = GetUserId();
        var response = await _postService.UploadPost(userId, createPostDto);

        if (response.Succeeded)
        {
            return CreatedAtAction(nameof(GetPostById), new { response.Data!.Id }, response);
        }

        return BadRequest(createPostDto);
    }
    
    [HttpGet]
    public async Task<ActionResult<ListResponse<MinimalPostDisplayDto>>> GetAll([FromQuery] PageQuery query)
    {
        var result = await _postService.GetAllPostsAsync(query.PageNumber, query.PageSize);
        return Ok(result);
    }
    
    [HttpGet]
    [Route("feed")]
    public async Task<ActionResult<ListResponse<StandardPostDisplayDto>>> GetFeed([FromQuery] PageQuery query)
    {
        int userId = GetUserId();
        var result = await _postService.GetFeedAsync(userId, query.PageNumber, query.PageSize);
        return Ok(result);
    }
    
    [HttpGet]
    [Route("mine")]
    public async Task<ActionResult<ListResponse<StandardPostDisplayDto>>> GetOwnPosts([FromQuery] PageQuery query)
    {
        int userId   = GetUserId();
        var response = await _postService.GetByUserIdAsync(userId, query.PageNumber, query.PageSize);

        if (response.Succeeded)
        {
            return Ok(response);
        }

        return BadRequest(response);
    }

    [HttpGet]
    [Route("{id:int}")]
    [AllowAnonymous]
    public async Task<ActionResult<ApplicationResponse<FullPostDisplayDto>>> GetPostById(int id)
    {
        var response = await _postService.GetPostByIdAsync(id);

        if (response.Succeeded)
        {
            return Ok(response);
        }

        return NotFound(response);
    }

    [HttpGet]
    [Route("user/{userId:int}")]
    [AllowAnonymous]
    public async Task<ActionResult<ListResponse<StandardPostDisplayDto>>> GetPostsByUser(int userId, [FromQuery] PageQuery                                                                query)
    {
        var response = await _postService.GetByUserIdAsync(userId, query.PageNumber, query.PageSize);

        if (response.Succeeded)
        {
            return Ok(response);
        }

        return BadRequest(response);
    }
    
    [HttpPut]
    [Route("{id:int}")]
    public async Task<ActionResult<ApplicationResponse<FullPostDisplayDto>>> UpdatePost(int id, [FromBody] UpdatePostDto updatePostDto)
    {
        var userId   = GetUserId();
        var response = await _postService.UpdatePost(id, userId, updatePostDto);

        if (response.Succeeded)
        {
            return Ok(response);
        }

        return BadRequest(response);
    }
    
    [HttpDelete]
    [Route("{id:int}")]
    public async Task<ActionResult<ApplicationResponse>> DeletePost(int id)
    {
        var userId   = GetUserId();
        var response = await _postService.DeletePostAsync(userId, id);
    
        if (response.Succeeded)
        {
            return Ok(response);
        }

        return BadRequest(response);
    }
}