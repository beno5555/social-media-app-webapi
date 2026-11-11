# 02: Refresh and server errors return the failure envelope

**What to build:** A client reads the message of a failed token refresh and of an unexpected server error the same way it reads any other WebCity failure: from the application response envelope, with the success flag false and a message. Today a failed refresh returns a bare message object, and the global exception handler returns its own `error` shape. Input-validation failures keep their standard shape and are not touched.

**Blocked by:** 01 (Integration suite sends no email)

**Status:** ready-for-agent

**Difficulty:** routine: small edits to one controller and the exception handler, following the envelope pattern every other endpoint already uses, checked by integration tests.

- [ ] A refresh with an invalid, expired or revoked token returns 401 with the envelope
- [ ] A refresh with no refresh cookie returns 401 with the envelope
- [ ] An unhandled exception returns the envelope, keeping the current status mapping (409 for a database update conflict, 500 otherwise)
- [ ] Outside development the exception message is generic and carries no internal detail; in development the exception detail is still returned, in the envelope's message
- [ ] Every other controller failure path is checked, and any that does not return the service's response is fixed
- [ ] Messages introduced or touched live with the other response messages, not as inline literals
- [ ] Integration tests cover both refresh failures by asserting on status and envelope body; tests asserting on the old bodies are updated
- [ ] The exception handler is covered by an integration test only if an unhandled exception can be provoked through a real endpoint without adding test-only code to the app; otherwise a comment on this ticket records how it was verified by hand
- [ ] The full integration suite passes
