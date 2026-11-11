# 04: Reset links accept configured origins

**What to build:** A user who asks for a password reset from the WebCity frontend gets an email whose link returns them to that frontend, wherever it is running. The return address is accepted when its origin is in the configured allowed origins (the same setting that drives the cross-origin policy) and rejected otherwise. Today the check uses a hardcoded set with one address, which is not where the frontend will run. Moving the frontend to a new address then needs a config change only.

**Blocked by:** 01 (Integration suite sends no email)

**Status:** ready-for-agent

**Difficulty:** tricky: this is a security boundary (an open-redirect guard on links WebCity emails to users), and it turns a static helper into an injected service.

- [ ] The redirect check reads the configured allowed origins; no origin is hardcoded
- [ ] The check is a registered concrete class injected where it is used, with no interface
- [ ] It compares scheme and authority, case-insensitively: a trusted host on a different port or scheme is rejected
- [ ] A reset request with an allowed return address succeeds, and the emailed link points at that address
- [ ] A reset request with a disallowed return address is rejected exactly as it is today
- [ ] A reset request with no return address still succeeds and sends a token
- [ ] With no allowed origins configured, every return address is rejected
- [ ] Integration tests cover the allowed, disallowed, case-variant and absent cases, with the allowed origin supplied through the test host's configuration
- [ ] The cross-origin policy and the refresh cookie settings are untouched (ADR 0001)
- [ ] The full integration suite passes
