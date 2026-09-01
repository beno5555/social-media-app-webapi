namespace aspnetproject.Infrastructure.Services.BackgroundJobs.Configuration;

public class SoftDeleteCleanupOptions
{
    public TimeSpan Interval      { get; set; } = TimeSpan.FromHours(24);
    public int      RetentionDays { get; set; } = 20;
    public int      BatchSize     { get; set; } = 300;
}