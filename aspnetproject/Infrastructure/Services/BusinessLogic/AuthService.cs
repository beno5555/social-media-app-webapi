using aspnetproject.BusinessLogic.Dtos.Auth;
using aspnetproject.BusinessLogic.Dtos.UserDtos;
using aspnetproject.BusinessLogic.Services.Helpers;
using aspnetproject.Common.ProjectConstants;
using aspnetproject.Common.Responses;
using aspnetproject.Data.Models;
using aspnetproject.Data.Repositories;
using aspnetproject.Infrastructure.Dtos.Auth;
using aspnetproject.Infrastructure.Dtos.Users;
using aspnetproject.Infrastructure.Mappers;
using aspnetproject.Infrastructure.Services.Helpers;

namespace aspnetproject.Infrastructure.Services.BusinessLogic;

public class AuthService
{
    private readonly UserRepository         _userRepository;
    private readonly RefreshTokenRepository _refreshTokenRepository;

    private readonly AuthMapper _authMapper;
    
    private readonly PasswordHasher _passwordHasher;
    private readonly TokenGenerator _tokenGenerator;
    private readonly IConfiguration _configuration;

    private const string RefreshErrorMessage = "Invalid or expired refresh token";
    
    public AuthService(
        UserRepository userRepository,
        RefreshTokenRepository refreshTokenRepository,
        
        AuthMapper authMapper,
        
        PasswordHasher passwordHasher,
        TokenGenerator tokenGenerator,
        IConfiguration configuration
        )
    {
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        
        _authMapper = authMapper;
        
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
                User userToRegister = _authMapper.ToRegisteredEntity(registerDto, hash, salt);
            
                await _userRepository.AddAsync(userToRegister);

                var displayDto = _authMapper.ToStandardDisplay(userToRegister);
                response.Ok(displayDto, ResponseMessages.RegistrationSuccessful);
            }
            else
            {
                response.Fail(ResponseMessages.UsernameIsAlreadyTaken);
            }
        }
        else 
        {
            response.Fail(ResponseMessages.EmailIsAlreadyTaken);
        }

        return response;
    }

    public async Task<ApplicationResponse<AuthResultDto>> LoginAsync(LoginDto loginDto)
    {
        var loginResponse = new ApplicationResponse<AuthResultDto>();
        var userToLogin = await _userRepository.GetByUniqueIdentifierAsync(loginDto.UniqueIdentifier);
        
        if (userToLogin is not null)
        {
            bool validPassword = _passwordHasher.VerifyPassword(loginDto.Password, userToLogin.PasswordHash, userToLogin.PasswordSalt);
            
            if (validPassword)
            {
                var authResult = await IssueTokensAsync(userToLogin);
                loginResponse.Ok(authResult, ResponseMessages.LoginSuccessful);
            }
            else
            {
                loginResponse.Fail(ResponseMessages.LoginErrorMessage);
            }
        }
        else
        {
            loginResponse.Fail(ResponseMessages.LoginErrorMessage);
        }

        return loginResponse;
    }

    /// <summary>
    /// provide a new access token by first validating a refresh token and then change that refresh token as well
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
                response.Fail(RefreshErrorMessage);
            }
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