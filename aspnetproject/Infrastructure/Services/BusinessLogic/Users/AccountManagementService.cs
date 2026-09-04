using aspnetproject.Common.ProjectConstants;
using aspnetproject.Common.ProjectConstants.Enums;
using aspnetproject.Common.Responses;
using aspnetproject.Data.Models;
using aspnetproject.Data.Repositories;
using aspnetproject.Infrastructure.Dtos.UserRoles;
using aspnetproject.Infrastructure.Services.BusinessLogic.Base;
using aspnetproject.Infrastructure.Services.Logging;

namespace aspnetproject.Infrastructure.Services.BusinessLogic.Users;

public class AccountManagementService : BaseService
{
    private readonly UserRepository         _userRepository;
    private readonly UserRoleRepository     _userRoleRepository;
    private readonly RoleRepository         _roleRepository;
    private readonly RefreshTokenRepository _refreshTokenRepository;

    public AccountManagementService(
        UserRepository         userRepository,
        UserRoleRepository     userRoleRepository,
        RoleRepository         roleRepository,
        RefreshTokenRepository refreshTokenRepository,
        DatabaseLogger         dbLogger
        ) : base(dbLogger)
    {
        _userRepository = userRepository;
        _userRoleRepository = userRoleRepository;
        _roleRepository = roleRepository;
        _refreshTokenRepository = refreshTokenRepository;
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
}
