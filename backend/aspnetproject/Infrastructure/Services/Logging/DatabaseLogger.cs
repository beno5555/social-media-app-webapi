using System.Security.Claims;
using aspnetproject.Data;
using aspnetproject.Data.Models;
using aspnetproject.Infrastructure.Dtos.DatabaseLogs;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace aspnetproject.Infrastructure.Services.Logging;

public class DatabaseLogger
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly SystemLogger         _systemLogger;

    public DatabaseLogger(
        ApplicationDbContext dbContext,
        IHttpContextAccessor httpContextAccessor,
        SystemLogger systemLogger
        )
    {
        _dbContext = dbContext;
        _httpContextAccessor = httpContextAccessor;
        _systemLogger = systemLogger;
    }

    public async Task LogUserActionAsync(CreateLogDto createLogDto)
    {
        try
        {
            int userId = GetUserId();

            if (userId != 0)
            {
                createLogDto.AuthorizedRequest = true;
                createLogDto.UserId = userId;
            }

            var logToAdd = new Log
            {
                AuthorizedRequest = createLogDto.AuthorizedRequest,
                UserId = createLogDto.UserId,
                Succeeded = createLogDto.Succeeded,
                Action = createLogDto.Action,
                Details = createLogDto.Details,
                EntityId = createLogDto.EntityId,
                EntityName = createLogDto.EntityName,
            };

            await _dbContext.Logs.AddAsync(logToAdd);

            try
            {
                await _dbContext.SaveChangesAsync();
            }
            catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 547 })
            {
                logToAdd.UserId = null;
                await _dbContext.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            await _systemLogger.LoggingErrorAsync(ex, $"Failed to write log entry {createLogDto.Action}");
        }
    }

    public async Task LogSystemActionAsync(CreateLogDto createLogDto)
    {
        var logToAdd = new Log
        {
            AuthorizedRequest = false,
            UserId = null,

            Succeeded = createLogDto.Succeeded,
            Action = createLogDto.Action,
            Details = createLogDto.Details,
            EntityId = createLogDto.EntityId,
            EntityName = createLogDto.EntityName,
        };

        try
        {
            await _dbContext.Logs.AddAsync(logToAdd);
            await _dbContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            await _systemLogger.LoggingErrorAsync(ex, $"Failed to write log entry {createLogDto.Action}");
        }
    }
    
    private int GetUserId()
    {
        var userIdRaw    = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        int.TryParse(userIdRaw, out var userId);
        
        return userId;
    }
}