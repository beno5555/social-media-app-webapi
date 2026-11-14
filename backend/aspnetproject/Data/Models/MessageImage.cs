namespace aspnetproject.Data.Models;

public class MessageImage : BaseImage
{
    public int      MessageId { get; set; }
    public Message? Message   { get; set; }
}
