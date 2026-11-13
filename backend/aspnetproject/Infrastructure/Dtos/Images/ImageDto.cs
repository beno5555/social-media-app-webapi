namespace aspnetproject.Infrastructure.Dtos.Images;

public class ImageDto
{
    public int    Id         { get; set; }
    public string Address    { get; set; } = string.Empty;
    public int    OrderIndex { get; set; }
}
