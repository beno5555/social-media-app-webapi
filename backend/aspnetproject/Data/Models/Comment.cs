using System.ComponentModel.DataAnnotations.Schema;
using aspnetproject.Common.ProjectConstants.Enums;

namespace aspnetproject.Data.Models;

public class Comment : BaseEntity
{
    public string CommentContent { get; set; } = string.Empty;

    public ModerationStatus ModerationStatus { get; set; } = ModerationStatus.Visible;

    public int?  CommenterUserId { get; set; }
    public User? CommenterUser   { get; set; }

    public int   PostId { get; set; }
    public Post? Post   { get; set; }

    [NotMapped]
    public bool IsHidden => ModerationStatus == ModerationStatus.Hidden;
}
