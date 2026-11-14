# WebCity — Background Jobs

> Part of the WebCity doc set. See also: `README.md`, `data.md`, `auth.md`, `controllers.md`, `signalr.md`, `background-jobs.md`.

---

## `PeriodicHostedService` (base class)

Abstract `BackgroundService` using a `PeriodicTimer`. Owns an `IServiceScopeFactory`; creates one `IServiceScope` per tick, resolves `DatabaseLogger` from that scope, and catches/logs per-cycle exceptions so one failed cycle doesn't kill the hosted service.

```csharp
protected override async Task ExecuteAsync(CancellationToken stoppingToken)
{
    using var timer = new PeriodicTimer(_interval);

    while (await timer.WaitForNextTickAsync(stoppingToken))
    {
        await RunOnceAsync(stoppingToken);
    }
}

public async Task RunOnceAsync(CancellationToken stoppingToken)
{
    using var scope = _scopeFactory.CreateScope();
    var dbLogger = scope.ServiceProvider.GetRequiredService<DatabaseLogger>();

    try
    {
        await RunCycleAsync(scope.ServiceProvider, stoppingToken);
    }
    catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
    catch (Exception ex)
    {
        await LogResultAsync(dbLogger, false, null, ex.Message);
    }
}

protected abstract Task RunCycleAsync(IServiceProvider serviceProvider, CancellationToken stoppingToken);
```

`RunOnceAsync` is one timer tick: a scope, one cycle, and the failure log. It is public so a test can run a cycle without the timer (see **Testing a job** below).

`RunCycleAsync` receives the scope's `IServiceProvider` (not the raw scope, not pre-resolved services) so derived classes resolve whatever scoped services they need from the same unit of work. `LogResultAsync(dbLogger, succeeded, entityName, details)` builds a `CreateLogDto` tagged `{ServiceName}.RunCycleAsync` and writes via `DatabaseLogger.LogSystemActionAsync` (the system-action variant, not the user-action one — no `HttpContext` exists here).

The base constructor takes `(IServiceScopeFactory, TimeSpan interval)`. Derived services take their own `IOptions<TConfig>` plus `IServiceScopeFactory`, forward the scope factory and `config.Value.Interval` to `base(...)`, and keep `config.Value` in a field for the rest of the settings. Config (interval, retention days, batch size) is bound from `appsettings` per job (`CleanupConfiguration:OldLogs`, `CleanupConfiguration:RefreshTokens`, `CleanupConfiguration:SoftDeletedUsers`, `CleanupConfiguration:OrphanImages`), not hardcoded, so values can change without a rebuild.

All four are registered as hosted services in `ApplicationServicesExtensions.AddApplicationServices`. `OrphanImagesCleanupService` is registered as a singleton first, and its hosted-service registration hands out that singleton, so a test can resolve it.

---

## Testing a job

`CustomWebApplicationFactory` removes every `IHostedService` registration, so no timer runs in a test and a job registered only through `AddHostedService<T>()` cannot be resolved at all. A job that has tests is registered twice:

```csharp
services.AddSingleton<OrphanImagesCleanupService>();
services.AddHostedService(serviceProvider => serviceProvider.GetRequiredService<OrphanImagesCleanupService>());
```

The test host still removes the hosted-service entry, so the job never starts, but the singleton stays. A test resolves it from the host and runs one cycle, the same code a tick runs:

```csharp
var job = _factory.Services.GetRequiredService<OrphanImagesCleanupService>();
await job.RunOnceAsync(CancellationToken.None);
```

Then it asserts on what the cycle changed: files in `_factory.UploadFolder`, rows read back through the API, and the job's `Log` rows through `api/admin/logs?action={JobName}`. A failed cycle is logged, not thrown, so a test that expects nothing to happen should also check that the job wrote no `Log` row. Tests live in `aspnetproject.IntegrationTests/BackgroundJobs/`, one class per job (`OrphanImagesCleanupServiceTests`). The other three jobs still have no tests; to add some, register the job the same way.

---

## `RefreshTokenCleanupService`

Config: `RefreshTokenCleanupConfiguration` — `Interval` (default 1 hour), `BatchSize` (default 500).

Repeatedly batch-deletes (`RefreshTokenRepository.DeleteExpiredAsync`) rows matching `ExpiresAt < utcNow && RevokedAt != null`, looping until a batch returns zero, logging the total removed if any were.

---

## `OldLogsCleanupService`

Config: `OldLogsCleanupConfiguration` — `Interval` (default 1 day), `RetentionDays` (default 90), `BatchSize` (default 1000).

Same batch-and-loop shape (`LogRepository.DeleteLogsAsync`) against `Log` rows older than the retention cutoff.

---

## `SoftDeletedUsersCleanupService`

Config: `SoftDeletedUsersCleanupConfiguration` — `Interval` (default 24 hours), `RetentionDays` (default 20), `BatchSize` (default 300, top-level user batch), `NestedBatchSize` (default 1000, per-dependency batch).

Purges `User` rows with `AccountDeletedAt` older than the retention cutoff, and their dependent data — required because most user-referencing FKs (`Post.UserId`, `Message.Sender/ReceiverUserId`, `Friendship.Requester/AddresseeUserId`) are non-nullable `Restrict`/`NoAction` (see `data.md`), so the parent rows can't just cascade away.

Each cycle (`UserRepository.DeleteSoftDeletedUsersBatchAsync`):
1. Selects up to `BatchSize` candidate user ids (`IgnoreQueryFilters()` — the global filter would otherwise hide the very rows being purged; `Take` + `Select` id list, not a direct delete, since dependents must go first).
2. Deletes that batch's messages, then friendships, then posts (their likes cascade), then the likes the users gave on other posts (`PostLike.UserId` does not cascade, see `data.md`) — each via its own internal batch-and-loop (`NestedBatchSize`) against `ExecuteDeleteAsync`, since `ExecuteDeleteAsync` can't be combined with `.Select()` projection and combining with `.Take()` directly is EF-version/translation-dependent, so the reliable form is: select a capped id batch, then a second `ExecuteDeleteAsync` filtered by that id set.
3. Deletes the user batch itself (`IgnoreQueryFilters()` again).
4. Repeats until a batch of users comes back empty.

**Message photo files.** Before the messages go, each batch reads the addresses of their photos; the `MessageImages` rows cascade with the messages. After each batch, `RunCycleAsync` passes the addresses to `ImageStorage.DeleteAsync`, best effort (a file that cannot be removed is logged to `SystemLogger`). This step has no integration test, because the job is not yet registered for tests (see **Testing a job**); it was verified by one manual run, recorded on ticket 06.

Result accumulation: `SoftDeleteCleanupResult` (Users/Posts/Messages/Friendships/Likes deleted counts, plus `MessageImageAddresses`) with an `operator +` for combining per-batch results into a cycle total, logged once at the end if anything was deleted.

**Why `Comment.CommenterUserId` isn't touched here**: it's the one nullable, `SetNull` FK to `User` (see `data.md`) — a comment from a purged user just shows an orphaned/anonymized author, no explicit cleanup step needed. Everything else (messages, friendships, posts, likes given) has no such "show it anyway" case, hence the explicit pre-delete. The like step has no integration test, because the job is not yet registered for tests (see **Testing a job**).

---

## `OrphanImagesCleanupService`

Config: `OrphanImagesCleanupConfiguration` (section `CleanupConfiguration:OrphanImages`, every key optional): `Interval` (default 24 hours), `MinimumAge` (default 1 hour, at least one minute), `BatchSize` (default 500).

The backstop for photo files that no row points at. They are left behind when the process crashes between writing the files and saving their post or message, when a best-effort delete fails (removing a friendship, the purge above, deleting a message) or when the purge stops partway through a batch. The immediate cleanup paths stay: `ImageStorage.SaveWithOwnerAsync` removes the files when saving the owner throws, and each database delete removes its files afterwards. This job catches what those miss.

Each sweep:
1. Takes the cutoff `UtcNow - MinimumAge` and asks `ImageStorage.ListImageAddressesWrittenBefore(cutoff)` for the addresses of the files last written before it. Only files directly in the upload folder whose names `ImageStorage` makes (`{guid}` in `N` format plus `.jpg`, `.png` or `.webp`) count; anything else in the folder, subfolders included, is left alone. A missing folder lists nothing.
2. Splits the addresses into batches of `BatchSize`. For each batch, `ImageRepository.GetReferencedAddressesAsync` returns the ones a `PostImages` or `MessageImages` row still holds (one `UNION` query), and the rest go to `ImageStorage.DeleteAsync`, which returns how many files it removed. A file that cannot be removed is logged to `SystemLogger` and the sweep goes on.
3. Writes one `Log` row (`EntityName` `BaseImage`) with the number of files removed, only if it removed any.

**Why `MinimumAge`.** An upload writes its files before it saves the post or message that refers to them (a file and a row cannot share a transaction), so for a moment a new file has no row. A sweep in that moment would delete a photo that is about to be referenced. The age is the file's last write time, so even a slow upload is measured from when its last byte landed, and a file younger than `MinimumAge` is never a candidate. Any file older than that had the whole window to get its row. The cutoff is taken once, before the listing, so a file written during the sweep is never considered. Zero or a negative age would switch this protection off without a sign, so the options are validated at startup (`AddOptions<OrphanImagesCleanupConfiguration>().Validate(...).ValidateOnStart()`): a `MinimumAge` under `OrphanImagesCleanupConfiguration.MinimumAgeFloor` (one minute) stops the host with an `OptionsValidationException`. It is the codebase's only validated options class so far.

`ImageStorage` stays the only code that lists or deletes uploaded files; the job only decides which ones.

Tests: `OrphanImagesCleanupServiceTests` (see **Testing a job**) cover deleting an old unreferenced file, keeping old files that a post's or a message's photo refers to, keeping a young unreferenced file, leaving files with other names alone, a missing upload folder, the `Log` row with and without removals, and a host that refuses to start with a `MinimumAge` under one minute. A file that cannot be deleted is not tested: a held file blocks deletion on Windows but not on Linux, so the test could not run the same on both.

---

## Logging from background jobs

Background jobs log through `DatabaseLogger.LogSystemActionAsync`, not `LogUserActionAsync` — no `HttpContext`/caller identity exists in a timer-triggered cycle, so `AuthorizedRequest` is always `false` and `UserId` is always `null` for these rows, distinguishing them from request-triggered log rows at a glance.

**Failed cycles.** The base class's catch block calls `LogResultAsync(dbLogger, false, null, ex.Message)`. `Log.EntityName` is nullable, so a failed cycle is written as a `Succeeded = false` row with no entity name. `LogResultAsync` truncates `details` to `Constants.LogDetailsMaxLength` (the `Log.Details` column size) because exception messages, such as SQL errors, can be longer and an oversized insert would be rejected. If the log write itself still fails, `DatabaseLogger` falls back to `SystemLogger`.