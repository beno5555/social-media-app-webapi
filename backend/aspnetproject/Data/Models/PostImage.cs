namespace aspnetproject.Data.Models;

public class PostImage : BaseEntity
{
    // Relative address under /api, e.g. /api/images/{file name}.
    public string Address    { get; set; } = string.Empty;
    public int    OrderIndex { get; set; }

    public int   PostId { get; set; }
    public Post? Post   { get; set; }
}
