using aspnetproject.Common.Domain;
using aspnetproject.Common.ProjectConstants;
using aspnetproject.Data.Repositories;
using aspnetproject.Infrastructure.Dtos.DatabaseLogs;
using aspnetproject.Infrastructure.Mappers;
using aspnetproject.Infrastructure.Services.BusinessLogic.Base;
using aspnetproject.Infrastructure.Services.Logging;

namespace aspnetproject.Infrastructure.Services.BusinessLogic;

public class LogService : BaseService
{
    private readonly LogRepository _logRepository;

    public LogService(LogRepository logRepository, DatabaseLogger dbLogger) : base(dbLogger)
    {
        _logRepository = logRepository;
    }

    // A successful read writes no Log row, so reading the logs does not fill them. A reversed date range never
    // reaches here: LogQueryDto.Validate refuses it as a validation error.
    public async Task<ListResponse<LogDisplayDto>> GetLogsAsync(LogQueryDto logQuery)
    {
        var response = new ListResponse<LogDisplayDto>();

        var logs    = await _logRepository.SearchLogsAsync(logQuery);
        var logDtos = logs.Select(LogMapper.ToDisplay).ToList();

        response.Ok(logDtos, ResponseMessages.LogsRetrieved);

        return response;
    }
}
