using System.ComponentModel.DataAnnotations.Schema;

namespace aspnetproject.Data.Models;

public class Message : BaseEntity
{
    public string MessageContent { get; set; } = string.Empty;

    [Column(TypeName = "datetime2(3)")]
    public DateTime? SeenAt { get; set; }
    public bool Seen { get; set; } = false;

    public int   SenderUserId { get; set; }
    public User? SenderUser   { get; set; }

    public int   ReceiverUserId { get; set; }
    public User? ReceiverUser   { get; set; }

    [NotMapped]
    public bool IsEdited => LastUpdatedAt is not null; 
}