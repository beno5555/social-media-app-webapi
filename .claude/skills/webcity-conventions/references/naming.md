# Naming

## 1. Descriptive over short, including lambda parameters

Avoid:
```csharp
var ids = fs.Select(x => x.A == uid ? x.B : x.A).ToList();
```

Prefer:
```csharp
var friendIds = friendships
    .Select(friendship => friendship.RequesterUserId == userId ? friendship.AddresseeUserId : friendship.RequesterUserId)
    .ToList();
```

## 2. Booleans read as predicates

Prefer `userExists`, `belongsToCaller`, `isAdmin`, `canEdit`, `wasLastConnection`. Avoid `flag`, `check`, `result2`, `status` (for a bool).

## 3. Async methods end in `Async`

```csharp
public async Task<ApplicationResponse> DeletePostAsync(int callerId, bool isAdmin, int id)
```

Do not rename existing methods that lack the suffix unless the user asks.

## 4. Fields and members (C#)

```csharp
private readonly PostRepository _postRepository;   // _camelCase private fields
public  string   PostTitle      { get; set; }      // PascalCase members
const   int      MaxPageSize    = 100;              // PascalCase constants
```

## 5. Routes

| Rule | Example |
|---|---|
| `api/` prefix, lowercase, kebab-case, plural nouns | `api/accounts/{id}/assign-administrator` |
| Caller's own resources | `api/posts/mine` |
| Another user's resources | `api/posts/user/{userId}` |
| Typed constraints to disambiguate | `api/users/{id:int}` vs `api/users/{username}` |
| Actions on a resource as sub-segments | `api/friendships/{requesterId}/accept` |

## 6. Name by role, not by type

Avoid `List<Role> roleList` and `string str`. Prefer `assignedRoles` and `usernameQuery`.

## 7. Inconsistencies

When you find an existing naming inconsistency, report it. Do not mass-rename.
> Note: `UploadPost` / `SendRequest` lack the `Async` suffix; left unchanged.
