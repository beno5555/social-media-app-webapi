# Other Languages

Every general rule applies in every language. These rules never change:

- indentation over early returns in domain code
- named boolean conditions
- central constants and messages
- audience-tiered output shapes
- generic security errors
- service-level logging that cannot throw
- tests against an isolated database

## Python

Use type hints everywhere and Pydantic (or similar) at the boundary. Use `snake_case`, and keep constants in a dedicated module.

```python
@dataclass
class AppResult(Generic[T]):
    succeeded: bool
    message: str | None = None
    data: T | None = None


def update_post(post_id: int, caller_id: int, data: UpdatePostInput) -> AppResult[FullPostView]:
    result: AppResult[FullPostView]
    post = post_repository.find_by_id(post_id)

    if post is not None:
        belongs_to_caller = post.user_id == caller_id
        if belongs_to_caller:
            updated = post_repository.update(post_id, data, last_updated_at=datetime.now(timezone.utc))
            result = AppResult(True, messages.POST_UPDATED, to_full_post_view(updated))
        else:
            result = AppResult(False, messages.POST_NOT_FOUND)
    else:
        result = AppResult(False, messages.POST_NOT_FOUND)

    return result
```

## Java / Kotlin

Keep the same layers: controller → service → repository → mapper. Use constructor injection with `private final` fields (Java) or `private val` (Kotlin). Expected failures use a result type, not checked exceptions.

```kotlin
fun updatePost(id: Long, callerId: Long, input: UpdatePostDto): AppResult<FullPostDto> {
    val result: AppResult<FullPostDto>
    val post = postRepository.findByIdOrNull(id)

    if (post != null) {
        val belongsToCaller = post.userId == callerId
        result = if (belongsToCaller) AppResult.ok(postMapper.toFull(postRepository.save(post.apply(input))))
                 else AppResult.fail(Messages.POST_NOT_FOUND)
    } else {
        result = AppResult.fail(Messages.POST_NOT_FOUND)
    }

    return result
}
```

## Go

Idiomatic early `if err != nil { return ..., err }` is acceptable for infrastructure errors. Business-rule branching still builds one result where that stays readable. Follow the existing codebase if it differs.

```go
func (s *PostService) UpdatePost(ctx context.Context, id, callerID int, in UpdatePostInput) (AppResult[FullPostView], error) {
    post, err := s.posts.FindByID(ctx, id)
    if err != nil {
        return AppResult[FullPostView]{}, err // infrastructure error
    }

    var result AppResult[FullPostView]
    belongsToCaller := post != nil && post.UserID == callerID
    if belongsToCaller {
        result = Ok(ToFullPostView(post), messages.PostUpdated)
    } else {
        result = Fail[FullPostView](messages.PostNotFound)
    }

    return result, nil
}
```

## SQL / migrations

Constraints mirror application validation. Name constraints and indexes explicitly. Change schemas only through migrations, never by manual edits on shared databases.

```sql
ALTER TABLE Users ADD CONSTRAINT CK_User_DateOfBirth
    CHECK (DateOfBirth <= DATEADD(year, -13, GETUTCDATE()) AND DateOfBirth >= DATEADD(year, -100, GETUTCDATE()));

CREATE UNIQUE INDEX IX_Friendships_RequesterUserId_AddresseeUserId
    ON Friendships (RequesterUserId, AddresseeUserId);
```
