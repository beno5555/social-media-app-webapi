using aspnetproject.Common.ProjectConstants;
using aspnetproject.Common.Responses;
using aspnetproject.Data.Models;
using aspnetproject.Data.Repositories;
using aspnetproject.Infrastructure.Dtos.Users;
using aspnetproject.Infrastructure.Mappers;
using aspnetproject.Infrastructure.Services.BusinessLogic.Base;
using aspnetproject.Infrastructure.Services.Logging;

namespace aspnetproject.Infrastructure.Services.BusinessLogic;

public class UserService : BaseService
{
    private readonly UserRepository _userRepository;

    public UserService(
        UserRepository userRepository,
        DatabaseLogger dbLogger
        ) : base(dbLogger)
    {
        _userRepository = userRepository;
    }

    public async Task<ApplicationResponse<StandardUserDto>> GetById(int id)
    {
        var response = new ApplicationResponse<StandardUserDto>();

        var user = await _userRepository.GetByIdAsync(id);
        if (user is not null)
        {
            var userDto = UserMapper.ToStandardDisplay(user);
            response.Ok(userDto, ResponseMessages.AccountRetrieved);
        }
        else
        {
            response.Fail(ResponseMessages.UserNotFound);
            await LogResultAsync(response.Succeeded, nameof(GetById), nameof(User), $"Retrieval failed. {response.Message}", null);
        }

        return response;
    }

    public async Task<ApplicationResponse<StandardUserDto>> GetByUsername(string username)
    {
        var response = new ApplicationResponse<StandardUserDto>();

        var user = await _userRepository.GetByUniqueIdentifierAsync(username);
        if (user is not null)
        {
            var userDto = UserMapper.ToStandardDisplay(user);
            response.Ok(userDto, ResponseMessages.AccountRetrieved);
        }
        else
        {
            response.Fail(ResponseMessages.UserNotFound);
            await LogResultAsync(response.Succeeded, nameof(GetByUsername), nameof(User), $"Retrieval by username failed. {response.Message}", null);
        }

        return response;
    }
    
    public async Task<ApplicationResponse<FullUserDto>> GetByUsernameFull(string username)
    {
        var response = new ApplicationResponse<FullUserDto>();

        var user = await _userRepository.GetByUniqueIdentifierAsync(username);
        if (user is not null)
        {
            var userDto = UserMapper.ToFullDisplay(user);
            response.Ok(userDto, ResponseMessages.ProfileRetrieved);
        }
        else
        {
            response.Fail(ResponseMessages.NoAccountsMatchingUsername(username));
            await LogResultAsync(response.Succeeded, nameof(GetByUsernameFull), nameof(User), $"Retrieval by username failed. {response.Message}", null);
        }

        return response;
    }
    
    public async Task<ListResponse<MinimalUserDto>> SearchUsersAsync(string usernameInput, int? pageNumber = null, int? pageSize = null)
    {
        var response = new ListResponse<MinimalUserDto>();
        
        var users    = await _userRepository.SearchByUsernameAsync(usernameInput, pageNumber, pageSize);
        var userDtos = users.Select(UserMapper.ToMinimalDisplay).ToList();
        
        response.Ok(userDtos, ResponseMessages.SearchResultsForUsername(usernameInput));
        await LogResultAsync(response.Succeeded, nameof(SearchUsersAsync), nameof(User), null, null);
        
        return response;
    }

    public async Task<ApplicationResponse<FullUserDto>> EditUserProfileAsync(int id, EditUserDto editUserDto)
    {
        var response = new ApplicationResponse<FullUserDto>();
        
        var userToEdit = await _userRepository.GetByIdAsync(id);

        if (userToEdit is not null)
        {
            editUserDto.Username = editUserDto.Username.ToLower();
            var usernameChanged = !string.Equals(userToEdit.Username, editUserDto.Username, StringComparison.OrdinalIgnoreCase);

            var nextAllowedChangeDate = userToEdit.UsernameLastChangedAt?.AddDays(Constants.UsernameChangeCooldownDays);
            var cooldownActive = usernameChanged && nextAllowedChangeDate is not null && nextAllowedChangeDate > DateTime.UtcNow;

            if (!cooldownActive)
            {
                await _userRepository.EditUserProfileAsync(userToEdit, editUserDto);
                
                var userDto = UserMapper.ToFullDisplay(userToEdit);
                response.Ok(userDto, ResponseMessages.ProfileEdited);
                await LogResultAsync(response.Succeeded, nameof(EditUserProfileAsync), nameof(User), null, id);
            }
            else
            {
                response.Fail(ResponseMessages.UsernameCanBeChangedAgainOn(nextAllowedChangeDate!.Value));
                await LogResultAsync(response.Succeeded, nameof(EditUserProfileAsync), nameof(User), $"Edit invalidated. Username cannot be edited yet. {response.Message}", id);
            }
        }
        else
        {
            response.Fail(ResponseMessages.UserNotFound);
            await LogResultAsync(response.Succeeded, nameof(EditUserProfileAsync), nameof(User), response.Message, null);
        }

        return response;
    }
}