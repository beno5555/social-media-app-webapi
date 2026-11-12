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

## 10. Frontend (Angular)

The frontend is an Angular 22 app in `frontend/`. Sections 1 to 9 apply to it; this section adds the Angular rules. The reasons behind the larger choices are in ADRs 0001 and 0003 to 0006.

### Baseline

- Use what the Angular CLI generates: standalone components, no zone.js, Vitest, Prettier. Never add NgModules or zone.js.
- Without zone.js the screen redraws only when a signal changes. Anything a template shows is a `signal` or a `computed`, never a plain field.
- Use the current idiom: `inject()` instead of constructor parameters, `@if` and `@for (...; track ...)` instead of `*ngIf` and `*ngFor`, `input()` and `output()` instead of decorators, `provideHttpClient` instead of `HttpClientModule`.
- Where this section is silent, follow the CLI's generated code.

### Structure and layering

```
frontend/src/app/
  core/       app-wide singletons: session, HTTP plumbing, hub client, languages, error handling
  shared/     reusable presentational components and pipes; nothing routed
  features/   auth/ users/ posts/ comments/ friendships/ messages/ accounts/
```

- Feature folders are named after the backend's controllers. Each holds its pages, components, `…Api` service and DTO types.
- Dependencies point one way: **pages and components → stores → `…Api` services → `HttpClient`**. A page may call an `…Api` directly when no store is involved.
- Only `…Api` services use `HttpClient`. Only the hub client touches the SignalR connection.
- A feature may import another feature's DTO types, `…Api` and stores, never its pages or components. UI that two features need moves to `shared/`.

### Naming

Every class carries a role word, because the CLI no longer adds `Component` or `Service` and bare names would collide with DTO types.

| Kind | Class | File |
|---|---|---|
| Routed page | `FeedPage` | `feed-page.ts`, `.html`, `.css` |
| Reusable component | `PostCard` | `post-card.ts`, `.html`, `.css` |
| HTTP service, one per backend controller | `PostApi` | `post-api.ts` |
| Shared state | `SessionStore` | `session-store.ts` |
| Guard, interceptor | `authGuard`, `authInterceptor` | `auth-guard.ts`, `auth-interceptor.ts` |
| DTO type, the backend's exact name | `FullUserDto` | `full-user-dto.ts` |

- A component is three files (`templateUrl`, `styleUrl`) with the `app-` selector prefix.
- Use full words and one spelling per concept: `userApi`, never `serv`.

### DTO types and constants

- One `type` per file, named exactly as the backend DTO it mirrors, so one search finds both halves. `AppResult` and its `ok` and `fail` helpers share one file.
- Backend enums arrive as numbers. Mirror each as an `as const` object with the same values:

```ts
export const FriendshipStatus = { Pending: 0, Accepted: 1, Declined: 2 } as const;
export type FriendshipStatus = (typeof FriendshipStatus)[keyof typeof FriendshipStatus];
```

- Times are ISO strings in DTO types (see Dates).
- Mirror the backend's limits by hand in one `LIMITS` file. Each group names the backend file it copies.

### API services and results

- Every `…Api` method returns `Promise<AppResult<T>>` and never throws for an expected failure. Callers `await` it and branch on `succeeded`.
- One helper in `core/`, `toAppResult`, converts the `HttpClient` Observable to that promise and maps failures. It is the only place that catches HTTP errors and the only place application code needs RxJS.
- On the frontend, the failure branch of `AppResult` also carries `status: number` and an optional `fieldErrors` map of field name to messages.
- Endpoints that answer without the envelope (login and refresh return `{ accessToken }`) are wrapped into `AppResult` by their `…Api` method.
- URLs are relative (`/api/...`, `/hubs/...`), per ADR 0001. Path literals appear only in `…Api` files.
- `…Api` services hold no state.
- Lists use infinite scroll. A page shorter than the page size is the last one.
- Where a newer request makes an older one stale (user search), ignore every response but the latest.

```ts
@Injectable({ providedIn: 'root' })
export class UserApi {
  private readonly http = inject(HttpClient);

  getMine(): Promise<AppResult<FullUserDto>> {
    return toAppResult(this.http.get<AppResult<FullUserDto>>('/api/users/mine'));
  }
}
```

### Errors

The backend has two failure shapes plus bodiless rejections (`docs/backend/auth.md`). `toAppResult` maps them all:

| Response | Becomes |
|---|---|
| Failure envelope | its `message`, shown as it arrives |
| Validation `400` | `fieldErrors` from its `errors` map, plus a general message |
| Bodiless `401`, `403`, `404`; network failure | a fixed text from the dictionary |

- A form shows field errors beside the fields and the general message above the submit button. An action outside a form shows a toast. Never use `alert()`.
- Unexpected exceptions (bugs) go to one global `ErrorHandler`. Do not add `try`/`catch` to hide them.

### State

- A signal lives in the component that shows it. Leaving the page discards it.
- Move it to a root `…Store` only when more than one page needs it or the hub changes it: the session, presence, the unread count, the open conversation.
- A store exposes read-only signals and the methods that change them. Nothing outside the store writes its signals.
- Use no state library.

### Components, templates and control flow

- Keep components thin. Fetching lives in `…Api` services. A rule that more than one component needs lives in a store or a pure helper.
- Section 3 applies unchanged to `…Api` services, stores, mappers and helpers: single exit, positive `if`, named booleans.
- Component event handlers, guards and interceptors play the controller's role: they may return early, checking success first.
- An event binding calls exactly one component method. Write no assignment and no second statement in a template.
- A page starts loading in `ngOnInit`. A page whose route parameter can change while it stays open loads in an `effect` that reads the input instead.
- Navigation is an `<a routerLink>`. A `<button>` is for actions.
- Leave no commented-out code and no `console.log`.

```ts
@Component({
  selector: 'app-my-profile-page',
  templateUrl: './my-profile-page.html',
  styleUrl: './my-profile-page.css',
})
export class MyProfilePage implements OnInit {
  private readonly userApi = inject(UserApi);

  protected readonly profile = signal<FullUserDto | null>(null);
  protected readonly failureMessage = signal<string | null>(null);

  async ngOnInit(): Promise<void> {
    const result = await this.userApi.getMine();

    if (result.succeeded) {
      this.profile.set(result.data);
      return;
    }

    this.failureMessage.set(result.message);
  }
}
```

### Session and auth

- The refresh token is an HttpOnly cookie that frontend code never reads. The access token lives only in memory, in `SessionStore`. Never write a token to `localStorage` or `sessionStorage`.
- Every page load restores the session by calling `POST /api/auth/refresh` before the first route activates.
- One tested function decodes the signed-in user's id, username and roles, and the token's expiry, from the access token. It must handle the claim names `nameid`, `unique_name`, `role` and `exp`; a `role` that is missing, a string or an array; and a base64url, UTF-8 payload. Decoded values decide only what the UI shows.
- One interceptor adds the `Authorization` header to `/api` calls. On a `401` from any endpoint except login and refresh, it refreshes once and retries the call once. Simultaneous `401`s share one refresh. If the refresh fails, it clears the session and navigates to sign-in.
- Refresh tokens rotate, so tabs take turns refreshing through the Web Locks API (`navigator.locks`).
- Calls are same-origin, so the cookie travels without `withCredentials`. Never fix a cookie problem with CORS settings (ADR 0001).

### Routing

- Every page is lazy (`loadComponent`) and names its title by dictionary key. The route table ends with a not-found route.
- There are three guards: `authGuard` (signed in), `guestGuard` (signed out, for sign-in and register) and `adminGuard`. They are a convenience for the user; the backend enforces access.
- Route parameters arrive as component inputs (`withComponentInputBinding()`), not through `ActivatedRoute` subscriptions.

### Forms

- Use typed reactive forms built with `NonNullableFormBuilder`. Never put `ngModel` or a second binding on the same control.
- Validators take their limits from `LIMITS`.
- Disable the submit button while the form is invalid, and guard the handler as well.
- Show a field's error once the field has been touched. After a failed submit, put the server's `fieldErrors` on the matching controls.

### Real-time

- One hub client in `core/` owns the connection to `/hubs/messages`. It starts after sign-in, stops on sign-out and reconnects automatically.
- On every connect it asks `SessionStore` for a valid access token, which refreshes first when the token has expired.
- It turns each server event into a store update. Components read stores and never touch the connection.
- Event names and payload types mirror `docs/backend/signalr.md`, as constants and DTO types.

### Text and languages

- The UI has a language switch. English and Georgian come first. Russian and Spanish are one file each, added later.
- All text the frontend owns comes from a dictionary: one TypeScript file per language in `core/`. English is the source and every other language is typed against it, so a missing key fails the build.
- `LanguageStore` holds the current language as a signal, remembers it in `localStorage` and exposes the current dictionary as `text()`. Templates read `text().feed.title`. Write no literal UI text in a template or component.
- An entry that needs a number or a plural form is a function. Use `Intl.PluralRules` for plurals.
- Text from the backend (failure and validation messages) is shown as it arrives, in English. Never translate it by matching strings.
- User content is never translated.

### Dates

- The backend sends every time as UTC with a `Z` suffix. Keep times as ISO strings in DTO types and in state.
- Format them only for display, through one shared formatter that follows the current language (built on `Intl.DateTimeFormat`).
- A date of birth is a calendar date. Format it in UTC, or users west of UTC see the previous day.
- **Temporary:** the backend converter that guarantees the `Z` is not built yet (ADR 0005). Do not compensate in the frontend. Delete this line when the converter lands.

### Styling

- **Temporary:** the styling toolset and the visual identity are decided in a separate design session, before the first page is built. Until then, install no UI kit and no CSS framework. Replace this line with the chosen toolset.
- Use exactly one styling toolset. Never mix kits or icon sets.
- Colours, type sizes and spacing come from one token file. Write no literal values in component styles.
- Write no global element styles beyond a reset.
- The typeface must cover Latin, Cyrillic and Georgian.
- Reusable UI lives in `shared/`.

### Tests and linting

- Write Vitest unit tests for code with logic and no template: `…Api` services, `toAppResult`, the token decoder, the refresh flow, guards and pure helpers. Name them as in section 11.
- **Temporary (until the 2026-11-25 release):** write no component DOM tests and no end-to-end tests; verify pages by running the app. Afterwards, a few end-to-end flows come first.
- ESLint runs Angular's recommended rules plus two: no un-awaited promises (`@typescript-eslint/no-floating-promises`) and no `any` (`@typescript-eslint/no-explicit-any`). Prettier owns formatting.
- Lint, tests and build run in the existing CI workflow.

## 11. Tests

```ts
describe("GET /api/posts/feed", () => {
  it("returns 401 without a token", async () => { ... });
  it("shows accepted friends' posts only", async () => { ... });
});
```
