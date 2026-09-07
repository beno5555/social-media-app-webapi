# AspNetProject — Data Layer

> Part of the AspNetProject doc set. See also: `README.md`, `data.md`, `auth.md`, `controllers.md`, `signalr.md`, `background-jobs.md`.

---

## Repository Layer

### `BaseRepository<T> where T : class`

- `Query(bool track = true)` — `protected virtual IQueryable<T>`; override to apply default `Include` chains. Takes an explicit tracking flag (`AsNoTracking()` when `false`) rather than a separate method.
- `GetAllAsync(pageNumber?, pageSize?)` — fetch all data with optional pagination.
- `GetWhereAsync(predicate?, pageNumber?, pageSize?, orderBy?, ignoreQueryFilters?)` — `protected`, composes filter/order/paging; `ignoreQueryFilters` explicitly bypasses global query filters (e.g. to include deactivated/deleted users in a friendship or conversation lookup).
- `GetFirstAsync(predicate)` — `protected`, returns `T?`.
- `AddAsync(T entity)` / `DeleteAsync(T entity)` — save immediately.
- `DeleteWhereAsync(predicate)` — `protected`; uses `ExecuteDeleteAsync`, bypasses change tracker.
- `ExistsAsync(predicate)` — `protected`.
- `ExecuteInTransactionAsync(Func<Task> operation)` — wraps operations in a DB transaction; safe to call from any repository since all share the same scoped `DbContext`.
- `GetSingleByIgnoringQueryFilterAsync` / `GetWhereByIgnoringQueryFilterAsync` / `ExistsByIgnoringQueryFilterAsync` — `protected`; explicit query-filter-bypassing variants, used where a lookup legitimately needs to see deactivated/deleted rows (e.g. resolving a user by email during registration, or finding a deactivated account to reactivate).
- `SaveChangesAsync()` — public passthrough to `_dbContext.SaveChangesAsync()`.

### `BaseEntityRepository<T> where T : BaseEntity`

Extends `BaseRepository<T>`. Adds:

- `GetByIdAsync(int id)` — uses `_dbSet.FindAsync(id)`.
- `DeleteWithoutChangeTrackingAsync(int id)` — `DeleteWhereAsync(e => e.Id == id)`.
- `ExistsByIdAsync(int id)`.
- `ClearTracker()` — detaches all tracked entities; lives here (not on `BaseRepository<T>`) since it's only meaningful for entities with an `Id`-based identity, used before soft-delete's related-data cleanup sequence.

### Pagination

`GetWhereAsync`/`GetAllAsync` accept optional `pageNumber`/`pageSize`; both null skips pagination (used by background jobs / internal callers). At the API layer, `PageQuery` now defaults `PageNumber = 1` and `PageSize = Constants.DefaultPageSize` (10) with `[Range(1, Constants.MaxPageSize)]` (100) validation — no longer unbounded/unvalidated.

### SaveChanges Strategy

`SaveChangesAsync` is generally called at the point of mutation. For atomic multistep operations (e.g. soft-delete), wrap in a transaction via `ExecuteInTransactionAsync` at the service layer. `ExecuteDeleteAsync`/`ExecuteUpdateAsync` bypass the change tracker and commit directly, without a separate `SaveChangesAsync` call.

---

## Entities

| Entity       | Key properties                                                                                                                                                                                                                                                                                  |
|--------------|-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| User         | Username, Email, PasswordHash, PasswordSalt, Bio, DateOfBirth, `UsernameLastChangedAt`, `LastOnlineAt` (null = online), `ResetTokenHash`/`ResetTokenExpiresAt` (shared by password-reset and account-activation flows), `AccountDeactivatedAt`, `AccountDeletedAt`, computed `IsAccountEnabled` |
| Post         | UserId, PostTitle, PostContent                                                                                                                                                                                                                                                                  |
| Comment      | CommenterUserId (nullable), PostId, CommentContent                                                                                                                                                                                                                                              |
| Friendship   | RequesterUserId, AddresseeUserId, FriendshipStatus (enum: Pending/Accepted/Declined, stored as string), SentAt — now inherits `BaseEntity` (has its own `Id`; `(RequesterUserId, AddresseeUserId)` is a unique index, not the PK)                                                               |
| Message      | SenderUserId, ReceiverUserId, MessageContent, Seen, SeenAt (nullable), computed `IsEdited => LastUpdatedAt is not null`                                                                                                                                                                         |
| Log          | AuthorizedRequest, UserId (nullable), Succeeded, Action, Details (nullable), EntityName, EntityId (nullable)                                                                                                                                                                                    |
| RefreshToken | UserId (FK, cascade delete), TokenHash (MaxLength 44, unique-indexed), ExpiresAt, RevokedAt (nullable)                                                                                                                                                                                          |
| Role         | Name — seeded rows: `User` (Id 1), `Administrator` (Id 2)                                                                                                                                                                                                                                       |
| UserRole     | UserId, RoleId — composite PK, no `BaseEntity`                                                                                                                                                                                                                                                  |

`BaseEntity`:

```csharp
public class BaseEntity
{
    public int Id { get; set; }

    [Column(TypeName = "datetime2(3)")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column(TypeName = "datetime2(3)")]
    public DateTime? LastUpdatedAt { get; set; } = null;
}
```

- `LastUpdatedAt` is nullable and defaults to `null` (not DB-defaulted) — stamped explicitly by service code on update, and doubles as a semantic flag (`Message.IsEdited`).
- `Friendship` now inherits `BaseEntity` (unlike the earlier composite-PK design) — it has an `Id`, but controllers still route by the other user's id, not by `Friendship.Id`.
- `UserRole` does not inherit `BaseEntity` — composite PK `(UserId, RoleId)`.
- `FriendshipStatus` is stored as a string in the database, lives under `Common/ProjectConstants/Enums`.
- `User.DateOfBirth` is validated via a DB check constraint (`CK_User_DateOfBirth`, 13–100 years) and, at the DTO layer, via `ValidAgeAttribute` (same bounds, `Constants.MinAge`/`MaxAge`).

### Account lifecycle fields on `User`

- `AccountDeactivatedAt` (nullable) — set by deactivation, cleared by reactivation. Reversible.
- `AccountDeletedAt` (nullable) — set by soft-delete. Not directly reversible through the API; a background job purges these rows (and dependent data) past a retention window.
- Global query filter: `HasQueryFilter(u => u.AccountDeactivatedAt == null && u.AccountDeletedAt == null)` — both states are hidden from normal queries by default; repository methods that need to see them explicitly use `IgnoreQueryFilters()` (via the `*ByIgnoringQueryFilterAsync` helpers or direct calls).
- `ResetTokenHash`/`ResetTokenExpiresAt` are reused for both password-reset tokens and account-activation tokens — same fields, different issuing/consuming service methods, cleared on successful use.

### FK Cascade Behavior

SQL Server disallows multiple cascade paths, so most user-referencing FKs are non-nullable with `Restrict`/`NoAction`, requiring explicit cleanup before a user row is removed:

- `Post.UserId`, `Message.SenderUserId`/`ReceiverUserId`, `Friendship.RequesterUserId`/`AddresseeUserId` → `NoAction`/`Restrict`, non-nullable. Service/background-job code must explicitly delete a user's posts/messages/friendships before the user row itself.
- `Comment.CommenterUserId` → `SetNull`, nullable — the one FK that tolerates a "deleted account" comment author, since displaying an orphaned comment is a real UX case; other resources don't have an equivalent display need.
- `Post → Comment` → `Cascade` (deleting a post deletes its comments).
- `RefreshToken.UserId` → `Cascade`.
- `UserRole` → `Cascade` on both `UserId` and `RoleId`.
- `Log.UserId` → `SetNull`, nullable (logs outlive the user they were about).

See `background-jobs.md` for how the soft-deleted-user purge job walks these dependencies in order (messages → friendships → posts → user).
