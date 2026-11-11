# 04: Reset links accept configured origins

**What to build:** A user who asks for a password reset from the WebCity frontend gets an email whose link returns them to that frontend, wherever it is running. The return address is accepted when its origin is in the configured allowed origins (the same setting that drives the cross-origin policy) and rejected otherwise. Today the check uses a hardcoded set with one address, which is not where the frontend will run. Moving the frontend to a new address then needs a config change only.

**Blocked by:** 01 (Integration suite sends no email)

**Status:** done

**Difficulty:** tricky: this is a security boundary (an open-redirect guard on links WebCity emails to users), and it turns a static helper into an injected service.

- [x] The redirect check reads the configured allowed origins; no origin is hardcoded
- [x] The check is a registered concrete class injected where it is used, with no interface
- [x] It compares scheme and authority, case-insensitively: a trusted host on a different port or scheme is rejected
- [x] A reset request with an allowed return address succeeds, and the emailed link points at that address
- [x] A reset request with a disallowed return address is rejected exactly as it is today
- [x] A reset request with no return address still succeeds and sends a token
- [x] With no allowed origins configured, every return address is rejected
- [x] Integration tests cover the allowed, disallowed, case-variant and absent cases, with the allowed origin supplied through the test host's configuration
- [x] The cross-origin policy and the refresh cookie settings are untouched (ADR 0001)
- [x] The full integration suite passes

## Comments

- Implemented: `UrlSecurityService` is now a scoped concrete class that reads `Cors:AllowedOrigins` and normalises both the configured entries and the redirect URL to `scheme://authority` before a case-insensitive compare. It is injected into `AuthService`. `CorsExtensions` and the refresh cookie are unchanged.
- The test host replaces `Cors:AllowedOrigins:0` with `CustomWebApplicationFactory.TrustedFrontendOrigin`. Any other entries in the gitignored `appsettings.Testing.json` stay trusted.
- New tests in `AuthControllerTests` cover: allowed, case-variant, absent, and disallowed (untrusted host, trusted host on another port, another scheme, relative URL). A mutation check (comparing host only) made the port and scheme cases fail.
- Two boxes are verified by reading the code, not by a test, because the HTTP seam can't observe them. The emailed link: `RedirectUrl` is passed unchanged into the reset email's `href`. No origins configured: the set is empty, so every URL is rejected.
- Full suite: 121/121 passed.
