namespace aspnetproject.Data.Models;

public class Post : BaseEntity
{
    public string PostTitle   { get; set; } = string.Empty;
    public string PostContent { get; set; } = string.Empty;
    
    public int   UserId { get; set; }
    public User? User   { get; set; } 
    
    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
}