namespace aspnetproject.Infrastructure.Services.BackgroundJobs.Configuration;

public class RefreshTokenCleanupConfiguration
{
    public TimeSpan Interval { get; set; } = TimeSpan.FromHours(1);
}