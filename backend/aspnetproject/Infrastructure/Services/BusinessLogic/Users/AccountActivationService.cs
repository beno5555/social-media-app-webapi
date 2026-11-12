using aspnetproject.Common.Domain;
using aspnetproject.Common.ProjectConstants;
using aspnetproject.Data.Models;
using aspnetproject.Data.Repositories;
using aspnetproject.Infrastructure.Dtos.Users;
using aspnetproject.Infrastructure.Mappers;
using aspnetproject.Infrastructure.Services.BusinessLogic.Base;
using aspnetproject.Infrastructure.Services.Helpers;
using aspnetproject.Infrastructure.Services.Logging;

namespace aspnetproject.Infrastructure.Services.BusinessLogic.Users;

public class AccountActivationService : BaseService
{
    private readonly UserRepository         _userRepository;
    private readonly RefreshTokenRepository _refreshTokenRepository;
    private readonly AccountSecurityService _accountSecurityService;
    private readonly TokenGenerator         _tokenGenerator;

    public AccountActivationService(
        UserRepository userRepository,
        RefreshTokenRepository refreshTokenRepository,
        AccountSecurityService accountSecurityService,
        TokenGenerator tokenGenerator,
        DatabaseLogger dbLogger
        ) : base(dbLogger)
    {
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _accountSecurityService = accountSecurityService;
        _tokenGenerator = tokenGenerator;
    }
    
    
    public async Task<ApplicationResponse> DeactivateAccountAsync(int id)
    {
        var response = new ApplicationResponse();
        
        var user = await _userRepository.GetByIdAsync(id);
        if (user is not null)
        {
            user.AccountDeactivatedAt = DateTime.UtcNow;
            await _refreshTokenRepository.RevokeAllForUserAsync(user.Id);
            
            await _userRepository.SaveChangesAsync();
            
            response.Ok(ResponseMessages.AccountDeactivated);
            await LogResultAsync(response.Succeeded, nameof(DeactivateAccountAsync), nameof(User), null, id);
            await _accountSecurityService.NotifyAccountDeactivation(user.Email);
        }
        else
        {
            response.Fail(ResponseMessages.UserNotFound);
            await LogResultAsync(response.Succeeded, nameof(DeactivateAccountAsync), nameof(User), $"{ResponseMessages.CouldNotDeactivateAccount}: {response.Message}", null);
        }

        return response;
    }
    public async Task<ApplicationResponse<FullUserDto>> ReactivateAccountAsync(int id)
    {
        var response = new ApplicationResponse<FullUserDto>();
        
        var user = await _userRepository.GetDeactivatedByIdAsync(id);
        if (user is not null)
        {
            user.AccountDeactivatedAt = null;
            await _userRepository.SaveChangesAsync();
            
            var userDto = UserMapper.ToFullDisplay(user);

            await _accountSecurityService.NotifyAccountReactivation(user.Email);
            
            response.Ok(userDto, ResponseMessages.AccountActivated);
            await LogResultAsync(response.Succeeded, nameof(ReactivateAccountAsync), nameof(User), null, id);
        }
        else
        {
            response.Fail(ResponseMessages.UserNotFound);
            await LogResultAsync(response.Succeeded, nameof(ReactivateAccountAsync), nameof(User), response.Message, null);
        }

        return response;
    }
    public async Task<ApplicationResponse> RequestAccountActivationAsync(string email)
    {
        var response = new ApplicationResponse();
        
        var user = await _userRepository.GetDeactivatedByEmailAsync(email);
        if (user is not null)
        {
            bool emailSent = await _accountSecurityService.HandleAccountActivationRequest(user);

            if (emailSent)
            {
                response.Ok(ResponseMessages.AccountActivationTokenSentToEmail);
                await LogResultAsync(response.Succeeded, nameof(RequestAccountActivationAsync), nameof(User), null, user.Id);
            }
            else
            {
                response.Ok(ResponseMessages.CouldNotDeactivateAccount);
                await LogResultAsync(response.Succeeded, nameof(RequestAccountActivationAsync), nameof(User), response.Message, user.Id);
            }
        }
        else
        {
            response.Fail(ResponseMessages.UserNotFound);
            await LogResultAsync(response.Succeeded, nameof(RequestAccountActivationAsync), nameof(User), response.Message, null);
        }

        return response;
    }  
    public async Task<ApplicationResponse<FullUserDto>> ReactivateOwnAccountAsync(string activationToken)
    {
        var response = new ApplicationResponse<FullUserDto>();

        var tokenHash = _tokenGenerator.HashToken(activationToken);
        var user      = await _userRepository.GetDeactivatedAccountByActivationTokenHashAsync(tokenHash);
        
        if (user is not null)
        {
            await _userRepository.ActivateAccountAsync(user);
            
            var userDto = UserMapper.ToFullDisplay(user);
            response.Ok(userDto, ResponseMessages.AccountActivated + ". " + ResponseMessages.YouCanNowSignIn);
            await LogResultAsync(response.Succeeded, nameof(ReactivateOwnAccountAsync), nameof(User), null, user.Id);
            
            await _accountSecurityService.NotifyAccountReactivation(user.Email);
        }
        else
        {
            response.Fail(ResponseMessages.UserNotFound);
            await LogResultAsync(response.Succeeded, nameof(ReactivateOwnAccountAsync), nameof(User), response.Message, null);
        }

        return response;
    }
}