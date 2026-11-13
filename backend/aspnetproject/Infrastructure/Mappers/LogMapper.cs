using aspnetproject.Data.Models;
using aspnetproject.Infrastructure.Dtos.DatabaseLogs;

namespace aspnetproject.Infrastructure.Mappers;

public static class LogMapper
{
    public static LogDisplayDto ToDisplay(Log log)
    {
        return new LogDisplayDto
        {
            Id        = log.Id,
            CreatedAt = log.CreatedAt,

            AuthorizedRequest = log.AuthorizedRequest,
            UserId            = log.UserId,

            Action    = log.Action,
            Succeeded = log.Succeeded,
            Details   = log.Details,

            EntityId   = log.EntityId,
            EntityName = log.EntityName
        };
    }
}
