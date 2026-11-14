namespace aspnetproject.Data.Models;

public class PostImage : BaseImage
{
    public int   PostId { get; set; }
    public Post? Post   { get; set; }
}
