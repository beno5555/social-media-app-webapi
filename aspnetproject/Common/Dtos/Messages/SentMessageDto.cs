namespace aspnetproject.Common.Dtos.Messages;

public class SentMessageDto
{
    public int      Id      { get; set; }
    public string   Content { get; set; } = string.Empty;
    public DateTime SentAt  { get; set; }
}