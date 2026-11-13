using System.Text.Json.Serialization;
using aspnetproject.Common.ProjectConstants.Enums;

namespace aspnetproject.Infrastructure.Dtos.Users;

// An administrator's view of an account in any state, including the ones every other read hides.
public class AdminUserDto
{
    public int    Id              { get; set; }
    public string Username        { get; set; } = string.Empty;
    public string Email           { get; set; } = string.Empty;
    public bool   IsAdministrator { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter<AccountState>))]
    public AccountState State { get; set; }

    public DateTime  RegisteredAt  { get; set; }
    public DateTime? DeactivatedAt { get; set; }
    public DateTime? DeletedAt     { get; set; }
}
