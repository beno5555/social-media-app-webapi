using aspnetproject.Common.Domain;
using aspnetproject.Common.ProjectConstants.Enums;
using aspnetproject.Controllers.Base;
using aspnetproject.Extensions;
using aspnetproject.Infrastructure.Dtos.UserRoles;
using aspnetproject.Infrastructure.Services.BusinessLogic.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace aspnetproject.Controllers.Users;

[Route("api/accounts")]
[Authorize]
public class AccountManagementController : BaseController
{
    private readonly AccountManagementService _accountManagementService;

    public AccountManagementController(AccountManagementService accountManagementService)
    {
        _accountManagementService = accountManagementService;
    }

    [HttpPut]
    [Route("{id:int}/assign-administrator")]
    [Authorize(Roles = nameof(RoleName.Administrator))]
    [EnableRateLimiting(RateLimitConfig.Policies.AdminActivateAccount)]
    public async Task<ActionResult<ApplicationResponse<DisplayUserRoleDto>>> AssignAdminRole(int id)
    {
        var response = await _accountManagementService.AssignAdminRoleAsync(id);

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
        var response = await _accountManagementService.UnassignAdminRoleAsync(id);

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
        var response = await _accountManagementService.SoftDeleteAccountAsync(userId);

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
        var response = await _accountManagementService.SoftDeleteAccountAsync(id);

        if (response.Succeeded)
        {
            return Ok(response);
        }

        return BadRequest(response);
    }
}