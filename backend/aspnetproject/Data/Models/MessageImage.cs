namespace aspnetproject.Data.Models;

public class MessageImage : BaseEntity
{
    // Relative address under /api, e.g. /api/images/{file name}.
    public string Address    { get; set; } = string.Empty;
    public int    OrderIndex { get; set; }

    public int      MessageId { get; set; }
    public Message? Message   { get; set; }
}
