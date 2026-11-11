# 03: Rate-limit rejections return the failure envelope

**What to build:** When a client is rate-limited, the 429 response carries the application response envelope with the success flag false and a fixed message, so the frontend's single error handler can tell the user to slow down. Today the rejection is a bare status with an empty body.

**Blocked by:** 01 (Integration suite sends no email)

**Status:** ready-for-agent

**Difficulty:** tricky: the rejection is written in the rate-limiting pipeline rather than a controller, and a test that exhausts a limit shares limiter state with the rest of the suite and can make unrelated tests fail.

- [ ] Every rate-limit rejection, for every policy, returns 429 with the envelope and a non-empty message
- [ ] The message lives with the other response messages
- [ ] Limits, windows and partitioning (per IP before login, per user after) are unchanged
- [ ] An integration test exhausts one policy and asserts on the 429 status and envelope body
- [ ] That test cannot cause other tests to be rate-limited, whatever order the suite runs in
- [ ] The full integration suite passes when run twice in a row
