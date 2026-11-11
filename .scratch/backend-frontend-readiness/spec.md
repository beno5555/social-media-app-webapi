# Backend frontend-readiness

Status: ready-for-agent

## Problem Statement

WebCity's backend was built and tested without a real frontend. An Angular frontend is about to be built against it, with the full-stack app due on 2026-11-25. An assessment of the backend found things that frontend would trip over, and things that make the backend unsafe to change in the meantime:

- Failures come back in several different shapes, so a client cannot read an error message one way.
- The links WebCity puts in password-reset emails are only accepted for one hardcoded address, which is not where the frontend will run.
- Every run of the integration suite sends real email, so the suite cannot be used freely as a safety net while the backend is being changed.
- The docs that coding agents rely on disagree with the code in several places, so agents act on false statements.

Three bugs found in the same assessment (any user could deactivate any account, two swapped rate-limit policies, a wrong failure body on post creation) and one vulnerable package are already fixed and committed, and are not part of this spec.

## Solution

Make the backend a dependable base for the frontend without changing what WebCity does for its users:

- Every failure that is not an input-validation failure returns the same response envelope, with a human-readable message in the same place.
- The addresses allowed in emailed links come from configuration, the same list that says which origins may call the backend.
- Email sending can be switched off by configuration, and is off whenever the integration suite runs.
- The docs say what the code does, and name the product WebCity.

The frontend and backend are always served from one origin (ADR 0001), so the refresh cookie keeps its strictest setting. Nothing in this spec may loosen that.

## User Stories

1. As a frontend developer, I want every non-validation failure to carry its message in the same field, so that one error handler can show any backend error.
2. As a frontend developer, I want a failed token refresh to return the same envelope as other failures, so that the session-expiry path needs no special parsing.
3. As a frontend developer, I want a refresh attempt with no refresh cookie to return the same envelope, so that a signed-out visitor and an expired session are handled by the same code.
4. As a frontend developer, I want an unexpected server error to return the same envelope, so that the app can show a generic failure message instead of breaking on an unknown shape.
5. As a frontend developer, I want a data conflict reported by the server to return the same envelope with a conflict status, so that I can tell the user their change clashed with existing data.
6. As a frontend developer, I want a rate-limit rejection to return the same envelope, so that the app can tell the user to slow down.
7. As a frontend developer, I want input-validation failures to keep their standard field-by-field shape, so that forms can show errors next to the right field.
8. As a frontend developer, I want to know there are exactly two failure shapes (the envelope and validation errors), so that I can write and test the error handler once.
9. As a frontend developer, I want server error responses outside development to carry no internal detail, so that the app never shows a stack trace to a user.
10. As a developer debugging locally, I want server error responses in development to still include the exception detail, so that I can diagnose failures quickly.
11. As a user who forgot their password, I want the reset email to link to the page of the app I am actually using, so that one click takes me to the reset form.
12. As a user who forgot their password, I want a reset request that names no return address to still send me a reset token, so that the flow works without a link.
13. As a user, I want reset links to be issued only for addresses WebCity trusts, so that nobody can use WebCity's email to send me to a hostile site.
14. As an operator, I want the trusted addresses to come from configuration, so that moving the frontend to a new address needs no code change.
15. As an operator, I want one list to govern both which origins may call the backend and which addresses may appear in emailed links, so that the two can never disagree.
16. As an operator, I want an address that differs from a trusted one only by letter case to be accepted, so that harmless variations do not break password reset.
17. As an operator, I want an address on a trusted host but a different port or scheme to be rejected, so that the allowlist means exactly what it says.
18. As a developer, I want to run the integration suite as often as I like without any email leaving my machine, so that I can use it as a safety net for every change.
19. As a developer, I want a fresh clone to be safe by default when running tests, so that I cannot send real email by forgetting a setting.
20. As a developer, I want flows that send email to behave the same in tests as in production apart from the sending itself, so that the tests still exercise deactivation, password reset and the failed-registration warning.
21. As an operator, I want email sending to stay on unless it is explicitly switched off, so that an existing deployment keeps working without a config change.
22. As a coding agent, I want the docs to state the real HTTP method of every endpoint, so that I write correct client calls and tests.
23. As a coding agent, I want the docs to describe who may deactivate an account as the code enforces it, so that I do not reintroduce a fixed security hole.
24. As a coding agent, I want the docs to name the real cross-origin policy, so that I do not search for one that does not exist.
25. As a coding agent, I want the docs to describe the real shape of error responses, so that I build the frontend error handler correctly.
26. As a coding agent, I want the docs to describe the reset link's return address and how it is checked, so that I can build the forgot-password page.
27. As a coding agent, I want the docs to contain no references to files that do not exist, so that I do not waste effort looking for them.
28. As a coding agent, I want every doc to call the product WebCity, so that the product name and the code project name are never confused.
29. As a coding agent, I want the data and background-job docs checked against the code, so that I can trust them as much as the others.
30. As a developer, I want the config prerequisites list to name every setting the backend needs, so that a fresh clone can be made to run.
31. As a developer, I want the integration suite to pass after each change in this spec, so that the monorepo move starts from a known-good state.

## Implementation Decisions

- **One failure envelope.** Every failure that is not an input-validation failure returns the existing application response envelope: a success flag set to false and a message. This applies to the token-refresh endpoint (both the failed-refresh and the missing-cookie cases), the global exception handler, rate-limit rejections, and any other controller failure path found not to return the service's response.
- **Validation failures are left alone.** The framework's automatic validation response keeps its standard shape. The frontend handles exactly two failure shapes.
- **Exception handler.** It keeps its existing status mapping (conflict for database update conflicts, server error for everything else). Outside development the message is generic. In development the exception detail is still returned, carried in the envelope's message.
- **Rate-limit rejections.** They currently return a bare status with an empty body; they gain the envelope with a fixed message. The status code does not change.
- **No magic literals.** Messages introduced or touched here live with the other response messages, not inline.
- **Redirect allowlist.** The check that guards the return address in password-reset emails reads the configured allowed origins (the same setting that drives the cross-origin policy) instead of a hardcoded set. It compares scheme and authority, case-insensitively, as today.
- **Redirect check becomes an injected service.** It needs configuration, so it changes from a static helper to a registered concrete class, injected where it is used. No interface, in line with the project's rule.
- **Email switch.** The email configuration gains an on/off setting that defaults to on. When off, the email sender sends nothing and reports success, so calling flows behave exactly as when a send succeeds.
- **Tests force the switch off.** The integration test host sets the switch off itself, so the outcome does not depend on a gitignored settings file.
- **Docs follow the code.** Where a doc and the code disagree on an HTTP method, the doc is corrected. The HTTP methods of logout and the two password-reset endpoints are not changed.
- **Product name.** Docs are retitled to WebCity, per the glossary. The code project and namespace keep their current name.
- **Docs stay where they are.** The per-area docs are corrected in place. Their move to a root docs folder belongs to the monorepo restructure.
- **Single origin.** ADR 0001 stands: nothing here enables cross-origin calls as a fix, and the refresh cookie's settings are not touched.

## Testing Decisions

- **One seam.** All behavior in this spec is tested through the existing HTTP integration seam: the test host built on the web application factory, issuing real HTTP requests against the app wired to the real test database. No new seam is introduced.
- **What makes a good test here.** A test sends a request and asserts on the status code and the response body a client would see. It does not reach into services, check which method was called, or assert on log rows.
- **Failure envelope.** Tests assert that the body of each affected failure deserializes as the envelope with the success flag false and a non-empty message: failed refresh, refresh without a cookie, and at least one rate-limit rejection. Existing tests that assert on the old bodies are updated.
- **Exception handler.** Covered only if an unhandled exception can be provoked through a real endpoint without adding test-only code to the app. If it cannot, it is verified by reading the response in a manual run, and the ticket says so.
- **Redirect allowlist.** Tests request a password reset with an allowed return address (accepted), a disallowed one (rejected), a case-variant of an allowed one (accepted), and none at all (accepted). The allowed origin is supplied through the test host's configuration.
- **Email switch.** The proof is that the full suite passes with the switch off and no email is sent. Flows that send email (deactivation, password reset, failed-registration warning) must still return their normal success responses. No test sends real email to prove the "on" path.
- **Docs.** Not tested; verified by review against the code.
- **Prior art.** The existing controller test classes: one class per controller, tests named `Method_Condition_ExpectedResult`, grouped by endpoint, relying on the deterministic seed data described in comments at the top of each class, with the shared authentication helper for signing in.

## Out of Scope

- The frontend itself, including its dev-server proxy, API services and error handler. Decisions already made for it are listed under Further Notes.
- Deployment. It is not required for the deadline. If time is left: one DigitalOcean Droplet, Docker Compose, nginx, and a DuckDNS subdomain.
- Switching the database to PostgreSQL. WebCity stays on SQL Server through the deadline. A later switch must account for the raw SQL in the conversation-list query, the test database reset setup, and case-sensitive text comparison affecting usernames, emails and search.
- Changing logout and the two password-reset endpoints to `POST`.
- A total count in paged list responses.
- Links instead of raw tokens in the reactivation and failed-registration emails.
- Generating a client from the OpenAPI document, and correcting the declared response types that would require.
- The monorepo restructure and moving the docs.

## Further Notes

- **Decisions carried into the frontend:** it calls only relative URLs under `/api` and `/hubs`; its dev server proxies those two paths to the backend; its API services and DTO types are hand-written, with generic response types mirroring the backend's; lists use infinite scroll, where a page shorter than the page size means the end.
- **Guardrails that keep WebCity deployable:** the frontend calls only relative URLs; no environment value is hardcoded; the refresh cookie is never loosened for local convenience.
- **Monorepo layout:** `backend/`, `frontend/` and `docs/` inside the current root folder. The root folder itself is never renamed or relocated, because Claude Code's project memory is keyed to its path.
- **Order:** the email switch comes first, because it makes the integration suite safe to run as the baseline for everything else.
- **Config files are gitignored.** Any new setting must be added to the prerequisites list in the project instructions, and its value never printed or committed.
