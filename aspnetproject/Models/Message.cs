using System.ComponentModel.DataAnnotations.Schema;

namespace aspnetproject.Models;

public class Message : BaseEntity
{
    public string MessageContent { get; set; } = string.Empty;
    public bool   IsRead         { get; set; } = false;

    public int   SenderUserId { get; set; }
    public User? SenderUser   { get; set; }

    public        int   ReceiverUserId { get; set; }
    public        User? ReceiverUser   { get; set; }

    [NotMapped]
    public bool IsEdited => LastUpdatedAt is not null; 
}