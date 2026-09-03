namespace aspnetproject.Infrastructure.Services.BackgroundJobs.Configuration;

public class OldLogsCleanupConfiguration
{
    public TimeSpan Interval      { get; set; } = TimeSpan.FromDays(1);
    public int      RetentionDays { get; set; } = 90;
    public int      BatchSize     { get; set; } = 1000;
}