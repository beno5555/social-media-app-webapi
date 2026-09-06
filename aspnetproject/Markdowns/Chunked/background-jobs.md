# AspNetProject — Background Jobs

> Part of the AspNetProject doc set. See also: `core.md`, `data.md`, `auth.md`, `controllers.md`, `signalr.md`, `background-jobs.md`.

---

## `PeriodicHostedService` (base class)

Abstract `BackgroundService` using a `PeriodicTimer`. Owns an `IServiceScopeFactory`; creates one `IServiceScope` per tick, resolves `DatabaseLogger` from that scope, and catches/logs per-cycle exceptions so one failed cycle doesn't kill the hosted service.

```csharp
protected override async Task ExecuteAsync(CancellationToken stoppingToken)
{
    using var timer = new PeriodicTimer(_interval);

    while (await timer.WaitForNextTickAsync(stoppingToken))
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
}

protected abstract Task RunCycleAsync(IServiceProvider serviceProvider, CancellationToken stoppingToken);
```

`RunCycleAsync` receives the scope's `IServiceProvider` (not the raw scope, not pre-resolved services) so derived classes resolve whatever scoped services they need from the same unit of work. `LogResultAsync(dbLogger, succeeded, entityName, details)` builds a `CreateLogDto` tagged `{ServiceName}.RunCycleAsync` and writes via `DatabaseLogger.LogSystemActionAsync` (the system-action variant, not the user-action one — no `HttpContext` exists here).

Derived services take only their own `IOptions<TConfig>` plus `IServiceScopeFactory` in their constructor, forwarding both to `base(...)`. Config (interval, retention days, batch size) is bound from `appsettings` per job (`CleanupConfiguration:OldLogs`, `CleanupConfiguration:RefreshTokens`, `CleanupConfiguration:SoftDeletedUsers`), not hardcoded, so values can change without a rebuild.

All three are registered as hosted services in `ApplicationServicesExtensions.AddApplicationServices`.

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
2. Deletes that batch's messages, then friendships, then posts — each via its own internal batch-and-loop (`NestedBatchSize`) against `ExecuteDeleteAsync`, since `ExecuteDeleteAsync` can't be combined with `.Select()` projection and combining with `.Take()` directly is EF-version/translation-dependent, so the reliable form is: select a capped id batch, then a second `ExecuteDeleteAsync` filtered by that id set.
3. Deletes the user batch itself (`IgnoreQueryFilters()` again).
4. Repeats until a batch of users comes back empty.

Result accumulation: `SoftDeleteCleanupResult` (Users/Posts/Messages/Friendships deleted counts) with an `operator +` for combining per-batch results into a cycle total, logged once at the end if anything was deleted.

**Why `Comment.CommenterUserId` isn't touched here**: it's the one nullable, `SetNull` FK to `User` (see `data.md`) — a comment from a purged user just shows an orphaned/anonymized author, no explicit cleanup step needed. Everything else (messages, friendships, posts) has no such "show it anyway" case, hence the explicit pre-delete.

---

## Logging from background jobs

Background jobs log through `DatabaseLogger.LogSystemActionAsync`, not `LogUserActionAsync` — no `HttpContext`/caller identity exists in a timer-triggered cycle, so `AuthorizedRequest` is always `false` and `UserId` is always `null` for these rows, distinguishing them from request-triggered log rows at a glance.