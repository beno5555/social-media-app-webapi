# The access token lives in memory, and every page load restores the session by refresh

The refresh token is an HttpOnly cookie (ADR 0001), and the access token arrives in the JSON body of login and refresh. The frontend keeps the access token only in memory, so every page load starts signed out and immediately calls `POST /api/auth/refresh`. The signed-in user's id, username and roles, and the token's expiry, are decoded from the access token itself. We chose memory because a token in browser storage can be read by any injected script and used elsewhere until it expires, and because storage would not remove the refresh at startup (a new tab or an expired token still needs it); it would only add a second path beside it.

## Considered Options

- **`sessionStorage` or `localStorage`**: rejected for the reasons above.
- **Calling `GET /api/users/mine` after every login and refresh to learn who is signed in**: rejected, because it adds a request to every page load and a second point where restoring the session can fail, and the token's expiry has to be read from the token anyway to keep the SignalR connection supplied with a valid one.

## Consequences

- The backend's `Refresh` rate limit must allow a refresh on every page load. At 5 per 10 minutes per IP, the sixth reload is rejected and the user appears signed out. It must be raised (to about 60 per 10 minutes) before the frontend is usable.
- Refresh tokens rotate, so two tabs refreshing at the same moment would leave one of them with a revoked token. Tabs take turns through the browser's Web Locks API.
- The decoder depends on the claim names the backend's token library writes (`nameid`, `unique_name`, `role`, `exp`), and `role` can be missing, a string or an array. A change to `TokenGenerator` must be matched in the decoder.
- Decoded claims decide only what the UI shows. The backend enforces every permission.
