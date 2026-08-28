using aspnetproject.Data.Models;

namespace aspnetproject.Models;

public class RefreshToken : BaseEntity
{
    public int       UserId    { get; set; }
    public User?     User      { get; set; }
    public string    TokenHash { get; set; } = string.Empty;
    public DateTime  ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; } // null -> still active
}