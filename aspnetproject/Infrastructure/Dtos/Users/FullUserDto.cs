namespace aspnetproject.Infrastructure.Dtos.Users;

public class FullUserDto
{
    public int     Id       { get; set; }
    public string  Username { get; set; } = string.Empty;
    public string  Email    { get; set; } = string.Empty;
    public string? Bio      { get; set; }
    
    public DateTime RegisteredAt { get; set; }
    public DateTime DateOfBirth  { get; set; }
}