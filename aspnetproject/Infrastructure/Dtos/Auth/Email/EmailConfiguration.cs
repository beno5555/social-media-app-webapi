namespace aspnetproject.Infrastructure.Dtos.Auth.Email;

public class EmailConfiguration
{
    public string SmtpHost    { get; set; } = string.Empty;
    public int    SmtpPort    { get; set; }
    public string SenderEmail { get; set; } = string.Empty;
    public string SenderName { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}