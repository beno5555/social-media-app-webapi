namespace aspnetproject.Infrastructure.Services.BackgroundJobs.Configuration;

// MinimumAge: files written more recently are skipped (why: OrphanImagesCleanupService.RunCycleAsync).
public class OrphanImagesCleanupConfiguration
{
    public TimeSpan Interval   { get; set; } = TimeSpan.FromHours(24);
    public TimeSpan MinimumAge { get; set; } = TimeSpan.FromHours(1);
    public int      BatchSize  { get; set; } = 500;
}
