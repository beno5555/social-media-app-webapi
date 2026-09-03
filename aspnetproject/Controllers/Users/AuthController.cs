using aspnetproject.Common.Responses;
using aspnetproject.Extensions;
using aspnetproject.Infrastructure.Dtos.Auth;
using aspnetproject.Infrastructure.Dtos.Auth.Password;
using aspnetproject.Infrastructure.Dtos.Users;
using aspnetproject.Infrastructure.Services.BusinessLogic.Users;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace aspnetproject.Controllers.Users;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AuthService    _authService;
    private readonly IConfiguration _configuration;

    public AuthController(AuthService authService, IConfiguration configuration)
    {
        _authService = authService;
        _configuration = configuration;
    }
    
    [HttpPost]
    [Route("register")]
    [EnableRateLimiting(RateLimitConfig.Policies.Register)]
    public async Task<ActionResult<ApplicationResponse<FullUserDto>>> Register(RegisterDto registerDto)
    {
        var response = await _authService.RegisterAsync(registerDto);
        if (response.Succeeded)
        {
            return CreatedAtAction(nameof(UserController.GetByUsername), "User", new { response.Data!.Username }, response);
        }

        return BadRequest(response);
    }

    [HttpPost]
    [Route("login")]
    [EnableRateLimiting(RateLimitConfig.Policies.Login)]
    public async Task<ActionResult<string>> Login(LoginDto loginDto)
    {
        var response = await _authService.LoginAsync(loginDto);
        if (response.Succeeded)
        {
            SetRefreshTokenCookie(response.Data!.RefreshToken);
            return Ok(new { accessToken = response.Data.AccessToken });
        }

        return BadRequest(response);
    }

    [HttpPost]
    [Route("refresh")]
    [EnableRateLimiting(RateLimitConfig.Policies.Refresh)]
    public async Task<IActionResult> Refresh()
    {
        var rawRefreshToken = Request.Cookies["refreshToken"];

        if (!string.IsNullOrEmpty(rawRefreshToken))
        {
            var response = await _authService.RefreshAsync(rawRefreshToken);

            if (response.Succeeded)
            {
                SetRefreshTokenCookie(response.Data!.RefreshToken);
                return Ok(new { accessToken = response.Data!.AccessToken });
            }

            return BadRequest(response.Message);
        }

        return BadRequest(new { Message = "No refresh token provided" });
    }

    [HttpPost]
    [Route("request-password-reset")]
    [EnableRateLimiting(RateLimitConfig.Policies.RequestPasswordReset)]
    public async Task<ActionResult<ApplicationResponse>> RequestPasswordReset(ForgotPasswordDto forgotPasswordDto)
    {
        var response = await _authService.RequestPasswordResetAsync(forgotPasswordDto);

        if (response.Succeeded)
        {
            return Ok(response);
        }
        
        return BadRequest(response);
    }

    [HttpPost]
    [Route("reset-password")]
    [EnableRateLimiting(RateLimitConfig.Policies.ResetPassword)]
    public async Task<ActionResult<ApplicationResponse>> ResetPassword(ResetPasswordDto resetPasswordDto)
    {
        var response = await _authService.ResetPasswordAsync(resetPasswordDto);

        if (response.Succeeded)
        {
            return Ok(response);
        }
        
        return BadRequest(response);
    }

    [HttpPost]
    [Route("logout")]
    public async Task<ActionResult> Logout()
    {
        var rawRefreshToken = Request.Cookies["refreshToken"];

        if (!string.IsNullOrEmpty(rawRefreshToken))
        {
            await _authService.LogoutAsync(rawRefreshToken);
        }
        
        Response.Cookies.Delete("refreshToken");
        return Ok();
    }

    private void SetRefreshTokenCookie(string refreshToken)
    {
        string daysStr          = _configuration.GetSection("Jwt:RefreshTokenDays").Value!;
        int refreshTokenDays = int.Parse(daysStr);

        Response.Cookies.Append("refreshToken", refreshToken, new CookieOptions
        {
            HttpOnly = true,
            // Secure = true, // commented temporarily to enable testing on http://localhost
            SameSite = SameSiteMode.Strict,
            Expires = DateTime.UtcNow.AddDays(refreshTokenDays)
        });
    }
}