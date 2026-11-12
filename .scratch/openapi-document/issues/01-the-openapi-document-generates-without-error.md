# 01: The OpenAPI document generates without error

**What to build:** In Development, the API's OpenAPI document loads instead of failing with 500. Swagger UI's prefilled register, create-account and edit-user bodies still carry a date of birth that passes the age check, shown in UTC.

The cause is known. Those three request bodies declare their example date of birth as text, and the built-in OpenAPI generator cannot write text as a date, so it throws. Swagger UI uses a separate generator and still works. The example must stay valid, because without it Swagger UI prefills today's date, which the minimum-age check rejects.

**Blocked by:** None (can start immediately)

**Status:** ready-for-agent

**Difficulty:** routine: one known cause, and a test proves the fix

- [ ] The OpenAPI document returns 200 and includes the three request bodies
- [ ] A test generates the document and fails if generation throws
- [ ] Each prefilled body, sent as-is from Swagger UI, passes validation
- [ ] The full integration suite passes
