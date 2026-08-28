namespace aspnetproject.BusinessLogic.Services.Logging;

public class SystemLogger
{
    private readonly string _logPath;

    public SystemLogger(IWebHostEnvironment environment)
    {
        _logPath = Path.Combine(environment.ContentRootPath, "Logs", "ErrorLogs.txt");
        Directory.CreateDirectory(Path.GetDirectoryName(_logPath)!);
    }

    public void LogError(Exception exception, string? httpMethod = null, string? requestPath = null)
    {
        string error = $"[{DateTime.UtcNow:yyyy-MMMM-dd HH:mm:ss}] (UTC) {httpMethod} {requestPath} \n{exception}\n\n ---  END  ---\n\n\n";
        File.AppendAllText(_logPath, error);
    }
}