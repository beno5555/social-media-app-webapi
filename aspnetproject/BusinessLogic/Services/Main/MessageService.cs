using aspnetproject.BusinessLogic.Dtos.MessageDtos;
using aspnetproject.BusinessLogic.Dtos.UserDtos;
using aspnetproject.BusinessLogic.Mappers;
using aspnetproject.BusinessLogic.Responses;
using aspnetproject.Data.Repositories;
using aspnetproject.ProjectConstants.Enums;
using aspnetproject.Repositories;

namespace aspnetproject.BusinessLogic.Services.Main;

public class MessageService
{
    private readonly MessageRepository    _messageRepository;
    private readonly UserRepository       _userRepository;
    private readonly FriendshipRepository _friendshipRepository;
    private readonly MessageMapper        _messageMapper;
    private readonly UserMapper           _userMapper;

    public MessageService(
        MessageRepository    messageRepository,
        UserRepository       userRepository,
        FriendshipRepository friendshipRepository,
        MessageMapper        messageMapper,
        UserMapper           userMapper)
    {
        _messageRepository = messageRepository;
        _userRepository = userRepository;
        _friendshipRepository = friendshipRepository;
        _messageMapper = messageMapper;
        _userMapper = userMapper;
    }

    /// <summary>
    /// Assumes that senderId is valid since the method should only be called when a logged-in user tries to send a message
    /// </summary>
    /// <param name="createMessageDto"></param>
    /// <returns></returns>
    public async Task<ApplicationResponse> SendMessageAsync(CreateMessageDto createMessageDto)
    {
        var response = new ApplicationResponse();
        
        var friendshipCheck = await ValidFriendship(createMessageDto.SenderId, createMessageDto.ReceiverId);
        
        if (friendshipCheck.Succeeded)
        {
            var messageToAdd = _messageMapper.ToEntity(createMessageDto);
            await _messageRepository.AddAsync(messageToAdd);
        }
        else
        {
            response = friendshipCheck;
        }

        return response;
    }


    public async Task<ApplicationResponse<List<DisplayMessageDto>>> GetConversationAsync(
        int currentUserId,     
        int  responderUserId,
        int? pageNumber = null,
        int? pageSize = null)
    {
        var response = new ApplicationResponse<List<DisplayMessageDto>>();
        var friendshipCheck = await ValidFriendship(currentUserId, responderUserId);

        if (friendshipCheck.Succeeded)
        {
            var messages = await _messageRepository.GetConversationAsync(currentUserId, responderUserId, pageNumber, pageSize);
            messages
                // .Where(message => !message.IsRead && message.ReceiverUserId == currentUserId)
                .Reverse(); // Repository fetches the messages in descending order to fetch the latest ones. we should reverse it.
            await _messageRepository.MarkAsReadAsync(messages);
            
            var messageDtos = messages.Select(_messageMapper.ToDisplay).ToList();
            response.Ok(messageDtos);
        }
        else
        {
            response.Fail(friendshipCheck.Message);
        }

        return response;
    }

    public async Task<List<DisplayUserDto>> GetConversationFriendsAsync(int userId, int? pageNumber, int? pageSize) =>
        await GetFriendsByConversationStatusAsync(userId, true, pageNumber, pageSize);

    public async Task<List<DisplayUserDto>> GetNonConversationFriendsAsync(int userId, int? pageNumber, int? pageSize) =>
        await GetFriendsByConversationStatusAsync(userId, false, pageNumber, pageSize);
    
    private async Task<List<DisplayUserDto>> GetFriendsByConversationStatusAsync(int userId, bool shouldHaveConversation, int? pageNumber, int? pageSize)
    {
        var friends =
            await _userRepository.GetFriendsByConversationStatusAsync(userId, shouldHaveConversation, pageNumber, pageSize);
        var userDtos = friends.Select(_userMapper.ToDisplay).ToList();

        return userDtos;
    }

    public async Task<bool> HasUnreadAsync(int userId)
    {
        return await _messageRepository.HasUnreadAsync(userId);
    }
    
    /// <summary>
    /// Checks if the ids match, receiverId is valid and 2 users are friends
    /// </summary>
    private async Task<ApplicationResponse> ValidFriendship(int senderId, int receiverId)
    {
        var response = new ApplicationResponse();

        if (receiverId != senderId)
        {
            var receiverExists = await _userRepository.ExistsByIdAsync(receiverId);

            if (receiverExists)
            {
                var areFriends = await _friendshipRepository.ExistsAsync(senderId, receiverId, FriendshipStatus.Accepted);

                if (!areFriends)
                {
                    response.Fail("You can only send messages to your friends");
                }
            }
            else
            {
                response.Fail("Receiver user not found");
            }
        }
        else
        {
            response.Fail("Cannot send a message to oneself");
        }

        return response;
    }

    public async Task<bool> HasConversation(int userAId, int userBId)
    {
        return await _messageRepository.HaveMessages(userAId, userBId);
    }
}