using aspnetproject.Common.Responses;
using aspnetproject.Controllers.Base;
using aspnetproject.Infrastructure.Dtos.Users;
using aspnetproject.Infrastructure.Queries;
using aspnetproject.Infrastructure.Services.BusinessLogic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace aspnetproject.Controllers;

[Route("api/accounts")]
[Authorize]
public class AccountController : BaseController
{
    private readonly AccountService _accountService;

    public AccountController(AccountService accountService)
    {
        _accountService = accountService;
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApplicationResponse<FullUserDto>>> CreateUser(CreateAccountDto createAccountDto)
    {
        var response = await _accountService.CreateAccountAsync(createAccountDto);

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
        var response = await _accountService.GetByUsername(username);
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
        var response = await _accountService.GetByUsernameFull(username);

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
        var response = await _accountService.SearchUsersAsync(
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
        var response = await _accountService.EditUserProfileAsync(userId, editUserDto);

        if (response.Succeeded)
        {
            return Ok(response);
        }

        return BadRequest(response);
    }
    
    [HttpPut]
    [Route("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApplicationResponse<FullUserDto>>> EditUserProfile(int id, [FromBody] EditUserDto editUserDto)
    {
        var response = await _accountService.EditUserProfileAsync(id, editUserDto);

        if (response.Succeeded)
        {
            return Ok(response);
        }

        return BadRequest(response);
    }
    
    [HttpDelete]
    [Route("mine")]
    public async Task<ActionResult<ApplicationResponse>> DeleteOwnAccount()
    {
        int userId   = GetUserId();
        var response = await _accountService.DeleteAccountAsync(userId);

        if (response.Succeeded)
        {
            return Ok(response);
        }

        return BadRequest(response);
    }

    [HttpDelete]
    [Route("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApplicationResponse>> DeleteUserAccount(int id)
    {
        var response = await _accountService.DeleteAccountAsync(id);

        if (response.Succeeded)
        {
            return Ok(response);
        }

        return BadRequest(response);
    }   
}