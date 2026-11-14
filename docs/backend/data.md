# WebCity — Data Layer

> Part of the WebCity doc set. See also: `README.md`, `data.md`, `auth.md`, `controllers.md`, `signalr.md`, `background-jobs.md`.

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
- `ClearTracker()` — detaches all tracked entities; lives here (not on `BaseRepository<T>`) since it's only meaningful for entities with an `Id`-based identity. Its one caller is `AccountManagementService.SoftDeleteAccountAsync`, at the start of the soft-delete transaction. Soft-delete itself removes no related data; the background purge job does that later.

### Pagination

`GetWhereAsync`/`GetAllAsync` accept optional `pageNumber`/`pageSize`; both null skips pagination (used by background jobs / internal callers). At the API layer, `PageQuery` now defaults `PageNumber = 1` and `PageSize = Constants.DefaultPageSize` (10), and validates both with `[Range]`: `PageNumber` from 1 to `Constants.MaxPageNumber` (so the offset `(PageNumber - 1) * PageSize` cannot overflow an `int`), `PageSize` from 1 to `Constants.MaxPageSize` (100) — no longer unbounded/unvalidated.

### SaveChanges Strategy

`SaveChangesAsync` is generally called at the point of mutation. For atomic multistep operations (e.g. soft-delete), wrap in a transaction via `ExecuteInTransactionAsync` at the service layer. `ExecuteDeleteAsync`/`ExecuteUpdateAsync` bypass the change tracker and commit directly, without a separate `SaveChangesAsync` call.

---

## Entities

| Entity       | Key properties                                                                                                                                                                                                                                                                                  |
|--------------|-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| User         | Username, Email, PasswordHash, PasswordSalt, Bio, DateOfBirth, `UsernameLastChangedAt`, `LastOnlineAt` (null = online), `ResetTokenHash`/`ResetTokenExpiresAt` (shared by password-reset and account-activation flows), `AccountDeactivatedAt`, `AccountDeletedAt`, computed `IsAccountEnabled` |
| Post         | UserId, PostTitle, PostContent (empty when the post has only photos), Images (`PostImage`)                                                                                                                                                                                                      |
| PostImage    | PostId. Address and OrderIndex come from the abstract `BaseImage` (not mapped itself): Address is relative, `/api/images/{name}`, max `Constants.ImageAddressMaxLength` = 200; OrderIndex follows upload order, from 0. The file itself lives in the upload folder, owned by `ImageStorage`                                                                                     |
| Comment      | CommenterUserId (nullable), PostId, CommentContent                                                                                                                                                                                                                                              |
| PostLike     | PostId, UserId, CreatedAt (when the like was given). Table `PostLikes`; `(PostId, UserId)` is a unique index, so a user likes a post at most once                                                                                                                                                |
| Friendship   | RequesterUserId, AddresseeUserId, FriendshipStatus (enum: Pending/Accepted/Declined, stored as string), SentAt — now inherits `BaseEntity` (has its own `Id`; `(RequesterUserId, AddresseeUserId)` is a unique index, not the PK)                                                               |
| Message      | SenderUserId, ReceiverUserId, MessageContent (empty when the message has only photos), Seen, SeenAt (nullable), Images (`MessageImage`), computed `IsEdited => LastUpdatedAt is not null`                                                                                                      |
| MessageImage | MessageId, plus Address and OrderIndex from `BaseImage`: the same shape and rules as `PostImage`. Table `MessageImages`. At most `ImageStorage:MaxImagesPerMessage` (default 5) per message; never changed after sending                                                                                                  |
| Log          | AuthorizedRequest, UserId (nullable), Succeeded, Action, Details (nullable, max `Constants.LogDetailsMaxLength` = 1000), EntityName (nullable, max 50), EntityId (nullable)                                                                                                                     |
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

### Times

Every time is a UTC `DateTime`, stored in a `datetime2` column, which carries no zone. A value read back from such a column has `DateTimeKind.Unspecified`, and the chat list (`GetConversationFriendsAsync`) is read with raw SQL, past any EF Core mapping. So the zone is added where JSON is written, not where data is read (ADR 0005):

- `UtcDateTimeJsonConverter` (`Common/Json`) writes every `DateTime` and `DateTime?` as ISO 8601 with a `Z` suffix, for example `2026-09-01T10:00:00Z`. It labels the value as UTC and never shifts it, so code must keep writing UTC.
- `AddUtcTimeJson()` (`Extensions/UtcTimeJsonExtensions.cs`) registers it for controller responses (`AddControllers()`) and for hub payloads (`AddSignalR()`).
- Responses written with `WriteAsJsonAsync` (`GlobalExceptionHandler`, the rate-limit rejection) use the separate minimal-API JSON options and do not get the converter. They carry no times today. If one ever does, register the converter there too, and check that both OpenAPI documents still describe `DateTime` as `date-time`, since the built-in generator reads those options.
- Reading is unchanged from the built-in converter: `2000-08-16` and `2000-08-16T00:00:00.000Z` both store midnight on that date. A date of birth therefore reads back as `2000-08-16T00:00:00Z`, and the frontend formats it in UTC.
- Covered by `Serialization/UtcTimeSerializationTests`.

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
- `Post → PostImage` → `Cascade`. The rows go with the post; the image files on disk do not yet.
- `Message → MessageImage` → `Cascade`, so bulk message deletes with `ExecuteDeleteAsync` (a removed friendship, the purge) take the rows with them. The code that deletes messages reads the image addresses first and removes the files afterwards through `ImageStorage`.
- `Post → PostLike` → `Cascade` (deleting a post deletes its likes).
- `PostLike.UserId` → `NoAction`, and the relationship is optional although the column is not nullable. Likes already cascade from `Post`, and a post points at a user, so a cascade from `User` would be a second path. The purge job deletes the likes a user gave itself. Optional, like `Post`'s link to `User`, so a like by a deactivated or soft-deleted user stays readable while the global query filter hides that user (the liker list shows a placeholder name).
- `RefreshToken.UserId` → `Cascade`.
- `UserRole` → `Cascade` on both `UserId` and `RoleId`.
- `Log.UserId` → `SetNull`, nullable (logs outlive the user they were about).

See `background-jobs.md` for how the soft-deleted-user purge job walks these dependencies in order (messages → friendships → posts → likes given → user).
