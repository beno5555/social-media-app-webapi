namespace aspnetproject.Data.Models;

// The shared shape of a stored photo. Not mapped itself: each subclass has its own table and owner.
public abstract class BaseImage : BaseEntity
{
    // Relative address under /api, e.g. /api/images/{file name}.
    public string Address    { get; set; } = string.Empty;
    public int    OrderIndex { get; set; }
}
