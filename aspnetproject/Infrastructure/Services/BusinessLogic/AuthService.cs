using aspnetproject.Common.ProjectConstants;
using aspnetproject.Common.Responses;
using aspnetproject.Data.Models;
using aspnetproject.Data.Repositories;
using aspnetproject.Infrastructure.Dtos.Auth;
using aspnetproject.Infrastructure.Dtos.Auth.Password;
using aspnetproject.Infrastructure.Dtos.Users;
using aspnetproject.Infrastructure.Mappers;
using aspnetproject.Infrastructure.Services.Base;
using aspnetproject.Infrastructure.Services.Helpers;
using aspnetproject.Infrastructure.Services.Logging;

namespace aspnetproject.Infrastructure.Services.BusinessLogic;

public class AuthService : BaseService
{
    private readonly UserRepository         _userRepository;
    private readonly RefreshTokenRepository _refreshTokenRepository;
    private readonly EmailSender            _emailSender;
    private readonly PasswordHasher         _passwordHasher;
    private readonly TokenGenerator         _tokenGenerator;
    private readonly IConfiguration         _configuration;
    
    public AuthService(
        UserRepository userRepository,
        RefreshTokenRepository refreshTokenRepository,
        EmailSender emailSender,
        PasswordHasher passwordHasher,
        TokenGenerator tokenGenerator,
        IConfiguration configuration,
        DatabaseLogger dbLogger
        ) : base(dbLogger)
    {
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _emailSender = emailSender;
        _passwordHasher = passwordHasher;
        _tokenGenerator = tokenGenerator;
        _configuration = configuration;
    }

    public async Task<ApplicationResponse<StandardUserDto>> RegisterAsync(RegisterDto registerDto)
    {
        var response    = new ApplicationResponse<StandardUserDto>();
        var emailExists = await _userRepository.ExistsByEmailAsync(registerDto.Email);

        if (!emailExists)
        {
            var usernameExists = await _userRepository.ExistsByUsernameAsync(registerDto.Username);
            
            if (!usernameExists)
            {
                var (hash, salt) = _passwordHasher.HashPassword(registerDto.Password);
                User userToRegister = AuthMapper.ToRegisteredEntity(registerDto, hash, salt);
            
                await _userRepository.AddAsync(userToRegister);

                var displayDto = AuthMapper.ToStandardDisplay(userToRegister);
                response.Ok(displayDto, ResponseMessages.RegistrationSuccessful);
                await LogResultAsync(response.Succeeded, nameof(RegisterAsync), nameof(User), null, userToRegister.Id);
            }
            else
            {
                response.Fail(ResponseMessages.UsernameIsAlreadyTaken);
                await LogResultAsync(response.Succeeded, nameof(RegisterAsync), nameof(User), $"Registration failed: {response.Message}", null);
            }
        }
        else 
        {
            response.Fail(ResponseMessages.EmailIsAlreadyTaken);
            await LogResultAsync(response.Succeeded, nameof(RegisterAsync), nameof(User), $"Registration failed: {response.Message}", null);
        }

        return response;
    }

    public async Task<ApplicationResponse<AuthResultDto>> LoginAsync(LoginDto loginDto)
    {
        var response = new ApplicationResponse<AuthResultDto>();
        var userToLogin = await _userRepository.GetByUniqueIdentifierAsync(loginDto.UniqueIdentifier);
        
        if (userToLogin is not null)
        {
            bool validPassword = _passwordHasher.VerifyPassword(loginDto.Password, userToLogin.PasswordHash, userToLogin.PasswordSalt);
            
            if (validPassword)
            {
                var authResult = await IssueTokensAsync(userToLogin);
                response.Ok(authResult, ResponseMessages.LoginSuccessful);
                await LogResultAsync(response.Succeeded, nameof(LoginAsync), nameof(User), null, userToLogin.Id);
            }
            else
            {
                response.Fail(ResponseMessages.LoginErrorMessage);
                await LogResultAsync(response.Succeeded, nameof(LoginAsync), nameof(User), $"Login failed: {response.Message}.", userToLogin.Id);
            }
        }
        else
        {
            response.Fail(ResponseMessages.LoginErrorMessage);
            await LogResultAsync(response.Succeeded, nameof(LoginAsync), nameof(User), $"Login failed: {response.Message}.", null);
        }

        return response;
    }

    /// <summary>
    /// provides a new access token by first validating a refresh token and then changes that refresh token as well
    /// </summary>
    public async Task<ApplicationResponse<AuthResultDto>> RefreshAsync(string rawRefreshToken)
    {
        var response = new ApplicationResponse<AuthResultDto>();
        
        var tokenHash     = _tokenGenerator.HashToken(rawRefreshToken);
        var existingToken = await _refreshTokenRepository.GetActiveByHashAsync(tokenHash);

        if (existingToken is not null)
        {
            var user = await _userRepository.GetByIdAsync(existingToken.UserId);

            if (user is not null)
            {
                await _refreshTokenRepository.RevokeAsync(existingToken);
                var authResult = await IssueTokensAsync(user!);
                response.Ok(authResult);
            }
            else
            {
                response.Fail(ResponseMessages.InvalidRefreshToken);
                await LogResultAsync(response.Succeeded, nameof(RefreshAsync), nameof(RefreshToken), $"Could not refresh access token: {response.Message}", existingToken.Id);
            }
        }
        else
        {
            response.Fail(ResponseMessages.RefreshTokenNotFound);
            await LogResultAsync(response.Succeeded, nameof(RefreshAsync), nameof(RefreshToken), $"Could not refresh access token: {response.Message}", null);
        }

        return response;
    }

    public async Task<ApplicationResponse> ForgotPasswordAsync(ForgotPasswordDto forgotPasswordDto)
    {
        var response = new ApplicationResponse();

        var user = await _userRepository.GetByUniqueIdentifierAsync(forgotPasswordDto.Email);
        if (user is not null)
        {
            var rawToken  = _tokenGenerator.GenerateRefreshToken();
            var tokenHash = _tokenGenerator.HashToken(rawToken);
            
            user.PasswordResetTokenHash = tokenHash;
            user.PasswordResetTokenExpiresAt = DateTime.UtcNow.AddMinutes(30);
            await _userRepository.SaveChangesAsync();
            
            await _emailSender.SendAsync(
                user.Email, 
                "Reset your password",
                $"<p>Your password reset token: {rawToken}</p><p>Token expires in 30 minutes.</p>");

            response.Ok(ResponseMessages.PasswordResetRequested);
            await LogResultAsync(response.Succeeded, nameof(ForgotPasswordAsync), nameof(User), $"{ResponseMessages.PasswordResetRequested}. Reset token sent to the email", null);
        }
        else
        {
            response.Fail(ResponseMessages.UserNotFound);
            await LogResultAsync(response.Succeeded, nameof(ForgotPasswordAsync), nameof(User), $"Failed to generate reset token. {ResponseMessages.UserNotFound}", null);
        }

        return response;
    }

    public async Task<ApplicationResponse> ResetPasswordAsync(ResetPasswordDto resetPasswordDto)
    {
        var response = new ApplicationResponse();
        
        var tokenHash = _tokenGenerator.HashToken(resetPasswordDto.Token);
        var user      = await _userRepository.GetByPasswordResetTokenHash(tokenHash);

        if (user is not null)
        {
            var (hash, salt) = _passwordHasher.HashPassword(resetPasswordDto.NewPassword);
            _userRepository.UpdatePassword(user, hash, salt);

            await _refreshTokenRepository.RevokeAllForUserAsync(user.Id);
            await _userRepository.SaveChangesAsync();
            
            response.Ok(ResponseMessages.PasswordResetSuccessful);
            await LogResultAsync(response.Succeeded, nameof(ResetPasswordAsync), nameof(User), $"{ResponseMessages.PasswordResetSuccessful}. Revoked all refresh tokens for user to force login", null);
        }
        else
        {
            response.Fail(ResponseMessages.InvalidPasswordResetToken);
            await LogResultAsync(response.Succeeded, nameof(ResetPasswordAsync), nameof(User), ResponseMessages.InvalidOrExpiredPasswordResetToken, null);
        }

        return response;
    }

    public async Task LogoutAsync(string rawRefreshToken)
    {
        var tokenHash = _tokenGenerator.HashToken(rawRefreshToken);
        
        var existingToken = await _refreshTokenRepository.GetActiveByHashAsync(tokenHash);
        if (existingToken is not null)
        {
            await _refreshTokenRepository.RevokeAsync(existingToken);
        }
    }

    private async Task<AuthResultDto> IssueTokensAsync(User user)
    {
        var accessToken     = _tokenGenerator.GenerateAccessToken(user.Id, user.Username);
        var rawRefreshToken = _tokenGenerator.GenerateRefreshToken();

        var refreshDays = int.Parse(_configuration.GetSection("Jwt:RefreshTokenDays").Value!);

        var refreshToken = new RefreshToken
        {
            UserId = user.Id,
            TokenHash = _tokenGenerator.HashToken(rawRefreshToken),
            ExpiresAt = DateTime.UtcNow.AddDays(refreshDays)
        };

        await _refreshTokenRepository.AddAsync(refreshToken);

        var tokens = new AuthResultDto
        {
            AccessToken = accessToken,
            RefreshToken = rawRefreshToken
        };

        return tokens;
    }
}