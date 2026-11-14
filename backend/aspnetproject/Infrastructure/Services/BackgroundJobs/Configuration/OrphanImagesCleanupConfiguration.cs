namespace aspnetproject.Infrastructure.Services.BackgroundJobs.Configuration;

// MinimumAge: files written more recently are skipped (why: OrphanImagesCleanupService.RunCycleAsync). Startup fails
// below MinimumAgeFloor, because zero or a negative age would let a sweep delete the photos of an upload in progress.
public class OrphanImagesCleanupConfiguration
{
    public static readonly TimeSpan MinimumAgeFloor = TimeSpan.FromMinutes(1);

    public const string MinimumAgeTooShortMessage =
        "CleanupConfiguration:OrphanImages:MinimumAge must be at least one minute.";

    public TimeSpan Interval   { get; set; } = TimeSpan.FromHours(24);
    public TimeSpan MinimumAge { get; set; } = TimeSpan.FromHours(1);
    public int      BatchSize  { get; set; } = 500;

    public bool HasSafeMinimumAge()
    {
        return MinimumAge >= MinimumAgeFloor;
    }
}
