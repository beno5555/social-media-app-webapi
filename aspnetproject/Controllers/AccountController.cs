using aspnetproject.Common.ProjectConstants.Enums;
using aspnetproject.Common.Responses;
using aspnetproject.Controllers.Base;
using aspnetproject.Extensions;
using aspnetproject.Infrastructure.Dtos.Accounts;
using aspnetproject.Infrastructure.Dtos.UserRoles;
using aspnetproject.Infrastructure.Dtos.Users;
using aspnetproject.Infrastructure.Services.BusinessLogic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace aspnetproject.Controllers;

[Route("api/account-management")]
[Authorize]
public class AccountController : BaseController
{
    private readonly AccountService _accountService;

    public AccountController(AccountService accountService)
    {
        _accountService = accountService;
    }

    [HttpPut]
    [Route("{id:int}/assign-administrator")]
    [Authorize(Roles = nameof(RoleName.Administrator))]
    [EnableRateLimiting(RateLimitConfig.Policies.AdminActivateAccount)]
    public async Task<ActionResult<ApplicationResponse<DisplayUserRoleDto>>> AssignAdminRole(int id)
    {
        var response = await _accountService.AssignAdminRoleAsync(id);

        if (response.Succeeded)
        {
            return Ok(response);
        }

        return BadRequest(response);
    }
    
    [HttpPut]
    [Route("{id:int}/remove-administrator")]
    [Authorize(Roles = nameof(RoleName.Administrator))]
    [EnableRateLimiting(RateLimitConfig.Policies.AdminActivateAccount)]
    public async Task<ActionResult<ApplicationResponse>> UnassignAdminRole(int id)
    {
        var response = await _accountService.UnassignAdminRoleAsync(id);

        if (response.Succeeded)
        {
            return Ok(response);
        }

        return BadRequest(response);
    }

    [HttpDelete]
    [Route("mine")]
    [EnableRateLimiting(RateLimitConfig.Policies.DeleteOwnAccount)]
    public async Task<ActionResult<ApplicationResponse>> DeleteOwnAccount()
    {
        int userId   = GetUserId();
        var response = await _accountService.SoftDeleteAccountAsync(userId);

        if (response.Succeeded)
        {
            return Ok(response);
        }

        return BadRequest(response);
    }
    
    [HttpDelete]
    [Route("{id:int}")]
    [Authorize(Roles = "Administrator")]
    [EnableRateLimiting(RateLimitConfig.Policies.AdminDeleteAccount)]
    public async Task<ActionResult<ApplicationResponse>> DeleteUserAccount(int id)
    {
        var response = await _accountService.SoftDeleteAccountAsync(id);

        if (response.Succeeded)
        {
            return Ok(response);
        }

        return BadRequest(response);
    }

    [HttpPatch]
    [Route("mine/deactivate")]
    [EnableRateLimiting(RateLimitConfig.Policies.AdminDeactivateAccount)]
    public async Task<ActionResult<ApplicationResponse>> DeactivateOwnAccount()
    {
        int userId   = GetUserId();
        var response = await _accountService.DeactivateAccountAsync(userId);

        if (response.Succeeded)
        {
            return Ok(response);
        }

        return BadRequest(response);
    }

    [HttpPatch]
    [Route("{id:int}/deactivate")]
    [EnableRateLimiting(RateLimitConfig.Policies.DeactivateOwnAccount)]
    public async Task<ActionResult<ApplicationResponse>> DeactivateAccount(int id)
    {
        var response = await _accountService.DeactivateAccountAsync(id);

        if (response.Succeeded)
        {
            return Ok(response);
        }

        return NotFound(response);
    }

    [HttpPatch]
    [Route("{id:int}/activate")]
    [Authorize(Roles = nameof(RoleName.Administrator))]
    [EnableRateLimiting(RateLimitConfig.Policies.AdminActivateAccount)]
    public async Task<ActionResult<ApplicationResponse<FullUserDto>>> ActivateAccount(int id)
    {
        var response = await _accountService.ReactivateAccountAsync(id);

        if (response.Succeeded)
        {
            return Ok(response);
        }

        return BadRequest(response);
    }
    
    [HttpPatch]
    [Route("activate")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitConfig.Policies.ConfirmActivation)]
    public async Task<ActionResult<ApplicationResponse<FullUserDto>>> ActivateAccount([FromBody] ActivateAccountDto activateAccountDto)
    {
        var response = await _accountService.ReactivateOwnAccountAsync(activateAccountDto.ActivationToken);

        if (response.Succeeded)
        {
            return Ok(response);
        }

        return BadRequest(response);
    }
    
    [HttpPatch]
    [Route("request-activate")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitConfig.Policies.RequestActivation)]
    public async Task<ActionResult<ApplicationResponse<FullUserDto>>> RequestAccountActivation([FromBody] ActivationRequestDto activationRequestDto)
    {
        var response = await _accountService.RequestAccountActivationAsync(activationRequestDto.Email);

        if (response.Succeeded)
        {
            return Ok(response);
        }

        return BadRequest(response);
    }
}