# What's next

The order of work toward the full-stack deadline, 2026-11-25. Tick a step when it is done, and add new steps here. Decisions live in `docs/adr/`, rules in the conventions skill, and work items in the tickets; this file holds only the order.

## Now

1. [ ] **Decide what ships.** Run `/grill-with-docs` from the repo root on which pages and flows make the deadline. End that same session with `/to-spec`, then `/to-tickets`. Every frontend ticket depends on this.
2. [ ] **Run the design session**, before the first page ticket is implemented. It chooses the styling toolset and the visual identity (palette, type, shape, mood).
   - Going-in recommendation for the toolset: Tailwind with Angular CDK, because the look must not be ordinary.
   - Fixed constraint: the typeface must cover Latin, Cyrillic and Georgian.
   - Consider installing one general frontend design skill first (candidate: Anthropic's `frontend-design`). Read `docs/agents/skills.md` before installing.
   - Output: the token values, a short style brief, the Styling rules in §10 of the conventions skill's TypeScript reference (replacing its temporary line), and an ADR for the toolset.
3. [ ] **Implement backend tickets 08 and 09** in `.scratch/backend-frontend-readiness/issues/`, with `/implement`, at any time and in parallel. Ticket 08 starts to bite as soon as sign-in exists.
4. [ ] **Implement the frontend tickets** that step 1 produces.

Other open tickets are in `.scratch/*/issues/`, each with a `Status:` line. This file does not repeat them.

## After the deadline

- A few end-to-end test flows (sign in, post, message). They come before component tests.
- Russian and Spanish dictionary files (ADR 0006).
- Translated server text, which needs a stable code on every backend failure (ADR 0006).
