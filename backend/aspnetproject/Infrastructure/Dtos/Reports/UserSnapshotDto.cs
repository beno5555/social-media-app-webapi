namespace aspnetproject.Infrastructure.Dtos.Reports;

// What an administrator sees of a reported user.
public class UserSnapshotDto
{
    public int     Id       { get; set; }
    public string  Username { get; set; } = string.Empty;
    public string? Bio      { get; set; }
}
