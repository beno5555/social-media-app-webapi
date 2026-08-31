using System.ComponentModel.DataAnnotations.Schema;

namespace aspnetproject.Data.Models;

public class User : BaseEntity
{
    public string   Username     { get; set; } = string.Empty;
    public string   Email        { get; set; } = string.Empty;
    public DateTime DateOfBirth  { get; set; }  
    public string   PasswordHash { get; set; } = string.Empty;
    public string   PasswordSalt { get; set; } = string.Empty;

    [Column(TypeName = "datetime2(3)")]
    public DateTime? UsernameLastChangedAt { get; set; } = null;
    
    [Column(TypeName = "datetime2(3)")]
    public DateTime? LastActiveAt { get; set; } = null; // null = online

    public string? Bio { get; set; }

    public string?   PasswordResetTokenHash      { get; set; } = string.Empty;
    public DateTime? PasswordResetTokenExpiresAt { get; set; }

    public ICollection<Log>      Logs      { get; set; } = [];
    public ICollection<UserRole> UserRoles { get; set; } = [];
}