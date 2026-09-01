using aspnetproject.Infrastructure.Dtos.DatabaseLogs;
using aspnetproject.Infrastructure.Services.Logging;

namespace aspnetproject.Infrastructure.Services.BusinessLogic.Base;

public class BaseService
{
    private readonly DatabaseLogger _dbLogger;
    private readonly string         _serviceName;

    public BaseService(DatabaseLogger dbLogger)
    {
        _dbLogger = dbLogger;
        _serviceName = GetType().Name;
    }

    protected async Task LogResultAsync(
        bool    succeeded,
        string  method,
        string  entityName,
        string? details,
        int?    entityId 
        )
    {
        
        var log = new CreateLogDto
        {
            Succeeded = succeeded,
            Action = $"{_serviceName}.{method}",
            EntityName = entityName,
            
            Details = details,
            EntityId = entityId
        };

        await _dbLogger.LogAsync(log);
    }
}