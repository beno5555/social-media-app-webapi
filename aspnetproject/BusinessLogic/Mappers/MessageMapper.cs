using aspnetproject.BusinessLogic.Dtos.MessageDtos;
using aspnetproject.BusinessLogic.Mappers.Base;
using aspnetproject.Models;

namespace aspnetproject.BusinessLogic.Mappers;

public class MessageMapper : IMapper<Message, CreateMessageDto, DisplayMessageDto>
{
    public Message ToEntity(CreateMessageDto createMessageDto)
    {
        return new Message
        {
            ReceiverUserId = createMessageDto.ReceiverId,
            SenderUserId = createMessageDto.SenderId,
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