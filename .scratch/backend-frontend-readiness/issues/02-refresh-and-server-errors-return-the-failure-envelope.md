# 02: Refresh and server errors return the failure envelope

**What to build:** A client reads the message of a failed token refresh and of an unexpected server error the same way it reads any other WebCity failure: from the application response envelope, with the success flag false and a message. Today a failed refresh returns a bare message object, and the global exception handler returns its own `error` shape. Input-validation failures keep their standard shape and are not touched.

**Blocked by:** 01 (Integration suite sends no email)

**Status:** done

**Difficulty:** routine: small edits to one controller and the exception handler, following the envelope pattern every other endpoint already uses, checked by integration tests.

- [x] A refresh with an invalid, expired or revoked token returns 401 with the envelope
- [x] A refresh with no refresh cookie returns 401 with the envelope
- [x] An unhandled exception returns the envelope, keeping the current status mapping (409 for a database update conflict, 500 otherwise)
- [x] Outside development the exception message is generic and carries no internal detail; in development the exception detail is still returned, in the envelope's message
- [x] Every other controller failure path is checked, and any that does not return the service's response is fixed
- [x] Messages introduced or touched live with the other response messages, not as inline literals
- [x] Integration tests cover both refresh failures by asserting on status and envelope body; tests asserting on the old bodies are updated
- [x] The exception handler is covered by an integration test only if an unhandled exception can be provoked through a real endpoint without adding test-only code to the app; otherwise a comment on this ticket records how it was verified by hand
- [x] The full integration suite passes

## Comments

**Controller audit.** Every failure branch in `aspnetproject/Controllers/**` already returned the service's `response` (`BadRequest(response)` / `NotFound(response)`). `AuthController.Refresh` was the only exception and is fixed. `Unread` and `MarkAsRead` call service methods that cannot fail.

**Exception handler: verified by hand, no integration test.** No real endpoint raises an unhandled exception without test-only code in the app. I called `GlobalExceptionHandler.TryHandleAsync` directly from a throwaway xUnit test (deleted afterwards) with a `DefaultHttpContext`. Results:
- Production, `DbUpdateException` -> 409, `{"succeeded":false,"message":"An unexpected error occurred."}`
- Production, other exception -> 500, same generic envelope
- Development, other exception -> 500, `{"succeeded":false,"message":"<exception message>"}`

In Development the stack trace is no longer returned: the envelope has no field for it, and the ticket asks for the exception detail in the message.

**Expired token.** Not tested separately. An expired token goes through the same `GetActiveByHashAsync` lookup as an invalid or revoked one, so the invalid and revoked tests cover that path.
