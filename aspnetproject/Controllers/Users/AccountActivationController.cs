using aspnetproject.Common.ProjectConstants.Enums;
using aspnetproject.Common.Responses;
using aspnetproject.Controllers.Base;
using aspnetproject.Extensions;
using aspnetproject.Infrastructure.Dtos.Accounts;
using aspnetproject.Infrastructure.Dtos.Users;
using aspnetproject.Infrastructure.Services.BusinessLogic.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace aspnetproject.Controllers.Users;

[Authorize]
[Route("api/accounts")]
public class AccountActivationController : BaseController
{
    private readonly AccountActivationService _accountActivationService;

    public AccountActivationController(AccountActivationService accountActivationService)
    {
        _accountActivationService = accountActivationService;
    }
    
    [HttpPatch]
    [Route("mine/deactivate")]
    [EnableRateLimiting(RateLimitConfig.Policies.AdminDeactivateAccount)]
    public async Task<ActionResult<ApplicationResponse>> DeactivateOwnAccount()
    {
        int userId   = GetUserId();
        var response = await _accountActivationService.DeactivateAccountAsync(userId);

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
        var response = await _accountActivationService.DeactivateAccountAsync(id);

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
        var response = await _accountActivationService.ReactivateAccountAsync(id);

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
        var response = await _accountActivationService.ReactivateOwnAccountAsync(activateAccountDto.ActivationToken);

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
        var response = await _accountActivationService.RequestAccountActivationAsync(activationRequestDto.Email);

        if (response.Succeeded)
        {
            return Ok(response);
        }

        return BadRequest(response);
    }
}