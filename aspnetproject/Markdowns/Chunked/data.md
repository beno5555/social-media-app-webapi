# AspNetProject — Data Layer

> Part of the AspNetProject doc set. See also: `core.md`, `data.md`, `auth.md`, `controllers.md`.

---

## Repository Layer

### `BaseRepository<T> where T : class`

- `Query()` — `protected virtual IQueryable<T>`; override to apply default `Include` chains (moving away from this and using manual includes per business operation as they become different as they become more and more specific)
- `GetAllAsync(page? pageSize?)` - simple method to fetch all data with optional pagination
- `GetWhereAsync(predicate?, page?, pageSize?, orderBy?, track?)` — composes the three steps; `predicate` is now optional (was required), so pagination-only or order-only calls don't need a no-op filter
- `GetFirstAsync(predicate)` — returns `T?`
- `AddAsync(T entity)`
- `DeleteAsync(T entity)`
- `DeleteWhereAsync(predicate)` — uses `ExecuteDeleteAsync`; bypasses change tracker
- `ExecuteInTransactionAsync(Func<Task> operation)` — wraps operations in a DB transaction; safe to call from any repository since all share the same scoped `DbContext`
- `ClearTracker()` — detaches all tracked entities; used before bulk delete sequences to avoid change tracker conflicts

### `BaseEntityRepository<T> where T : BaseEntity`

Extends `BaseRepository<T>`. Adds:

- `GetByIdAsync(int id)`
- `FindAsync(int id)` — uses `FindAsync` directly without includes; used when a clean untracked instance is needed
- `ExistsAsync(int id)`
- `DeleteAsync(int id)`

### Pagination

`GetWhereAsync` accepts optional `page` and `pageSize`. `Skip`/`Take` is applied and translated to SQL when both are provided. In the API, these values come from query parameters rather than the console app's `PaginatedInput`.
`GetAllAsync` also accepts optional `page` and `pageSize` parameters, applied accordingly.

### SaveChanges Strategy

`SaveChangesAsync` is called in the repository. For atomic multistep operations, wrap in a transaction via `ExecuteInTransactionAsync` at the service layer. `ExecuteDeleteAsync` bypasses the change tracker and does not require `SaveChangesAsync`.

---

## Entities

| Entity     | Properties                                                                                    |
|------------|------------------------------------------------------------------------------------------------|
| User       | Username, Email, PasswordHash, PasswordSalt, Bio, DateOfBirth                                  |
| Post       | UserId, Content                                                                                |
| Comment    | UserId, PostId, Content                                                                        |
| Friendship | RequesterId, AddresseeId, Status (enum: Pending/Accepted/Declined), CreatedAt                  |
| Message    | SenderId, ReceiverId, MessageContent, IsRead, SentAt                                           |
| Log        | *(in progress)* — inherits `BaseEntity`; `LastUpdatedAt` present but unused                    |
| RefreshToken | UserId (FK, cascade delete), TokenHash (MaxLength 44, unique-indexed), ExpiresAt, RevokedAt (nullable) — inherits `BaseEntity`; `LastUpdatedAt` |

`BaseEntity`:

```csharp
public abstract class BaseEntity
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastUpdatedAt { get; set; } = DateTime.UtcNow;
}
```

- `Friendship` and `Message` do not inherit `BaseEntity` — `Friendship`'s PK is `(RequesterId, AddresseeId)`.
- `FriendshipStatus` is stored as a string in the database.
- `User.DateOfBirth` is validated via check constraint. Age must be between 13 and 130.

### FK Cascade Behavior

Unchanged from the console app. SQL Server disallows multiple cascade paths, so only `User → Posts` and `Post → Comments` use `Cascade`; all other relationships use `Restrict` with manual cleanup on account deletion.