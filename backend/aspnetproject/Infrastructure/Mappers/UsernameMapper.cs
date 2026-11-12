using aspnetproject.Common.ProjectConstants;
using aspnetproject.Data.Models;

namespace aspnetproject.Infrastructure.Mappers;

public static class UsernameMapper
{
    /// <summary>
    /// assumes applied .Include() on user navigation property
    /// </summary>
    public static string ResolveUsername(User? user, int? userId)
    {
        return userId is null                       ? ResponseMessages.AccountDeletedUsername
            : user is null                          ? ResponseMessages.AccountDeactivatedUsername
            : user.AccountDeletedAt is not null     ? ResponseMessages.AccountDeletedUsername
            : user.AccountDeactivatedAt is not null ? ResponseMessages.AccountDeactivatedUsername
                                                      : user.Username;
    }
}