# Frontend and backend are always served from a single origin

The refresh token travels in an `HttpOnly; Secure; SameSite=Strict` cookie, and browsers only send a `Strict` cookie when the page and the API are on the same site. We therefore serve the Angular frontend and the backend from one origin everywhere: in development the Angular dev server proxies `/api` and `/hubs` to the backend, and in production a reverse proxy (nginx) serves the frontend build and forwards the same two paths. The frontend only ever calls relative URLs.

## Considered Options

- **Separate origins with CORS and `SameSite=None`**: rejected, because it weakens the cookie's protection against cross-site requests purely for hosting convenience.
- **Separate subdomains of one parent domain**: works with `Strict`, but needs careful cookie and CORS setup and gains nothing for a project of this size.

## Consequences

- Do not "fix" a cookie or connection problem by enabling cross-origin calls or loosening `SameSite`; fix the proxy instead.
- `Cors:AllowedOrigins` stays in the backend, but in the intended setup no browser request is cross-origin. It also serves as the allowlist for links placed in emails.
