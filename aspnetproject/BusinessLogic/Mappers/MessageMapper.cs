using aspnetproject.BusinessLogic.Dtos.MessageDtos;
using aspnetproject.BusinessLogic.Mappers.Base;
using aspnetproject.Common.Dtos.Messages;
using aspnetproject.Models;

namespace aspnetproject.BusinessLogic.Mappers;

public class MessageMapper 
{
    public Message ToEntity(int senderId, CreateMessageDto createMessageDto)
    {
        return new Message
        {
            ReceiverUserId = createMessageDto.ReceiverId,
            SenderUserId = senderId,
            MessageContent = createMessageDto.MessageContent
        };
    }

    public DisplayMessageDto ToDisplay(Message message)
    {
        return new DisplayMessageDto(
            message.MessageContent,
            message.SenderUser!.Username,
            message.CreatedAt,
            message.IsRead);
    }
}