using aspnetproject.Data.Models;
using aspnetproject.Infrastructure.Dtos.Messages;
using aspnetproject.Infrastructure.Dtos.Users;
using aspnetproject.Infrastructure.Dtos.WebsocketsTransfer;

namespace aspnetproject.Infrastructure.Mappers;

public static class MessageMapper
{
    public static Message ToEntity(
        int senderId,
        int receiverId,
        CreateMessageDto createMessageDto,
        IReadOnlyList<string> imageAddresses
        )
    {
        return new Message
        {
            SenderUserId = senderId,
            ReceiverUserId = receiverId,
            MessageContent = createMessageDto.Content ?? string.Empty,
            Images = ImageMapper.ToEntities<MessageImage>(imageAddresses)
        };
    }

    public static StandardMessageDto ToStandardDisplay(Message message)
    {
        return new StandardMessageDto
        {
            Id = message.Id,
            Content = message.MessageContent,
            Images = ImageMapper.ToImageDtos(message.Images),

            Sender = new MinimalUserDto
            {
                Id = message.SenderUserId,
                Username = UsernameMapper.ResolveUsername(message.SenderUser, message.SenderUserId)
            },
            ReceiverId = message.ReceiverUserId,
            
            SentAt = message.CreatedAt,
            
            Seen = message.Seen,
            SeenAt = message.SeenAt, 
            IsEdited = message.IsEdited
        };
    }
    
    public static SentMessageDto ToSentMessageDisplay(Message message)
    {
        return new SentMessageDto
        {
            Id = message.Id,
            Content = message.MessageContent,
            SentAt = message.CreatedAt,
            Images = ImageMapper.ToImageDtos(message.Images)
        };
    }

    public static PushMessageDto ToPush(Message message)
    {
        return new PushMessageDto
        {
            Id = message.Id,
            Content = message.MessageContent,
            Images = ImageMapper.ToImageDtos(message.Images),
            SentAt = message.CreatedAt,
            Sender = new MinimalUserDto
            {
                Id = message.SenderUserId,
                Username = UsernameMapper.ResolveUsername(message.SenderUser, message.SenderUserId)
            }
        };
    }

    public static MessageEditedDto ToMessageEdited(Message message)
    {
        return new MessageEditedDto
        {
            Id = message.Id,
            Content = message.MessageContent,
        };
    }
}