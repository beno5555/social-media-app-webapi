# DTOs and Mapping

## 1. Shape output by audience

| Tier | Audience | Example fields |
|---|---|---|
| `Minimal` | Lists, search, nested references | Id, Username |
| `Standard` | Public single fetch | + Bio, RegisteredAt |
| `Full` | Owner's own view, freshly created resource | + Email, Roles |

```csharp
public class MinimalUserDto  { public int? Id { get; set; } public string Username { get; set; } = string.Empty; }
public class StandardUserDto { /* Id, Username, Bio, RegisteredAt, DateOfBirth */ }
public class FullUserDto     { /* Standard + Email, Roles */ }
```

Never return entities. Avoid:
```csharp
public async Task<ActionResult<User>> GetUser(int id)   // leaks PasswordHash, ResetTokenHash
```

## 2. Naming

| Kind | Pattern | Examples |
|---|---|---|
| Input | `{Verb}{Entity}Dto` | `CreatePostDto`, `EditUserDto` |
| Output | `{Tier}{Entity}Dto` / `{Tier}{Entity}DisplayDto` | `StandardFriendshipDto`, `FullPostDisplayDto` |
| Query | `{Purpose}Query : PageQuery` | `SearchUserQuery` |
| Projection (raw SQL) | `{Purpose}Projection` | `ConversationFriendProjection` |

## 3. Mappers are static and pure

```csharp
public static class PostMapper
{
    public static Post ToEntity(int userId, CreatePostDto createPostDto)
    {
        return new Post
        {
            UserId      = userId,
            PostTitle   = createPostDto.PostTitle,
            PostContent = createPostDto.PostContent,
        };
    }

    public static StandardPostDisplayDto ToStandardDisplay(Post post) { ... }
    public static FullPostDisplayDto     ToFullDisplay(Post post)     { ... }
}
```

A mapper must not query the database, inject services or mutate its input.

## 4. Validation

Put shape rules on the DTO. Business rules that need the database go in the service.

```csharp
public class EditUserDto
{
    [Required, MinLength(Constants.UsernameMinLength), MaxLength(Constants.UsernameMaxlength)]
    public string Username { get; set; } = string.Empty;

    [ValidAge]   // custom attribute shared by RegisterDto and EditUserDto
    public DateTime DateOfBirth { get; set; }
}
```

## 5. Defaults

Initialize non-nullable strings to `string.Empty` and collections to `[]`. Make optional values nullable explicitly.

```csharp
public string       Title    { get; set; } = string.Empty;
public List<string> Roles    { get; set; } = [];
public string?      Bio      { get; set; }
```
