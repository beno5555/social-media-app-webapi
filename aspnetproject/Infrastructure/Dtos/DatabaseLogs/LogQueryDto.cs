namespace aspnetproject.Infrastructure.Dtos.DatabaseLogs;

public class LogQueryDto
{
    public bool? AuthorizedRequest { get; set; }
    public int? UserId            { get; set; }
    
    public bool?    Succeeded  { get; set; }
    
    public string? EntityName { get; set; } 
    public int?    EntityId   { get; set; }
}