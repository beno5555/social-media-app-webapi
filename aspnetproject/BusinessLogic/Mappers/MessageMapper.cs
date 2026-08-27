using aspnetproject.BusinessLogic.Mappers.Base;
using aspnetproject.Common.Dtos.Messages;
using aspnetproject.Common.Dtos.Users;
using aspnetproject.Models;

namespace aspnetproject.BusinessLogic.Mappers;

public class MessageMapper 
{
    public Message ToEntity(int senderId, int receiverId, CreateMessageDto createMessageDto)
    {
        return new Message
        {
            SenderUserId = senderId,
            ReceiverUserId = receiverId,
            MessageContent = createMessageDto.MessageContent
        };
    }

    public StandardMessageDto ToStandardDisplay(Message message)
    {
        return new StandardMessageDto
        {
            Id = message.Id,
            Content = message.MessageContent,
            
            Sender = new MinimalUserDto
            {
                Id = message.SenderUserId,
                Username = message.SenderUser!.Username
            },
            ReceiverId = message.ReceiverUserId,
            
            SentAt = message.CreatedAt,
            IsRead = message.IsRead,
            IsEdited = message.IsEdited
        };
    }
    
    public SentMessageDto ToSentMessageDisplay(Message message)
    {
        return new SentMessageDto
        {
            Id = message.Id,
            Content = message.MessageContent,
            SentAt = message.CreatedAt,
        };
    }
}