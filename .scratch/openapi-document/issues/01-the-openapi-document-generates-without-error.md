# 01: The OpenAPI document generates without error

**What to build:** In Development, the API's OpenAPI document loads instead of failing with 500. Swagger UI's prefilled register, create-account and edit-user bodies still carry a date of birth that passes the age check, shown in UTC.

The cause is known. Those three request bodies declare their example date of birth as text, and the built-in OpenAPI generator cannot write text as a date, so it throws. Swagger UI uses a separate generator and still works. The example must stay valid, because without it Swagger UI prefills today's date, which the minimum-age check rejects.

**Blocked by:** None (can start immediately)

**Status:** done

**Difficulty:** routine: one known cause, and a test proves the fix

- [ ] The OpenAPI document returns 200 and includes the three request bodies
- [x] A test generates the document and fails if generation throws
- [x] Each prefilled body, sent as-is from Swagger UI, passes validation
- [x] The full integration suite passes

## Comments

- Cause fixed with a new `UtcDateTimeDefaultValueAttribute` (a `DefaultValueAttribute` holding a UTC `DateTime`), used on `RegisterDto`, `EditUserDto` and `CreateAccountDto`. Both generators read it; the example stays `2000-08-16T10:10:23.546Z` and does not shift with the server's time zone (checked with `TZ=Pacific/Auckland`).
- Unmet box (left unticked): "includes the three request bodies". `CreateAccountDto` is used by no endpoint, so it is in neither document, and the document can only include `RegisterDto` and `EditUserDto`. The test checks those two; the OpenAPI document returns 200. Owner: either wire `CreateAccountDto` to an endpoint or drop the DTO.
- The OpenAPI endpoints exist only in Development, so `CustomWebApplicationFactory` gained an optional `environmentName` parameter (default Testing).
- The prefilled-body test rebuilds each body from the Swagger document's field defaults and runs full DataAnnotations validation on the deserialized DTO; it does not POST the body.
- Full suite (`scripts/test-linux.sh`): 217 passed, Duration: 43 s.
- spec-review: no scope creep; style notes applied (comment wording, constants in the test).
