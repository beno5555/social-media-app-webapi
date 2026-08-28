using System.ComponentModel.DataAnnotations.Schema;

namespace aspnetproject.Data.Models;

public class BaseEntity
{
    public int      Id            { get; set; }
    [Column(TypeName = "datetime2(3)")]
    public DateTime CreatedAt     { get; set; } = DateTime.UtcNow;
    
    [Column(TypeName = "datetime2(3)")]
    public DateTime? LastUpdatedAt { get; set; }  = DateTime.UtcNow;
}