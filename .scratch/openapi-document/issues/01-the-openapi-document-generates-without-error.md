# 01: The OpenAPI document generates without error

**What to build:** In Development, `GET /openapi/v1.json` returns the API's OpenAPI document. Today it returns 500 with `InvalidCastException: Unable to cast object of type 'System.String' to type 'System.DateTime'`. Swagger UI and `/swagger/v1/swagger.json` still work, because Swashbuckle builds its own document and is not affected.

The cause is `[DefaultValue("2000-08-16T10:10:23.546Z")]` on the `DateTime DateOfBirth` property of `RegisterDto`, `CreateAccountDto` and `EditUserDto`. That attribute stores the value as a string. `Microsoft.AspNetCore.OpenApi` writes a schema's default value with the property's own type, so it tries to unbox the string as a `DateTime` and throws (`JsonNodeSchemaExtensions.ApplyDefaultValue`). The attributes date from 2026-08-29 and were found while checking `dotnet run` for monorepo ticket 01.

The fix may drop the default or give it a real `DateTime` value. Either way it must keep UTC times and leave Swagger UI's other prefilled example values alone.

**Blocked by:** None (can start immediately)

**Status:** ready-for-agent

**Difficulty:** routine: one known cause, three properties, and a check that proves the document generates.

- [ ] `GET /openapi/v1.json` returns 200 with a document that includes the register, create-account and edit-user request bodies
- [ ] A test generates the OpenAPI document and fails if generation throws. The endpoint is mapped only in Development, but `AddOpenApi()` registers its services in every environment, so the test can use those instead of the URL
- [ ] Swagger UI still loads and still prefills the other example values on those three request bodies
- [ ] No other `DateTime` property carries a string `DefaultValue`
- [ ] The full integration suite passes
