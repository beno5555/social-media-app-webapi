---
name: implement
description: "Implement a piece of work based on a spec or set of tickets."
disable-model-invocation: true
---

Implement the work described by the user in the spec or tickets.

If the user passes a ticket reference, fetch it from the issue tracker and state its title before starting. If the reference is ambiguous, ask.

Read the repo's issue tracker doc (see CLAUDE.md, usually `docs/agents/issue-tracker.md`) for its status convention. When working from a ticket, set its status to `in-progress` before you start.

Call the Skill tool with "tdd" where possible, at pre-agreed seams.

Run typechecking regularly, single test files regularly, and the full test suite once at the end.

Once done, call the Skill tool with "spec-review" to review the work against the spec/tickets and the repo's standards.

When working from a ticket, once the review is done, set its status to `done` and tick its acceptance checkboxes that are met. Report any box that is not met instead of ticking it.

Commit your work to the current branch, including the ticket's status update, but only when the build and the full test suite pass. If either fails, don't commit: report the failures and stop.

If the work adds a database migration, read it before committing. If it drops or renames a table or column, or changes a column's type (in EF Core: `DropTable`, `DropColumn`, `RenameColumn`, `AlterColumn` with a new type), don't commit: list those operations and ask the user to confirm the data loss is intended. Otherwise commit as usual, and note in your summary that the migration has to be applied to the user's dev database.
