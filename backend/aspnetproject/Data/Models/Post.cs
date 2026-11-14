using System.ComponentModel.DataAnnotations.Schema;
using aspnetproject.Common.ProjectConstants.Enums;

namespace aspnetproject.Data.Models;

public class Post : BaseEntity
{
    public string PostTitle   { get; set; } = string.Empty;
    public string PostContent { get; set; } = string.Empty;

    public ModerationStatus ModerationStatus { get; set; } = ModerationStatus.Visible;

    public int   UserId { get; set; }
    public User? User   { get; set; } 
    
    public ICollection<Comment>   Comments { get; set; } = new List<Comment>();
    public ICollection<PostImage> Images   { get; set; } = new List<PostImage>();
    public ICollection<PostLike>  Likes    { get; set; } = new List<PostLike>();

    [NotMapped]
    public bool IsHidden => ModerationStatus == ModerationStatus.Hidden;
}