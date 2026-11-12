# 09: Every time the API sends is marked as UTC

**What to build:** Any client reading a time from WebCity (a post's creation time, a message's sent and seen times, a friend's last-active time) gets an ISO 8601 string that ends in `Z`, whether it arrives in an HTTP response or in a hub push. Today nothing marks times as UTC on the way out. Times are stored as UTC in columns that carry no zone, so a value read back from the database is expected to be written without the `Z`, and a browser reads a time without it as local time: a user at UTC+4 would see every time four hours behind. This was inferred from the code, not observed, so the work starts with a test that reads a stored time from a real response and shows the missing suffix.

The fix is decided in ADR 0005: times stay `DateTime`, and one converter at the JSON boundary writes every one of them as UTC. It must cover controller responses and hub payloads, including the chat list, which is read with raw SQL. The converter labels values; it does not shift them. Times the API receives keep working as they do today.

Out of scope: changing any column or property to `DateTimeOffset` (rejected in ADR 0005), and any frontend code.

**Blocked by:** None (can start immediately)

**Status:** ready-for-agent

**Difficulty:** tricky: it touches every response and the hub, the suffix is only missing for values read back from the database, and no integration test observes a hub payload today.

- [ ] A test, written failing first, reads a time that was stored earlier (seed data) from an HTTP response as raw text and finds it ends in `Z`
- [ ] The same holds for a time in the chat list, which is read with raw SQL
- [ ] A time created in the same request (not read back) also ends in `Z` and names the same instant as before
- [ ] Hub payloads carry the suffix too, proven by a test if a hub payload can be observed through the existing test host, otherwise verified in a manual run and recorded in this ticket
- [ ] A date of birth sent to the API and read back is the same calendar date
- [ ] The temporary note under Dates in the conventions skill's frontend rules is removed, and the data doc describes how times are serialized
- [ ] The full integration suite passes
