using aspnetproject.Common.Domain;
using aspnetproject.Common.ProjectConstants;
using aspnetproject.Common.ProjectConstants.Enums;
using aspnetproject.Data.Models;
using aspnetproject.Data.Repositories;
using aspnetproject.Hubs;
using aspnetproject.Infrastructure.Dtos.Messages;
using aspnetproject.Infrastructure.Dtos.Users.Friends;
using aspnetproject.Infrastructure.Mappers;
using aspnetproject.Infrastructure.Services.BusinessLogic.Base;
using aspnetproject.Infrastructure.Services.Logging;
using Microsoft.AspNetCore.SignalR;
using Constants = aspnetproject.Common.ProjectConstants.Constants;

namespace aspnetproject.Infrastructure.Services.BusinessLogic.Content;

public class MessageService : BaseService
{
    private readonly MessageRepository    _messageRepository;
    private readonly UserRepository       _userRepository;
    private readonly FriendshipRepository _friendshipRepository;
    private readonly IHubContext<MessageHub> _hubContext;

    public MessageService(
        MessageRepository    messageRepository,
        UserRepository       userRepository,
        FriendshipRepository friendshipRepository,
        IHubContext<MessageHub> hubContext,
        DatabaseLogger          dbLogger
            ) : base(dbLogger)
    {
        _messageRepository = messageRepository;
        _userRepository = userRepository;
        _friendshipRepository = friendshipRepository;
        _hubContext = hubContext;
    }

    public async Task<ApplicationResponse<SentMessageDto>> SendMessageAsync(int senderId, int receiverId, CreateMessageDto createMessageDto)
    {
        var response = new ApplicationResponse<SentMessageDto>();
        
        var friendshipCheck = await ValidFriendship(senderId, receiverId);
        
        if (friendshipCheck.Succeeded)
        {
            var messageToAdd = MessageMapper.ToEntity(senderId, receiverId, createMessageDto);
            var message      = await _messageRepository.AddMessageAsync(messageToAdd);

            var pushMessageDto = MessageMapper.ToPush(message);
            await _hubContext.Clients.Group(receiverId.ToString()).SendAsync("ReceiveMessage", pushMessageDto);

            var displayMessageDto = MessageMapper.ToSentMessageDisplay(message);
            response.Ok(displayMessageDto, ResponseMessages.MessageSent);
        }
        else
        {
            response.Fail(friendshipCheck.Message);
            await LogResultAsync(response.Succeeded, nameof(SendMessageAsync), nameof(Message), $"Could not send a message: {response.Message}", null);
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
        var callerExists = await _userRepository.ExistsByIdAsync(readerId);

        if (callerExists)
        {
            var messages = await _messageRepository.GetConversationAsync(readerId, otherUserId, pageNumber, pageSize);
            messages.Reverse(); // Repository fetches the messages in descending order to fetch the latest ones. we should reverse it.
        
            await MarkAsReadAsync(readerId, otherUserId);
        
            var messageDtos = messages.Select(MessageMapper.ToStandardDisplay).ToList();
            response.Ok(messageDtos, ResponseMessages.ConversationRetrievedSuccessfully);
        }
        else
        {
            response.Fail(ResponseMessages.InvalidRequest);
            await LogResultAsync(response.Succeeded, nameof(GetConversationAsync), nameof(Message), $"Could not fetch conversation of two users: {response.Message}", null);
        }

        return response;
    }
    public async Task<ListResponse<ConversationFriendDto>> GetConversationFriendsAsync(int userId, int? pageNumber, int? pageSize)
    {
        var response = new ListResponse<ConversationFriendDto>();
        
        var conversationFriends = await _userRepository.GetConversationFriendsAsync(userId, pageNumber, pageSize);
        var conversationFriendDtos = conversationFriends.Select(UserMapper.ToConversationFriendDisplay).ToList();
        response.Ok(conversationFriendDtos, ResponseMessages.ConversationFriendsListSuccessMessage);

        return response;
    }
    public async Task<ListResponse<DisplayFriendDto>> GetNonConversationFriendsAsync(int userId, int? pageNumber, int? pageSize) 
    {
        var response = new ListResponse<DisplayFriendDto>();
        
        var friends =
            await _userRepository.GetNonConversationFriendsAsync(userId, pageNumber, pageSize);
        var userDtos = friends.Select(UserMapper.ToFriendDisplay).ToList();
        
        response.Ok(userDtos, ResponseMessages.FriendsWithNoConversationRetrieved);

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
                    response.Fail(ResponseMessages.CanOnlySendMessagesToFriends);
                }
            }
            else
            {
                response.Fail(ResponseMessages.ReceiverUserNotFound);
            }
        }
        else
        {
            response.Fail(ResponseMessages.CannotSendMessagesToOneself);
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

                    var messageEditedDto = MessageMapper.ToMessageEdited(message);
                    await _hubContext.Clients.Group(userId.ToString()).SendAsync("MessageEdited", messageEditedDto);
                    
                    var messageDto = MessageMapper.ToStandardDisplay(message);
                    response.Ok(messageDto, ResponseMessages.MessageEdited);
                }
                else
                {
                    response.Fail(ResponseMessages.EditWindowExpired);
                    await LogResultAsync(response.Succeeded, nameof(EditMessage), nameof(Message), $"Could not edit message: {response.Message}", id);
                }
            }
            else
            {
                bool isCallerInConversation = message.ReceiverUserId == userId;
                if (isCallerInConversation)
                {
                    response.Fail(ResponseMessages.DoNotHavePermissionToEditMessage);
                    await LogResultAsync(response.Succeeded, nameof(EditMessage), nameof(Message), $"Could not edit message. only the sender can edit a message, not a receiver", id);
                }
                else
                {
                    response.Fail(ResponseMessages.InvalidRequest);
                    await LogResultAsync(response.Succeeded, nameof(EditMessage), nameof(Message), $"Could not edit message: only the sender can edit a message, not a receiver, and definitely not the user who is not even in a conversation", id);
                }
            }
        }
        else
        {
            response.Fail(ResponseMessages.MessageNotFound);
            await LogResultAsync(response.Succeeded, nameof(EditMessage), nameof(Message), $"Could not edit message: {response.Message}", id);
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
                response.Ok(ResponseMessages.MessageDeleted);
                await LogResultAsync(response.Succeeded, nameof(DeleteMessage), nameof(Message), null, id);
            }
            else
            {
                response.Fail(ResponseMessages.DeleteRequestDeclined);
                await LogResultAsync(response.Succeeded, nameof(DeleteMessage), nameof(Message), $"Could not delete message. Caller does not have a permission", id);
            }
        }
        else
        {
            response.Fail(ResponseMessages.MessageNotFound);
            await LogResultAsync(response.Succeeded, nameof(DeleteMessage), nameof(Message), response.Message, id);
        }

        return response;
    }
}