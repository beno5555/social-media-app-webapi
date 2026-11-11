# TypeScript / JavaScript

Every general rule applies. This file shows the TypeScript form of each one.

## 1. Strict typing

Turn on `"strict": true` and never use `any`. Narrow `unknown` instead.

Avoid:
```ts
function parse(body: any) { return body.title; }
```

Prefer:
```ts
function parse(body: unknown): CreatePostInput {
  return createPostSchema.parse(body);
}
```

Use `const` by default. Use `let` only for the single result variable that branches reassign.

## 2. Result type

```ts
export type AppResult<T> =
  | { succeeded: true; message?: string; data: T }
  | { succeeded: false; message: string };

export const ok   = <T>(data: T, message?: string): AppResult<T> => ({ succeeded: true, data, message });
export const fail = <T>(message: string): AppResult<T> => ({ succeeded: false, message });
```

## 3. Control flow

Services use a single exit. Handlers may return early, with success first.

```ts
export async function updatePost(id: number, callerId: number, input: UpdatePostInput): Promise<AppResult<FullPostView>> {
  let result: AppResult<FullPostView>;
  const post = await postRepository.findById(id);

  if (post !== null) {
    const belongsToCaller = post.userId === callerId;
    if (belongsToCaller) {
      const updated = await postRepository.update(id, { ...input, lastUpdatedAt: new Date() });
      result = ok(toFullPostView(updated), responseMessages.postUpdated);
    } else {
      result = fail(responseMessages.postNotFound);
    }
  } else {
    result = fail(responseMessages.postNotFound);
  }

  return result;
}
```

```ts
router.put("/api/posts/:id", requireAuth, rateLimit(policies.updatePost), async (req, res) => {
  const result = await updatePost(Number(req.params.id), req.user.id, parseUpdatePost(req.body));

  if (result.succeeded) {
    return res.status(200).json(result);
  }

  return res.status(404).json(result);
});
```

## 4. Validate at the boundary using shared constants

```ts
export const createPostSchema = z.object({
  title: z.string().min(1).max(LIMITS.postTitleMaxLength),
  content: z.string().min(1).max(LIMITS.postContentMaxLength),
});
```

## 5. Async

Use `async/await`, not `.then()` chains. Never leave a promise floating.

Avoid:
```ts
notifyAuthor(comment);
```

Prefer:
```ts
await notifyAuthor(comment);
```

or, when fire-and-forget is intentional:
```ts
void notifyAuthor(comment); // fire-and-forget: notification failure must not fail the request
```

## 6. Naming

| Kind | Style | Example |
|---|---|---|
| Variables, functions | camelCase | `belongsToCaller`, `getFeed` |
| Types, classes | PascalCase, no `I` prefix | `PostRepository`, `FullPostView` |
| Constants | `as const` objects or SCREAMING_SNAKE | `LIMITS.maxPageSize`, `MAX_PAGE_SIZE` |
| Files | kebab-case (unless the project differs) | `post-service.ts` |

## 7. Mappers and messages

```ts
export const toMinimalUserView = (user: User): MinimalUserView => ({ id: user.id, username: user.username });

export const responseMessages = {
  postUpdated: "Post updated successfully",
  postNotFound: "Post not found",
} as const;
```

## 8. `type` vs `interface`

Use `type` for unions and data shapes. Use `interface` only for extensible contracts. Follow the project's existing choice.

## 9. Formatting

Always use braces. If Prettier or ESLint is configured, its output wins; do not hand-align columns.

## 10. Frontend

The frontend will be Angular. Its specific conventions are not decided yet; settle them when frontend work starts. Until then:

- Keep components thin. Fetching and business rules live in injectable services.
- Follow idiomatic Angular and the Angular CLI's generated structure where this file is silent.
- Never put tokens in `localStorage`. Rely on HttpOnly cookies for refresh tokens.

## 11. Tests

```ts
describe("GET /api/posts/feed", () => {
  it("returns 401 without a token", async () => { ... });
  it("shows accepted friends' posts only", async () => { ... });
});
```
