using aspnetproject.Common.Responses;
using aspnetproject.Controllers.Base;
using aspnetproject.Infrastructure.Dtos.Auth;
using aspnetproject.Infrastructure.Dtos.Users;
using aspnetproject.Infrastructure.Queries;
using aspnetproject.Infrastructure.Services.BusinessLogic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace aspnetproject.Controllers;

[Route("api/users")]
[Authorize]
public class UserController : BaseController
{
    private readonly UserService _userService;
    private readonly AuthService _authService;

    public UserController(UserService userService, AuthService authService)
    {
        _userService = userService;
        _authService = authService;
    }

    [HttpPost]
    [Authorize(Roles = "Administrator")]
    public async Task<ActionResult<ApplicationResponse<FullUserDto>>> CreateUser(RegisterDto registerDto)
    {
        var response = await _authService.RegisterAsync(registerDto);

        if (response.Succeeded)
        {
            return Ok(response);
        }

        return BadRequest(response);
    }
        
    [HttpGet]
    [Route("{username}")]
    [AllowAnonymous]
    public async Task<ActionResult<ApplicationResponse<StandardUserDto>>> GetByUsername(string username)
    {
        var response = await _userService.GetByUsername(username);
        if (response.Succeeded)
        {
            return Ok(response);
        }

        return NotFound(response);
    }

    [HttpGet]
    [Route("mine")]
    public async Task<ActionResult<ApplicationResponse<FullUserDto>>> GetProfile()
    {
        var username = GetUsername();
        var response = await _userService.GetByUsernameFull(username);

        if (response.Succeeded)
        {
            return Ok(response);
        }

        return BadRequest(response);
    }

    [HttpGet]
    [Route("search")]
    [AllowAnonymous]
    public async Task<ActionResult<ListResponse<MinimalUserDto>>> SearchUsers(
        [FromQuery] SearchUserQuery searchUserQuery)
    {
        var response = await _userService.SearchUsersAsync(
            searchUserQuery.Username,
            searchUserQuery.PageNumber,
            searchUserQuery.PageSize
            );

        if (response.Succeeded)
        {
            return Ok(response);
        }

        return BadRequest(response);
    }

    [HttpPut]
    [Route("mine")]
    public async Task<ActionResult<ApplicationResponse<FullUserDto>>> EditOwnProfile([FromBody] EditUserDto editUserDto)
    {
        int userId   = GetUserId();
        var response = await _userService.EditUserProfileAsync(userId, editUserDto);

        if (response.Succeeded)
        {
            return Ok(response);
        }

        return BadRequest(response);
    }
    
    [HttpPut]
    [Route("{id:int}")]
    [Authorize(Roles = "Administrator")]
    public async Task<ActionResult<ApplicationResponse<FullUserDto>>> EditUserProfile(int id, [FromBody] EditUserDto editUserDto)
    {
        var response = await _userService.EditUserProfileAsync(id, editUserDto);

        if (response.Succeeded)
        {
            return Ok(response);
        }

        return BadRequest(response);
    }
}