using System.Text.Json.Serialization;

namespace aspnetproject.Infrastructure.Dtos.Auth;

public class AuthResultDto
{
    public string AccessToken { get; set; } = string.Empty;
    
    [JsonIgnore] // refresh tokens are only stored as cookies in the browser, not returned in response bodies.
    public string      RefreshToken           { get; set; } = string.Empty;
}