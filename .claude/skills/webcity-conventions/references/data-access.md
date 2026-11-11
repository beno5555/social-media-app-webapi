# Data Access

## 1. Generic base, named domain methods

The base repository keeps its generic helpers `protected`. Callers use methods named for their intent.

```csharp
public async Task<List<Post>> GetFeedAsync(List<int> friendIds, int? pageNumber, int? pageSize)
{
    return await GetWhereAsync(post => friendIds.Contains(post.UserId), pageNumber, pageSize,
        query => query.OrderByDescending(post => post.CreatedAt));
}
```

## 2. Load relations explicitly per operation

Avoid (navigation assumed loaded after insert, so `!` hides a runtime null):
```csharp
await AddAsync(post);
return PostMapper.ToStandardDisplay(post);   // post.User is null
```

Prefer:
```csharp
await AddAsync(post);
return (await _dbSet.Include(postFromDb => postFromDb.User)
                    .FirstOrDefaultAsync(postFromDb => postFromDb.Id == post.Id))!;
```

Use `AsSplitQuery()` when including several collections. Use `AsNoTracking()` for reads that will not be modified.

## 3. Use set-based operations for bulk changes

These bypass the change tracker and need no `SaveChangesAsync()`.

```csharp
await _dbSet.Where(token => token.UserId == userId && token.RevokedAt == null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(token => token.RevokedAt, DateTime.UtcNow));
```

## 4. Batch large purges

Select a capped batch of ids, delete by that id set, and loop until a batch returns zero.

```csharp
int deletedInBatch;
do
{
    var ids = await _dbSet.Where(log => log.CreatedAt < cutoff).Select(log => log.Id).Take(batchSize).ToListAsync();
    deletedInBatch = await _dbSet.Where(log => ids.Contains(log.Id)).ExecuteDeleteAsync();
}
while (deletedInBatch > 0);
```

## 5. Multistep writes run in one transaction, dependents first

```csharp
await _postRepository.ExecuteInTransactionAsync(async () =>
{
    await _commentRepository.DeletePostCommentsAsync(post.Id);
    await _postRepository.DeleteWithoutChangeTrackingAsync(post.Id);
});
```

## 6. Soft delete with a global filter and named bypasses

```csharp
builder.HasQueryFilter(user => user.AccountDeactivatedAt == null && user.AccountDeletedAt == null);

protected async Task<T?> GetSingleByIgnoringQueryFilterAsync(Expression<Func<T, bool>> predicate)
{
    return await _dbSet.IgnoreQueryFilters().FirstOrDefaultAsync(predicate);
}
```

Raw SQL bypasses query filters, so repeat the filter conditions in the SQL by hand.

## 7. Time and sentinels

- All timestamps are UTC (`DateTime.UtcNow`) and stored with millisecond precision.
- `CreatedAt` defaults on creation.
- `LastUpdatedAt` is nullable and stamped explicitly by the service on update. It can also act as a flag.

```csharp
post.LastUpdatedAt = DateTime.UtcNow;

[NotMapped]
public bool IsEdited => LastUpdatedAt is not null;

public DateTime? LastOnlineAt { get; set; } // null = online
```

## 8. Paginate every list endpoint through a shared query object

```csharp
public class PageQuery
{
    public int PageNumber { get; set; } = 1;

    [Range(1, Constants.MaxPageSize)]
    public int PageSize { get; set; } = Constants.DefaultPageSize;
}
```

Unpaginated access (`null`/`null`) is for internal callers and background jobs only.

## 9. Entity configuration

Use one configuration class per entity. Store enums as strings. Comment every non-cascading foreign key.

```csharp
builder.Property(friendship => friendship.FriendshipStatus).HasMaxLength(30).HasConversion<string>();

// SQL Server disallows multiple cascade paths; services delete friendships before the user.
builder.HasOne(friendship => friendship.RequesterUser)
    .WithMany()
    .HasForeignKey(friendship => friendship.RequesterUserId)
    .OnDelete(DeleteBehavior.NoAction);
```
