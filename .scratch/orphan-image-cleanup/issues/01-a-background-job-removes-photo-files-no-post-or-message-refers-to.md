# 01: A background job removes photo files no post or message refers to

**What to build:** Photo files can end up in the upload folder with no row pointing at them. That happens when the process crashes between writing the files and saving the post or message, when a best-effort file delete fails (removing a friendship, the purge of soft-deleted users, and later deleting a post), or when the purge stops partway through a batch. Today those files stay on disk forever. A new background job sweeps the upload folder on a timer and deletes every photo file that is older than a minimum age and whose address no `PostImages` row and no `MessageImages` row holds.

The job is a backstop. The immediate cleanup paths stay as they are: the roll-back in `ImageStorage` when saving the owner throws, and the best-effort deletes after a database change. The minimum age keeps the job away from files that an upload in progress has just written but whose post or message is not saved yet.

The job follows the existing cleanup jobs: a `PeriodicHostedService` with its own settings section, batch-and-loop work and one `Log` row per sweep that removed something. `ImageStorage` stays the only code that touches uploaded files, so listing and deleting files goes through it. Finding which addresses are still referenced is a database query.

The integration test host removes every hosted service, so no existing job has a test. This job's sweep must be callable from a test, without the timer, against the test host's temporary upload folder. The test should resolve it from the host's services, not construct it by hand. This is the first job with a test; record the approach in the background-jobs doc so later jobs can follow it.

**Blocked by:** None (can start immediately)

**Status:** ready-for-agent

**Difficulty:** tricky: first background job with a test (new seam while the test host removes hosted services), and a race with uploads still in progress that the minimum age must handle correctly.

- [ ] Settings live under `CleanupConfiguration:OrphanImages`: `Interval` (default 24 hours), `MinimumAge` (default 1 hour) and `BatchSize`, each with a default so existing settings files keep working
- [ ] A sweep deletes a photo file older than `MinimumAge` that no post and no message refers to
- [ ] A sweep keeps a file a post's photo refers to, and a file a message's photo refers to, however old they are
- [ ] A sweep keeps a file nothing refers to that is younger than `MinimumAge`
- [ ] Only files with an accepted image extension (the names `ImageStorage` makes) directly in the upload folder are considered; anything else there is left alone
- [ ] A missing or empty upload folder is a sweep that removes nothing, not a failure
- [ ] A file that cannot be deleted is logged and does not stop the sweep
- [ ] A sweep that removed files writes one `Log` row with the count, through `DatabaseLogger` like the other jobs; a sweep that removed nothing writes none
- [ ] The job is registered with the other hosted services, and the test host still removes it
- [ ] Integration tests cover the delete, keep-referenced and keep-young cases by running one sweep directly
- [ ] `docs/backend/background-jobs.md` describes the job and how its sweep is tested, and the Images section of `docs/backend/controllers.md` names the job as the backstop for files left behind
