using aspnetproject.Common.ProjectConstants;
using aspnetproject.Common.ProjectConstants.Enums;
using aspnetproject.Common.Responses;
using aspnetproject.Data.Models;
using aspnetproject.Data.Repositories;
using aspnetproject.Infrastructure.Dtos.UserRoles;
using aspnetproject.Infrastructure.Dtos.Users;
using aspnetproject.Infrastructure.Mappers;
using aspnetproject.Infrastructure.Services.BusinessLogic.Base;
using aspnetproject.Infrastructure.Services.Helpers;
using aspnetproject.Infrastructure.Services.Logging;

namespace aspnetproject.Infrastructure.Services.BusinessLogic;

public class AccountService : BaseService
{
    private readonly UserRepository         _userRepository;
    private readonly UserRoleRepository     _userRoleRepository;
    private readonly RoleRepository         _roleRepository;
    private readonly RefreshTokenRepository _refreshTokenRepository;
    private readonly AccountSecurityService _accountSecurityService;
    private readonly TokenGenerator         _tokenGenerator;

    public AccountService(
        UserRepository         userRepository,
        UserRoleRepository     userRoleRepository,
        RoleRepository         roleRepository,
        RefreshTokenRepository refreshTokenRepository,
        AccountSecurityService accountSecurityService,
        TokenGenerator         tokenGenerator,
        DatabaseLogger         dbLogger
        ) : base(dbLogger)
    {
        _userRepository = userRepository;
        _userRoleRepository = userRoleRepository;
        _roleRepository = roleRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _accountSecurityService = accountSecurityService;
        _tokenGenerator = tokenGenerator;
    }

    public async Task<ApplicationResponse<DisplayUserRoleDto>> AssignAdminRoleAsync(int id)
    {
        var response = new ApplicationResponse<DisplayUserRoleDto>();
        
        var userExists =  await _userRepository.ExistsByIdAsync(id);
        if (userExists)
        {
            var role = await _roleRepository.GetRoleByName(nameof(RoleName.Administrator));
            if (role is not null)
            {
                bool userRoleExists = await _userRoleRepository.UserRoleExists(id, role.Id);
                if (!userRoleExists)
                {
                    var userRole = new UserRole
                    {
                        UserId = id,
                        RoleId = role.Id
                    };
                    await _userRoleRepository.AddAsync(userRole);

                    var userRoleDto = new DisplayUserRoleDto { UserId = userRole.UserId, RoleId = userRole.RoleId };
                    response.Ok(userRoleDto, ResponseMessages.AdministratorPrivilegesAssignedToUser);
                    await LogResultAsync(response.Succeeded, nameof(AssignAdminRoleAsync), nameof(User), null, null);
                }
                else
                {
                    response.Fail(ResponseMessages.RoleAlreadyAssigned);
                    await LogResultAsync(response.Succeeded, nameof(AssignAdminRoleAsync), nameof(UserRole), $"Could not assign role to user: {response.Message}", null);
                }
            }
            else
            {
                response.Fail(ResponseMessages.RoleNotFound);
                await LogResultAsync(response.Succeeded, nameof(AssignAdminRoleAsync), nameof(UserRole), response.Message, null);
            }
        }
        else
        {
            response.Fail(ResponseMessages.UserNotFound);
            await LogResultAsync(response.Succeeded, nameof(AssignAdminRoleAsync), nameof(UserRole), response.Message, null);
        }

        return response;
    }
    public async Task<ApplicationResponse> UnassignAdminRoleAsync(int userId)
    {
        var response = new ApplicationResponse();

        var userExists = await _userRepository.ExistsByIdAsync(userId);
        if (userExists)
        {
            var role = await _roleRepository.GetRoleByName(nameof(RoleName.Administrator));
            if (role is not null)
            {
                bool deleted = await _userRoleRepository.DeleteUserRole(userId, role.Id);
                if (deleted)
                {
                    response.Ok(ResponseMessages.RoleUnassignedFromUser);
                    await LogResultAsync(response.Succeeded, nameof(UnassignAdminRoleAsync), nameof(UserRole), null, null);
                }
                else
                {
                    response.Fail(ResponseMessages.UserRoleNotFound);
                    await LogResultAsync(response.Succeeded, nameof(UnassignAdminRoleAsync), nameof(UserRole), "Could not unassign role. the role was not attached to the user.", null);
                }
            }
            else
            {
                response.Fail(ResponseMessages.RoleNotFound);
                await LogResultAsync(response.Succeeded, nameof(UnassignAdminRoleAsync), nameof(UserRole), response.Message, null);
            }
        }
        else
        {
            response.Fail(ResponseMessages.UserNotFound);
            await LogResultAsync(response.Succeeded, nameof(UnassignAdminRoleAsync), nameof(UserRole), response.Message, null);
        }

        return response;
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
            await LogResultAsync(response.Succeeded, nameof(ReactivateOwnAccountAsync), nameof(User), null, id);
        }
        else
        {
            response.Fail(ResponseMessages.UserNotFound);
            await LogResultAsync(response.Succeeded, nameof(ReactivateOwnAccountAsync), nameof(User), response.Message, null);
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
            user.AccountDeactivatedAt = null;
            await _userRepository.SaveChangesAsync();
            
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
    
    public async Task<ApplicationResponse> SoftDeleteAccountAsync(int id)
    {
        var response = new ApplicationResponse();

        await _userRepository.ExecuteInTransactionAsync(async () =>
        {
            _userRepository.ClearTracker();
            await _refreshTokenRepository.RevokeAllForUserAsync(id);
            
            var userToDelete = await _userRepository.GetUserByIdNoFilterAsync(id);

            if (userToDelete is not null)
            {
                if (userToDelete.AccountDeletedAt == null)
                {
                    userToDelete.AccountDeletedAt = DateTime.UtcNow;
                    await _userRepository.SaveChangesAsync();
                    
                    response.Ok(ResponseMessages.AccountDeleted);
                    await LogResultAsync(response.Succeeded, nameof(SoftDeleteAccountAsync), nameof(User), null, id);
                }
                else
                {
                    response.Fail(ResponseMessages.UserNotFound);
                    await LogResultAsync(response.Succeeded, nameof(SoftDeleteAccountAsync), nameof(User), $"Could not soft delete: user is already soft deleted", id);
                }
            }
            else
            {
                response.Fail(ResponseMessages.UserNotFound);
                await LogResultAsync(response.Succeeded, nameof(SoftDeleteAccountAsync), nameof(User), $"Could not soft delete: {response.Message}", id);
            }
        });

        return response;
    }
    
    // private async Task UpdateUserRelatedDataAsync(int userId)
    // {
    //     await _refreshTokenRepository.RevokeAllForUserAsync(userId);
    //     await _commentRepository.SetUserIdToNullInCommentsAsync(userId);
    //     await _messageRepository.DeleteUserMessagesAsync(userId);
    //     await _friendshipRepository.DeleteUserFriendshipsAsync(userId);
    // }
}
