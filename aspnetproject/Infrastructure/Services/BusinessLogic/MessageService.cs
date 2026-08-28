using aspnetproject.BusinessLogic.Mappers;
using aspnetproject.Common.Dtos.Users;
using aspnetproject.Common.Responses;
using aspnetproject.Data.Repositories;
using aspnetproject.Hubs;
using aspnetproject.Infrastructure.Dtos.Messages;
using aspnetproject.Infrastructure.Mappers;
using aspnetproject.ProjectConstants.Enums;
using Microsoft.AspNetCore.SignalR;
using Constants = aspnetproject.ProjectConstants.Constants;

namespace aspnetproject.Infrastructure.Services.BusinessLogic;

public class MessageService
{
    private readonly MessageRepository    _messageRepository;
    private readonly UserRepository       _userRepository;
    private readonly FriendshipRepository _friendshipRepository;

    private readonly MessageMapper _messageMapper;
    private readonly UserMapper    _userMapper;
    
    private readonly IHubContext<MessageHub> _hubContext;

    public MessageService(
        MessageRepository    messageRepository,
        UserRepository       userRepository,
        FriendshipRepository friendshipRepository,
        
        MessageMapper        messageMapper,
        UserMapper           userMapper,
        
        IHubContext<MessageHub> hubContext)
    {
        _messageRepository = messageRepository;
        _userRepository = userRepository;
        _friendshipRepository = friendshipRepository;
        
        _messageMapper = messageMapper;
        _userMapper = userMapper;
        
        _hubContext = hubContext;
    }

    /// <summary>
    /// Assumes that senderId is valid since the method should only be called when a logged-in user tries to send a message
    /// </summary>
    public async Task<ApplicationResponse<SentMessageDto>> SendMessageAsync(int senderId, int receiverId, CreateMessageDto createMessageDto)
    {
        var response = new ApplicationResponse<SentMessageDto>();
        
        var friendshipCheck = await ValidFriendship(senderId, receiverId);
        
        if (friendshipCheck.Succeeded)
        {
            var messageToAdd = _messageMapper.ToEntity(senderId, receiverId, createMessageDto);
            var message      = await _messageRepository.AddMessageAsync(messageToAdd);

            var pushMessageDto = _messageMapper.ToPush(message);
            await _hubContext.Clients.Group(receiverId.ToString()).SendAsync("ReceiveMessage", pushMessageDto);

            var displayMessageDto = _messageMapper.ToSentMessageDisplay(message);
            response.Ok(displayMessageDto, "Message sent successfully");
        }
        else
        {
            response.Fail(friendshipCheck.Message);
        }

        return response;
    }

    public async Task<ListResponse<StandardMessageDto>> GetConversationAsync(
        int readerId,     
        int  otherUserId,
        int? pageNumber = null,
        int? pageSize = null)
    {
        var response = new ListResponse<StandardMessageDto>();
        var friendshipCheck = await ValidFriendship(readerId, otherUserId);

        if (friendshipCheck.Succeeded)
        {
            var messages = await _messageRepository.GetConversationAsync(readerId, otherUserId, pageNumber, pageSize);
            messages.Reverse(); // Repository fetches the messages in descending order to fetch the latest ones. we should reverse it.
            
            await MarkAsReadAsync(readerId, otherUserId);
            
            var messageDtos = messages.Select(_messageMapper.ToStandardDisplay).ToList();
            response.Ok(messageDtos, "Conversation retrieved successfully!");
        }
        else
        {
            response.Fail(friendshipCheck.Message);
        }

        return response;
    }
    public async Task<ListResponse<ConversationFriendDto>> GetConversationFriendsAsync(int userId, int? pageNumber, int? pageSize)
    {
        var response = new ListResponse<ConversationFriendDto>();
        
        var conversationFriends = await _userRepository.GetConversationFriendsAsync(userId, pageNumber, pageSize);
        var conversationFriendDtos = conversationFriends.Select(_userMapper.ToConversationFriendDisplay).ToList();
        response.Ok(conversationFriendDtos, "Conversation friends retrieved successfully");

        return response;
    }
    public async Task<ListResponse<MinimalUserDto>> GetNonConversationFriendsAsync(int userId, int? pageNumber, int? pageSize) 
    {
        var response = new ListResponse<MinimalUserDto>();
        
        var friends =
            await _userRepository.GetFriendsByConversationStatusAsync(userId, shouldHaveConversation: false, pageNumber, pageSize);
        var userDtos = friends.Select(_userMapper.ToMinimalDisplay).ToList();
        
        response.Ok(userDtos, "Friends with no conversation retrieved successfully");

        return response;
    }
    public async Task<int> GetUnreadConversationsCount(int userId)
    {
        return await _messageRepository.GetUnreadConversationsCount(userId);
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

                if (areFriends)
                {
                    response.Ok();   
                }
                else
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

    public async Task<ApplicationResponse<StandardMessageDto>> EditMessage(int id, EditMessageDto editMessageDto, int userId)
    {
        var response = new ApplicationResponse<StandardMessageDto>();

        var message = await _messageRepository.GetMessageByIdAsync(id);

        if (message is not null)
        {
            bool belongsToCaller = message.SenderUserId == userId;
            if (belongsToCaller)
            {
                bool canEdit = DateTime.UtcNow - message.CreatedAt > Constants.EditMessageWindow;

                if (canEdit)
                {
                    message.MessageContent = editMessageDto.NewContent;
                    message.LastUpdatedAt = DateTime.UtcNow;
                    await _messageRepository.SaveChangesAsync();

                    var messageEditedDto = _messageMapper.ToMessageEdited(message);
                    await _hubContext.Clients.Group(userId.ToString()).SendAsync("MessageEdited", messageEditedDto);
                    
                    var messageDto = _messageMapper.ToStandardDisplay(message);
                    response.Ok(messageDto, "Message edited successfully");
                }
                else
                {
                    response.Fail("Edit window has expired");
                }
            }
            else
            {
                bool isCallerInConversation = message.ReceiverUserId == userId;
                if (isCallerInConversation)
                {
                    response.Fail("You do not have permission to edit this message");
                }
                else
                {
                    response.Fail("Invalid request");
                }
            }
        }
        else
        {
            response.Fail("Message not found");
        }

        return response;
    }

    public async Task MarkAsReadAsync(int readerId, int otherUserId)
    {
        var (marked, seenAt) = await _messageRepository.MarkConversationAsReadAsync(readerId, otherUserId);

        if (marked)
        {
            await _hubContext.Clients.Group(otherUserId.ToString()).SendAsync("MessageSeen", new { SeenBy = readerId, SeenAt = seenAt});
        }
    }

    public async Task<ApplicationResponse> DeleteMessage(int id, int userId)
    {
        var response = new ApplicationResponse();
        
        var message = await _messageRepository.GetByIdAsync(id);

        if (message is not null)
        {
            bool belongsToCaller = message.SenderUserId == userId;
            if (belongsToCaller)
            {
                await _messageRepository.DeleteAsync(message);
                response.Ok("Message deleted");
            }
            else
            {
                response.Fail("Delete request declined");
            }
        }
        else
        {
            response.Fail("Message not found");
        }

        return response;
    }
}