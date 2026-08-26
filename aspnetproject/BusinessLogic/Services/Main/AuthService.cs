using aspnetproject.BusinessLogic.Dtos;
using aspnetproject.BusinessLogic.Dtos.Auth;
using aspnetproject.BusinessLogic.Dtos.UserDtos;
using aspnetproject.BusinessLogic.Mappers;
using aspnetproject.BusinessLogic.Services.Helpers;
using aspnetproject.Common.Responses;
using aspnetproject.Data.Repositories;
using aspnetproject.Models;
using aspnetproject.Repositories;

namespace aspnetproject.BusinessLogic.Services.Main;

public class AuthService
{
    private readonly UserRepository         _userRepository;
    private readonly RefreshTokenRepository _refreshTokenRepository;
    
    private readonly UserMapper             _userMapper;
    
    private readonly PasswordHasher         _passwordHasher;
    private readonly TokenGenerator         _tokenGenerator;
    private readonly IConfiguration         _configuration;

    private const string LoginErrorMessage = "Invalid username or password";
    private const string RefreshErrorMessage = "Invalid or expired refresh token";
    
    public AuthService(
        UserRepository userRepository,
        RefreshTokenRepository refreshTokenRepository,
        
        UserMapper userMapper,
        
        PasswordHasher passwordHasher,
        TokenGenerator tokenGenerator,
        IConfiguration configuration
        )
    {
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        
        _userMapper = userMapper;
        
        _passwordHasher = passwordHasher;
        _tokenGenerator = tokenGenerator;
        _configuration = configuration;
    }

    public async Task<ApplicationResponse<DisplayUserDto>> RegisterAsync(RegisterDto registerDto)
    {
        var response = new ApplicationResponse<DisplayUserDto>();
        var      emailExists      = await _userRepository.ExistsByEmailAsync(registerDto.Email);

        if (!emailExists)
        {
            var usernameExists = await _userRepository.ExistsByUsernameAsync(registerDto.Username);
            
            if (!usernameExists)
            {
                var (hash, salt) = _passwordHasher.HashPassword(registerDto.Password);
                User userToRegister = _userMapper.ToEntity(registerDto, hash, salt);
            
                await _userRepository.AddAsync(userToRegister);

                var displayDto = _userMapper.ToDisplay(userToRegister);
                response.Data = displayDto;
                response.Message = "User registered successfully";
            }
            else
            {
                response.Fail("Username is already taken");
            }
        }
        else 
        {
            response.Fail("Email is already taken");
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
                loginResponse.Ok(authResult);
            }
            else
            {
                loginResponse.Fail(LoginErrorMessage);
            }
        }
        else
        {
            loginResponse.Fail(LoginErrorMessage);
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
            var user       = await _userRepository.GetByIdAsync(existingToken.UserId);

            if (user is not null)
            {
                await _refreshTokenRepository.RevokeAsync(existingToken);
                var authResult = await IssueTokensAsync(user!);
                response.Ok(authResult);
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