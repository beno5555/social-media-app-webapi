namespace aspnetproject.Infrastructure.Services.BackgroundJobs.Configuration;

public class RefreshTokenCleanupConfiguration
{
    public TimeSpan Interval  { get; set; } = TimeSpan.FromHours(1);
    public int      BatchSize { get; set; } = 500;
}