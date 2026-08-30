namespace aspnetproject.Infrastructure.Dtos.DatabaseLogs;

public class CreateLogDto
{
    public bool AuthorizedRequest { get; set; }
    public int? UserId            { get; set; }

    public string  Action    { get; set; } = string.Empty;
    public bool    Succeeded { get; set; }
    public string? Details   { get; set; }

    public int?    EntityId   { get; set; }
    public string? EntityName { get; set; }
}