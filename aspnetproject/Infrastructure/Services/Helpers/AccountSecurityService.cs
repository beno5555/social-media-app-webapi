using aspnetproject.Data.Models;
using aspnetproject.Data.Repositories;
using aspnetproject.Infrastructure.Dtos.Auth;

namespace aspnetproject.Infrastructure.Services.Helpers;

public class AccountSecurityService
{
    private readonly UserRepository _userRepository;
    private readonly TokenGenerator _tokenGenerator;
    private readonly EmailSender    _emailSender;
    private readonly RefreshTokenRepository _refreshTokenRepository;

    private readonly int _resetTokenMinutes;
    private readonly int _refreshTokenDays;

    public AccountSecurityService(
        UserRepository userRepository,
        TokenGenerator tokenGenerator,
        EmailSender emailSender,
        RefreshTokenRepository refreshTokenRepository,
        IConfiguration configuration
        )
    {
        _userRepository = userRepository;
        _tokenGenerator = tokenGenerator;
        _emailSender = emailSender;
        _refreshTokenRepository = refreshTokenRepository;

        _resetTokenMinutes = configuration.GetValue<int>("PasswordConfiguration:ResetTokenMinutes");
        _refreshTokenDays = configuration.GetValue<int>("Jwt:RefreshTokenDays");
    }

    public async Task<bool> HandlePasswordResetRequest(User user)
    {
        var resetToken = _tokenGenerator.GenerateRefreshToken();
        await IssueResetTokenAsync(user, resetToken);
        var sentEmail = await NotifyPasswordResetRequest(user.Email, resetToken);

        return sentEmail;
    }
    public async Task HandleFailedRegisterAttempt(User user)
    {
        string resetToken = _tokenGenerator.GenerateRefreshToken();

        await IssueResetTokenAsync(user, resetToken);
        await _emailSender.SendAsync(
            user.Email,
            "Someone tried to register with your email",
            $"<p>Someone tried to create an account using this email address, which is already registered.</p><p>If this was you, you can reset your password using this token: {resetToken}</p><p>Token expires in {_resetTokenMinutes} minutes.</p><p>If this wasn't you, you can safely ignore this email.</p>"
        );
    }
    public async Task<bool> HandleAccountActivationRequest(User user)
    {
        var activationToken = _tokenGenerator.GenerateRefreshToken();
        
        await IssueResetTokenAsync(user, activationToken);
        bool emailSent = await NotifyAccountReactivationRequest(user.Email, activationToken);

        return emailSent;
    }
    
    private async Task IssueResetTokenAsync(User user, string rawResetToken)
    {
        var tokenHash = _tokenGenerator.HashToken(rawResetToken);
        user.ResetTokenHash = tokenHash;

        user.ResetTokenExpiresAt = DateTime.UtcNow.AddMinutes(_resetTokenMinutes);
        await _userRepository.SaveChangesAsync();
    }
    public async Task<AuthResultDto> IssueAccessAndRefreshTokensAsync(User user)
    {
        var accessToken     = _tokenGenerator.GenerateAccessToken(user);
        var rawRefreshToken = _tokenGenerator.GenerateRefreshToken();

        var refreshToken = new RefreshToken
        {
            UserId = user.Id,
            TokenHash = _tokenGenerator.HashToken(rawRefreshToken),
            ExpiresAt = DateTime.UtcNow.AddDays(_refreshTokenDays)
        };

        await _refreshTokenRepository.AddAsync(refreshToken);

        var tokens = new AuthResultDto
        {
            AccessToken = accessToken,
            RefreshToken = rawRefreshToken
        };

        return tokens;
    }

    private async Task<bool> NotifyAccountReactivationRequest(string userEmail, string activationToken)
    {
        return await _emailSender.SendAsync(
            userEmail,
            "Reactivate your account",
            $"<p>You requested to reactivate your account.</p><p>You can reactivate using this token: {activationToken}</p><p>Token expires in {_resetTokenMinutes} minutes.</p><p>If you did not request this, you can safely ignore this email.</p>"
        );
    }
    private async Task<bool> NotifyPasswordResetRequest(string userEmail, string resetToken)
    {
        return await _emailSender.SendAsync(
            userEmail,
            "Reset your password",
            $"<p>Your password reset token: {resetToken}</p><p>Token expires in {_resetTokenMinutes} minutes.</p>");
    }
    public async Task NotifyAccountReactivation(string userEmail)
    {
        await _emailSender.SendAsync(
            userEmail, 
            "Your account has been reactivated",
            "<p>Your account has been reactivated and you can now sign in as usual.</p><p>If you did not expect this, please contact support.</p>"
            );
    }
    public async Task NotifyPasswordReset(string userEmail)
    {
        await _emailSender.SendAsync(
            userEmail, 
            "Your password has been reset",
            "<p>Your password was just reset.</p><p>If you did not do this, please contact support immediately.</p>"
        );
    }
}