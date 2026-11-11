# 07: Per-IP rate limits see the real client IP behind the proxy

**What to build:** Behind the reverse proxy that always fronts WebCity (the dev server proxy in development, nginx in production; see ADR 0001), per-IP rate limits count each visitor's real IP. Today the backend sees only the proxy's address, so every anonymous visitor shares a single budget. For example, the eleventh login attempt of the hour from anyone, anywhere, is rejected, and one person can lock everyone out on purpose.

The proxy passes the original client IP in a forwarded header, and the backend should use it, but only when the request arrives from a trusted proxy. A client can write that header itself, so trusting it from anyone would let an attacker invent a new IP per request and bypass every per-IP limit, including brute-force protection on login. The trusted proxy addresses come from configuration, because they depend on how WebCity is deployed (same host, container, load balancer). The forwarded scheme (HTTPS terminated at the proxy) is handled by the same mechanism and belongs here too.

**Blocked by:** None (can start immediately)

**Deferred:** not scheduled until deployment work begins.

**Status:** ready-for-agent

**Difficulty:** tricky: a security boundary where a wrong trust setting silently disables rate limiting, and the right proxy addresses depend on the deployment topology.

- [ ] Trusted proxy addresses are read from configuration, with no hardcoded deployment addresses
- [ ] A test shows a request forwarded by a trusted proxy is rate-limited by the forwarded client IP
- [ ] A test shows a forwarded header from an untrusted source is ignored and the connection IP is used
- [ ] Only the entry the trusted proxy added is believed; entries a client prepends are ignored
- [ ] The forwarded scheme is honoured, so HTTPS terminated at the proxy is not seen as plain HTTP
- [ ] The auth doc and ADR 0001's consequences describe the requirement, including what the proxy must send
- [ ] The full integration suite passes when run twice in a row
