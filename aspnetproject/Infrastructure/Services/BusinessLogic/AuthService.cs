using aspnetproject.Common.ProjectConstants;
using aspnetproject.Common.Responses;
using aspnetproject.Data.Models;
using aspnetproject.Data.Repositories;
using aspnetproject.Infrastructure.Dtos.Auth;
using aspnetproject.Infrastructure.Dtos.Auth.Password;
using aspnetproject.Infrastructure.Dtos.Users;
using aspnetproject.Infrastructure.Mappers;
using aspnetproject.Infrastructure.Services.BusinessLogic.Base;
using aspnetproject.Infrastructure.Services.Helpers;
using aspnetproject.Infrastructure.Services.Logging;

namespace aspnetproject.Infrastructure.Services.BusinessLogic;

public class AuthService : BaseService
{
    private readonly UserRepository         _userRepository;
    private readonly RefreshTokenRepository _refreshTokenRepository;
    private readonly PasswordHasher         _passwordHasher;
    private readonly TokenGenerator         _tokenGenerator;
    private readonly AccountSecurityService _accountSecurityService;
    
    public AuthService(
        UserRepository userRepository,
        RefreshTokenRepository refreshTokenRepository,
        PasswordHasher passwordHasher,
        TokenGenerator tokenGenerator,
        AccountSecurityService accountSecurityService,
        DatabaseLogger dbLogger
        ) : base(dbLogger)
    {
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _passwordHasher = passwordHasher;
        _tokenGenerator = tokenGenerator;
        _accountSecurityService = accountSecurityService;
    }

    public async Task<ApplicationResponse<FullUserDto>> RegisterAsync(RegisterDto registerDto)
    {
        var response    = new ApplicationResponse<FullUserDto>();
        var user = await _userRepository.GetUserByEmailAsync(registerDto.Email);

        if (user is null)
        {
            var usernameExists = await _userRepository.ExistsByUsernameAsync(registerDto.Username);
            
            if (!usernameExists)
            {
                var (hash, salt) = _passwordHasher.HashPassword(registerDto.Password);
                User userToRegister = AuthMapper.ToRegisteredEntity(registerDto, hash, salt);
            
                User addedUser = await _userRepository.AddUserAsync(userToRegister);
                var displayDto = UserMapper.ToFullDisplay(addedUser);
                
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
            await _accountSecurityService.HandleFailedRegisterAttempt(user);
        }

        return response;
    }
    public async Task<ApplicationResponse<AuthResultDto>> LoginAsync(LoginDto loginDto)
    {
        var response = new ApplicationResponse<AuthResultDto>();
        var userToLogin = await _userRepository.GetByUniqueIdentifierWithRolesAsync(loginDto.UniqueIdentifier);
        
        if (userToLogin is not null)
        {
            bool validPassword = _passwordHasher.PasswordsMatch(loginDto.Password, userToLogin.PasswordHash, userToLogin.PasswordSalt);
            
            if (validPassword)
            {
                var authResult = await _accountSecurityService.IssueAccessAndRefreshTokensAsync(userToLogin);
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
            // add login count and send warning email after every 3 invalid attempts.
        }

        return response;
    }
    public async Task<ApplicationResponse<AuthResultDto>> RefreshAsync(string rawRefreshToken)
    {
        var response = new ApplicationResponse<AuthResultDto>();
        
        var tokenHash     = _tokenGenerator.HashToken(rawRefreshToken);
        var existingToken = await _refreshTokenRepository.GetActiveByHashAsync(tokenHash);

        if (existingToken is not null)
        {
            var user = await _userRepository.GetUserByIdAsync(existingToken.UserId);

            if (user is not null)
            {
                await _refreshTokenRepository.RevokeAsync(existingToken);
                var authResult = await _accountSecurityService.IssueAccessAndRefreshTokensAsync(user);
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

    public async Task<ApplicationResponse> RequestPasswordResetAsync(ForgotPasswordDto forgotPasswordDto)
    {
        var response = new ApplicationResponse();

        var user = await _userRepository.GetByUniqueIdentifierAsync(forgotPasswordDto.Email);
        if (user is not null)
        {
            bool sentEmailToUser = await _accountSecurityService.HandlePasswordResetRequest(user);
            if (sentEmailToUser)
            {
                response.Ok(ResponseMessages.PasswordResetRequested);
                await LogResultAsync(response.Succeeded, nameof(RequestPasswordResetAsync), nameof(User), $"{ResponseMessages.PasswordResetRequested}. Reset token sent to the email", null);
            }
            else
            {
                response.Fail(ResponseMessages.CouldNotSendEmail);
                await LogResultAsync(response.Succeeded, nameof(RequestPasswordResetAsync), nameof(User), $"{ResponseMessages.CouldNotSendEmail}: Something went wrong", null);
            }
            
        }
        else
        {
            response.Fail(ResponseMessages.UserNotFound);
            await LogResultAsync(response.Succeeded, nameof(RequestPasswordResetAsync), nameof(User), $"Failed to generate reset token. {ResponseMessages.UserNotFound}", null);
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
            var isDifferent = !(_passwordHasher.PasswordsMatch(resetPasswordDto.NewPassword, user.PasswordHash, user.PasswordSalt));
            if (isDifferent)
            {
                var (hash, salt) = _passwordHasher.HashPassword(resetPasswordDto.NewPassword);
            
                _userRepository.UpdatePassword(user, hash, salt);

                await _refreshTokenRepository.RevokeAllForUserAsync(user.Id);
                await _userRepository.SaveChangesAsync();
            
                response.Ok(ResponseMessages.PasswordResetSuccessful);
                await LogResultAsync(response.Succeeded, nameof(ResetPasswordAsync), nameof(User), $"{ResponseMessages.PasswordResetSuccessful}. Revoked all refresh tokens for user to force login", null);
                await _accountSecurityService.NotifyPasswordReset(user.Email);
            }
            else
            {
                response.Fail(ResponseMessages.NewPasswordCannotBeTheSame);
                await LogResultAsync(response.Succeeded, nameof(ResetPasswordAsync), nameof(User), ResponseMessages.NewPasswordCannotBeTheSame, user.Id);
            }
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
}