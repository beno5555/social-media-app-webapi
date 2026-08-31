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
    public DateTime? LastOnlineAt { get; set; } = null; // null = online

    public string? Bio { get; set; }

    public string?   ResetTokenHash      { get; set; } = string.Empty;
    public DateTime? ResetTokenExpiresAt { get; set; }

    public DateTime? AccountDeactivatedAt { get; set; }

    [NotMapped] 
    public bool IsAccountEnabled => AccountDeactivatedAt == null;

    public ICollection<Log>      Logs      { get; set; } = [];
    public ICollection<UserRole> UserRoles { get; set; } = [];
}