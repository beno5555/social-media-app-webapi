---
name: webcity-conventions
description: "WebCity house style. Use when writing, editing or reviewing any code in this repo (C#/ASP.NET Core backend, TypeScript/Angular frontend): control flow, layering, errors, security, logging, data access, naming, tests."
---

# WebCity Conventions

The house style for this repo. The rules come from the WebCity ASP.NET Core Web API. `references/typescript.md` carries them over to the frontend. This skill takes precedence over the global `coding-conventions` skill.

## How to use this skill

1. Read this file completely.
2. Before you write code, read the reference files from the index that match the task.
3. For any code change, always read `references/control-flow.md` and the language file for that code.
4. Before you finish, run the checklist at the end of this file.

## Priority order

When rules conflict, follow the first applicable item:

1. The user's explicit instruction in the current task.
2. The existing codebase's established pattern. Match the neighboring code.
3. This skill.
4. The language community's default idiom.

## Reference index

| File | Covers | Read when |
|---|---|---|
| `references/control-flow.md` | Single exit, nesting, named conditions, boundary exception | Always |
| `references/layering.md` | Layer responsibilities, thin controllers, abstractions, DI | Adding a class, endpoint or service |
| `references/error-handling.md` | Result wrappers, exceptions, HTTP status mapping | A method can fail, or writing an endpoint |
| `references/security.md` | Generic errors, ownership scoping, secrets, rate limits, auth | Auth, user data, endpoints, tokens |
| `references/logging.md` | Action logs vs system logs, what to log, safe loggers | Service methods, background jobs |
| `references/data-access.md` | Repositories, loading, bulk ops, transactions, soft delete, time | Queries, schema, persistence |
| `references/dtos-and-mapping.md` | Audience tiers, DTO naming, mappers, validation | Input/output shapes |
| `references/constants-and-config.md` | Constants, response messages, options classes | Any literal value or setting |
| `references/naming.md` | Identifiers, booleans, async, routes | Naming anything |
| `references/testing.md` | Integration tests, isolated DB, test naming | Adding or changing behavior |
| `references/comments-and-docs.md` | Comment style, doc comments, keeping docs in sync | Writing comments or changing documented behavior |
| `references/csharp.md` | C#/.NET formatting and idioms, full reference examples | Writing C# |
| `references/typescript.md` | TypeScript translation of every rule | Writing TS/JS |
| `references/other-languages.md` | Python, Java/Kotlin, Go, SQL | Writing anything else |

## Agent workflow

- **Read before writing.** Before you touch a layer, open at least one sibling file in it, plus any project docs (README, `docs/`).
- **Make minimal diffs.** Do not reformat, rename or reorganize anything the task does not require.
- **Report suspicious code instead of silently fixing it.** Code that looks inconsistent may be intentional. Report anything outside the task with its location and your reasoning:
  > `CommentController`: create uses `api/comments/posts/{postId}` but list uses `api/comments/post/{postId}`. Left unchanged; clients may depend on it.
- **Never hand-edit generated files**, such as migrations, model snapshots, lockfiles or generated clients. Regenerate them with their tool instead.
- **Never commit secrets.** Keep them in configuration or user secrets.
- **Verify your work.** Build, run the existing tests, and add tests for new behavior. Report exactly what was and was not tested.
- **Keep docs in sync.** When behavior described in project docs changes, update those docs in the same change.
- **Ask before irreversible, ambiguous decisions**, such as a schema change, a public route change, a deletion or a new dependency.

## Core rules

Apply these even before you open a reference file:

- **Control flow.** Outside controllers and handlers, use one `return` per method, nest the success path inside a positive `if`, and give each condition a named boolean. Controllers may return early, checking success first.
- **Errors.** Expected failures are result values, not exceptions. One global handler deals with the unexpected.
- **One type per file.** Each class, record, enum, interface or struct gets its own file, named after the type (`UserRepository` → `UserRepository.cs`). Never group a concept's types into one file such as `Repositories.cs` or `Dtos.cs`. Exceptions: nested types, and a response wrapper's closely tied variants (`ApplicationResponse`, `ApplicationResponse<T>`, `ListResponse<T>`) may share one file.
- **Controllers.** Keep them thin. Services own business rules, logging and notifications. Repositories own queries.
- **Abstractions.** Add no speculative interfaces or abstractions. Follow the codebase's choice.
- **Error messages.** Never reveal whether something exists or who owns it. Scope ownership checks inside the query.
- **Logging.** Log in services, never in controllers. Logging must never throw into business logic.
- **Literals.** No magic numbers or strings. Use central constants, response messages and options classes.
- **Output shapes.** Return Minimal, Standard or Full DTOs by audience. Never return entities.
- **Time.** Use UTC. Load relations explicitly. Use set-based bulk operations. Use transactions for multistep writes.
- **Tests.** Write integration tests against an isolated, reseeded database. Name them `Method_Condition_ExpectedResult`.

## Pre-submit checklist

- [ ] I read sibling files and docs, and the change matches their patterns.
- [ ] Every new type is in its own file named after it (except nested types and response wrapper variants).
- [ ] Domain, data and helper code has one return per method, a nested success path and named conditions.
- [ ] Controllers are thin, check success first and map status codes per call site.
- [ ] There are no magic literals.
- [ ] Ownership is scoped, errors are generic, and the endpoint is authorized and rate-limited.
- [ ] Failures are logged in the service, and the logger cannot throw.
- [ ] The DTO tier fits the audience, mappers are pure and no entity leaks.
- [ ] Includes are explicit, bulk operations are set-based, multistep writes are transactional and times are UTC.
- [ ] Tests were added or updated and run, and the results are reported honestly.
- [ ] No generated files were hand-edited, no secrets were added, docs are updated and suspected bugs were reported.
