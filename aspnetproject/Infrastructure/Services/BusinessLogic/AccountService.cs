using aspnetproject.Common.ProjectConstants;
using aspnetproject.Common.Responses;
using aspnetproject.Data.Repositories;
using aspnetproject.Infrastructure.Dtos.Users;
using aspnetproject.Infrastructure.Mappers;
using aspnetproject.Infrastructure.Services.Helpers;

namespace aspnetproject.Infrastructure.Services.BusinessLogic;

public class AccountService
{
    private readonly UserRepository       _userRepository;
    private readonly MessageRepository    _messageRepository;
    private readonly FriendshipRepository _friendshipRepository;
    private readonly CommentRepository    _commentRepository;
    
    private readonly PasswordHasher _passwordHasher;

    private readonly UserMapper _userMapper;

    public AccountService(
        UserRepository userRepository,
        MessageRepository messageRepository,
        FriendshipRepository friendshipRepository,
        CommentRepository commentRepository,
        
        PasswordHasher passwordHasher,
        
        UserMapper userMapper
        )
    {
        _userRepository = userRepository;
        _messageRepository = messageRepository;
        _friendshipRepository = friendshipRepository;
        _commentRepository = commentRepository;
        
        _passwordHasher = passwordHasher;
        
        _userMapper = userMapper;
    }

    public async Task<ApplicationResponse<FullUserDto>> CreateAccountAsync(CreateAccountDto createAccountDto)
    {
        var response = new ApplicationResponse<FullUserDto>();

        var uniqueUserCheck = await UniqueUser(createAccountDto);
        if (uniqueUserCheck.Succeeded)
        {
            var (hash, salt) = _passwordHasher.HashPassword(createAccountDto.Password);
            
            var userToAdd = _userMapper.ToEntity(createAccountDto, hash, salt);
            await _userRepository.AddAsync(userToAdd);

            var displayDto = _userMapper.ToFullDisplay(userToAdd);
            response.Ok(displayDto, ResponseMessages.AccountCreated);
        }
        else
        {
            response.Fail(uniqueUserCheck.Message);
        }

        return response;
    }

    private async Task<ApplicationResponse> UniqueUser(CreateAccountDto createAccountDto)
    {
        var response = new ApplicationResponse();
        
        var uniqueUsername = !await _userRepository.ExistsByUsernameAsync(createAccountDto.Username);
        if (uniqueUsername)
        {
            var uniqueEmail = !await _userRepository.ExistsByEmailAsync(createAccountDto.Email);
            if (uniqueEmail)
            {
                response.Ok();
            }
            else
            {
                response.Fail(ResponseMessages.UserWithEmailExists);
            }
        }
        else
        {
            response.Fail(ResponseMessages.UserWithUsernameExists);
        }

        return response;
    }
    
    public async Task<ApplicationResponse<StandardUserDto>> GetById(int id)
    {
        var response = new ApplicationResponse<StandardUserDto>();

        var user = await _userRepository.GetByIdAsync(id);
        if (user is not null)
        {
            var userDto = _userMapper.ToStandardDisplay(user);
            response.Ok(userDto, ResponseMessages.AccountRetrieved);
        }
        else
        {
            response.Fail(ResponseMessages.UserNotFound);
        }

        return response;
    }

    public async Task<ApplicationResponse<StandardUserDto>> GetByUsername(string username)
    {
        var response = new ApplicationResponse<StandardUserDto>();

        var user = await _userRepository.GetByUniqueIdentifierAsync(username);
        if (user is not null)
        {
            var userDto = _userMapper.ToStandardDisplay(user);
            response.Ok(userDto, ResponseMessages.AccountRetrieved);
        }
        else
        {
            response.Fail(ResponseMessages.UserNotFound);
        }

        return response;
    }
    
    public async Task<ApplicationResponse<FullUserDto>> GetByUsernameFull(string username)
    {
        var response = new ApplicationResponse<FullUserDto>();

        var user = await _userRepository.GetByUniqueIdentifierAsync(username);
        if (user is not null)
        {
            var userDto = _userMapper.ToFullDisplay(user);
            response.Ok(userDto, ResponseMessages.ProfileRetrieved);
        }
        else
        {
            response.Fail(ResponseMessages.NoAccountsMatchingUsername(username));
        }

        return response;
    }
    
    public async Task<ListResponse<MinimalUserDto>> SearchUsersAsync(string usernameInput, int? pageNumber = null, int? pageSize = null)
    {
        var response = new ListResponse<MinimalUserDto>();
        
        var users    = await _userRepository.SearchByUsernameAsync(usernameInput, pageNumber, pageSize);
        var userDtos = users.Select(_userMapper.ToMinimalDisplay).ToList();
        
        response.Ok(userDtos, ResponseMessages.SearchResultsForUsername(usernameInput));
        
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
                
                var userDto = _userMapper.ToFullDisplay(userToEdit);
                response.Ok(userDto, ResponseMessages.ProfileEdited);
            }
            else
            {
                response.Fail(ResponseMessages.UsernameCanBeChangedAgainOn(nextAllowedChangeDate!.Value));
            }
        }
        else
        {
            response.Fail(ResponseMessages.UserNotFound);
        }

        return response;
    }
    
    public async Task<ApplicationResponse> DeleteAccountAsync(int userId)
    {
        var response = new ApplicationResponse();

        await _userRepository.ExecuteInTransactionAsync(async () =>
        {
            _userRepository.ClearTracker();
            await DeleteUserRelatedData(userId);
            
            var userToDelete = await _userRepository.GetByIdAsync(userId);

            if (userToDelete is not null)
            {
                await _userRepository.DeleteAsync(userToDelete);
                response.Ok(ResponseMessages.AccountDeleted);
            }
            else
            {
                response.Fail(ResponseMessages.UserNotFound);
            }
        });

        return response;
    }

    private async Task DeleteUserRelatedData(int userId)
    {
        await _commentRepository.DeleteUserCommentsAsync(userId);
        await _messageRepository.DeleteUserMessagesAsync(userId);
        await _friendshipRepository.DeleteUserFriendshipsAsync(userId);
    }
}