using aspnetproject.Common.ProjectConstants.Enums;
using aspnetproject.Common.Responses;
using aspnetproject.Controllers.Base;
using aspnetproject.Infrastructure.Dtos.Accounts;
using aspnetproject.Infrastructure.Dtos.Users;
using aspnetproject.Infrastructure.Services.BusinessLogic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
    [Authorize(Roles = "Administrator")]
    public async Task<ActionResult<ApplicationResponse>> DeleteUserAccount(int id)
    {
        var response = await _accountService.DeleteAccountAsync(id);

        if (response.Succeeded)
        {
            return Ok(response);
        }

        return BadRequest(response);
    }

    [HttpPatch]
    [Route("mine/deactivate")]
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
    [Route("{email}/request-activate")]
    [AllowAnonymous]
    public async Task<ActionResult<ApplicationResponse<FullUserDto>>> RequestAccountActivation(string email)
    {
        var response = await _accountService.RequestAccountActivationAsync(email);

        if (response.Succeeded)
        {
            return Ok(response);
        }

        return BadRequest(response);
    }
}