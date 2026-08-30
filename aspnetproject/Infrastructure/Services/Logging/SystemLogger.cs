namespace aspnetproject.Infrastructure.Services.Logging;

public class SystemLogger
{
    private readonly string        _logPath;
    private readonly SemaphoreSlim _writeLock = new(1, 1);

    public SystemLogger(IWebHostEnvironment environment)
    {
        _logPath = Path.Combine(environment.ContentRootPath, "Logs", "ErrorLogs.txt");
        Directory.CreateDirectory(Path.GetDirectoryName(_logPath)!);
    }

    public async Task LogEndpointErrorAsync(Exception exception, string? httpMethod = null, string? requestPath = null)
    {
        await WriteAsync(exception, $"{httpMethod} {requestPath}");
    }

    public async Task LoggingErrorAsync(Exception exception, string? message)
    {
        await WriteAsync(exception, message);
    }

    private async Task WriteAsync(Exception exception, string? context)
    {
        string error = $"[{DateTime.UtcNow:yyyy-MMMM-dd HH:mm:ss}] (UTC) {context}  \n{exception}\n\n ---  END  ---\n\n\n";
        await _writeLock.WaitAsync();
        try
        {
            await File.AppendAllTextAsync(_logPath, error);
        }
        catch (Exception)
        {
            // ignored
        }
        finally
        {
            _writeLock.Release();
        }
    }
}