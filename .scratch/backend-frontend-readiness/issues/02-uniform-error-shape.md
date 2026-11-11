# 02 - Uniform error shape

Status: ready-for-agent
Difficulty: routine
Blocked by: 01

## Problem

Non-validation failures come back in several shapes, so the frontend cannot read errors one way:

- `AuthController.Refresh` returns `Unauthorized(response.Message)` (a bare string) on a failed refresh, and `Unauthorized(new { Message = "No refresh token provided" })` when the cookie is missing (also a magic literal).
- `GlobalExceptionHandler` writes `{ "error": ... }` (plus `stackTrace` in Development).
- Rate-limit rejections (429) have whatever body `RateLimitConfig` produces; check it.

## What to do

- Make each of these return an `ApplicationResponse` body (`succeeded: false`, `message`). Move literals to `ResponseMessages`.
- In `GlobalExceptionHandler`, keep the 409/500 mapping and keep the stack trace out of non-Development responses.
- Sweep the controllers for any other failure path that does not return the service's `ApplicationResponse`, and fix those too.
- Leave the automatic `[ApiController]` validation responses as they are.
- Update integration tests that assert on the old bodies.

## Done when

- Every non-validation failure body deserializes as `ApplicationResponse`.
- The integration suite passes.
- `CLAUDE.md` and `auth.md` describe the exception handler's real output (covered with ticket 04 if done together).
