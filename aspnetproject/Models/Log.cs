namespace aspnetproject.Models;

public class Log : BaseEntity
{
    public bool AuthorizedRequest { get; set; }

    public int?  UserId { get; set; }
    public User? User   { get; set; }

    public bool Succeeded { get; set; }

    public string  Action  { get; set; } = string.Empty;
    public string? Details { get; set; }

    public string? EntityName { get; set; } = string.Empty;
    public int?    EntityId   { get; set; }
}