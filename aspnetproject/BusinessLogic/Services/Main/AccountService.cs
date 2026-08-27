using aspnetproject.BusinessLogic.Dtos.UserDtos;
using aspnetproject.BusinessLogic.Mappers;
using aspnetproject.Common.Dtos.Users;
using aspnetproject.Common.Responses;
using aspnetproject.Data.Repositories;

namespace aspnetproject.BusinessLogic.Services.Main;

public class AccountService
{
    private readonly UserRepository       _userRepository;
    private readonly MessageRepository    _messageRepository;
    private readonly FriendshipRepository _friendshipRepository;
    private readonly CommentRepository    _commentRepository;
    private readonly UserMapper           _userMapper;

    public AccountService(UserRepository userRepository, MessageRepository messageRepository, FriendshipRepository friendshipRepository, CommentRepository commentRepository, UserMapper userMapper)
    {
        _userRepository = userRepository;
        _messageRepository = messageRepository;
        _friendshipRepository = friendshipRepository;
        _commentRepository = commentRepository;
        _userMapper = userMapper;
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
            }
            else
            {
                response.Fail("User not found.");
            }
        });

        return response;
    }

    // Assumes valid userId
    private async Task DeleteUserRelatedData(int userId)
    {
        await _commentRepository.DeleteUserCommentsAsync(userId);
        await _messageRepository.DeleteUserMessagesAsync(userId);
        await _friendshipRepository.DeleteUserFriendshipsAsync(userId);
    }

    public async Task<List<StandardUserDto>> GetUsersAsync(int currentUserId, int? pageNumber, int? pageSize)
    {
        var users    = await _userRepository.GetUsersAsync(currentUserId, pageNumber, pageSize);
        var userDtos = users
            .Select(_userMapper.ToStandardDisplay)
            .ToList();
        
        return userDtos;
    }

    public async Task<List<StandardUserDto>> SearchUsersAsync(string usernameInput, int? pageNumber = null,
        int?                                                        pageSize = null)
    {
        var users = await _userRepository.SearchByUsernameAsync(usernameInput, pageNumber, pageSize);
        var userDtos = users
            .Select(_userMapper.ToStandardDisplay)
            .ToList();
        
        return userDtos;
    }

    public async Task<ApplicationResponse<StandardUserDto>> GetByUsername(string username)
    {
        var response = new ApplicationResponse<StandardUserDto>();
        
        var user     = await _userRepository.GetByUniqueIdentifierAsync(username);
        if (user is not null)
        {
            var userDto = _userMapper.ToStandardDisplay(user);
            response.Ok(userDto);
        }
        else
        {
            response.Fail($"No users matching {username}.");
        }

        return response;
    }

    public async Task<ApplicationResponse> UpdateBioAsync(int userId, string bio)
    {
        var response = new ApplicationResponse();
        
        var userToUpdate = await _userRepository.GetByIdAsync(userId);
        if (userToUpdate is not null)
        {
            await _userRepository.UpdateBioAsync(userToUpdate, bio);
        }
        else
        {
            response.Fail("User not found");
        }

        return response;
    }
}