using aspnetproject.Common.Responses;
using aspnetproject.Infrastructure.Dtos.Auth;
using aspnetproject.Infrastructure.Dtos.Users;
using aspnetproject.Infrastructure.Services.BusinessLogic;
using Microsoft.AspNetCore.Mvc;

namespace aspnetproject.Controllers;

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
    public async Task<ActionResult<ApplicationResponse<StandardUserDto>>> Register(RegisterDto registerDto)
    {
        var response = await _authService.RegisterAsync(registerDto);
        if (response.Succeeded)
        {
            return CreatedAtAction(nameof(AccountController.GetByUsername), "Account", new { response.Data!.Id }, response);
        }

        return BadRequest(response);
    }

    [HttpPost]
    [Route("login")]
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
    public async Task<ActionResult> Refresh()
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

        return BadRequest("No refresh token provided");
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