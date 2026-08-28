using System.ComponentModel.DataAnnotations.Schema;
using aspnetproject.Models;

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

    public string? Bio { get; set; }

    public ICollection<Log> Logs { get; set; } = [];
    // role
}