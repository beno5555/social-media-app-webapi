# 01: Backend and docs move into the monorepo layout

**What to build:** The repository takes its monorepo shape, so the Angular frontend can be added beside the backend. The backend's solution file and both projects move into `backend/`, and the per-area docs move to `docs/backend/`. Nothing about what WebCity does changes: this is a move, plus the few path edits that keep the build, the tests and the docs true. Before anything moves, the backend-only state is preserved as the tag `api-only` (ADR 0002).

Every decision below was settled with the owner in a grilling session on 2026-11-12. Do not reopen them; if one turns out to be impossible, stop and ask.

**Blocked by:** None (can start immediately)

**Status:** done

**Difficulty:** routine: every decision is made and listed here, and the integration suite proves the result.

## Target layout

```
/
├── .claude/  .github/  .scratch/      (unchanged)
├── CLAUDE.md  GLOSSARY.md  README.md  (stay at the root)
├── backend/
│   ├── aspnetproject.slnx
│   ├── aspnetproject/
│   └── aspnetproject.IntegrationTests/
├── docs/
│   ├── adr/  agents/                  (unchanged)
│   └── backend/
│       └── auth.md  background-jobs.md  controllers.md  data.md  signalr.md
└── scripts/
    └── test-linux.sh                  (stays at the root)
```

## Decisions

- **Tag first.** Before any move, push an annotated tag `api-only` at commit `5c7ad3b7d0d5ea7dc6b24b19997d10475e6a015d` (the merge of pull request 1, the last backend-only commit on `master`). The owner has approved this push. If the push is refused, stop and ask the owner to create the tag through GitHub's "Draft a new release" form, targeting that commit, and continue only once it exists.
- **Same repository.** The move happens in place. The root folder is never renamed or relocated, and the GitHub repository keeps its name for now (ticket 02).
- **Names do not change.** The project folders, the solution file and every namespace stay `aspnetproject`.
- **Commands run from `backend/`.** The solution file and both projects stay siblings, so the project reference and the solution file need no edits.
- **Docs.** `aspnetproject/Markdowns/Chunked/*.md` become `docs/backend/*.md`. The `Markdowns/` folder disappears.
- **The test script stays at the root.** Only its two internal paths change (the settings file path and the `dotnet test` target). `.github/workflows/tests.yml` is not edited.
- **No `frontend/` folder.** It first appears when the Angular project is scaffolded.
- **Pure move.** No code changes and no content rewrites. Path edits only, in:
  - `scripts/test-linux.sh`: the two paths above.
  - `CLAUDE.md`: commands, paths, the "about to become a monorepo" sentence (it now is one), and the docs location. It stays one root file and is not slimmed down.
  - `README.md`: the one stale `/Markdowns` line in its folder tree. Its rewrite is ticket 02.
  - `.claude/skills/webcity-conventions/SKILL.md`: its mention of `Markdowns/` as a docs folder.
  - Open tickets 06 and 07 in `.scratch/backend-frontend-readiness/issues/`, if they name an old path. (None did when this ticket was written.)
- **History is left alone.** The `backend-frontend-readiness` spec and its done tickets 01 to 05 keep the paths they were written with.
- **Two commits, in order.** First a commit of `git mv` moves only, with no file content changed. Then a commit of the path edits. This keeps `git log --follow` and blame exact.
- **One pull request into `master`**, opened only when the owner asks for it, to be merged with "Create a merge commit", never squashed. Its description carries the Windows checklist below. ADR 0002 and these tickets are already committed on branch `ccr-da8d83d6-8flhf2`; build on that branch's commits so they arrive in the same pull request.

## Windows checklist (put this in the pull request description)

Git moves only the files it tracks. After pulling, the old `aspnetproject/` and `aspnetproject.IntegrationTests/` folders remain at the root, holding only ignored files: the three `appsettings*.json` files, `bin/`, `obj/` and `Logs/`. In this order:

1. Close Visual Studio or Rider.
2. Pull `master`.
3. Move `appsettings.json`, `appsettings.Development.json` and `appsettings.Testing.json` from `aspnetproject/` to `backend/aspnetproject/`.
4. Delete the two leftover folders at the root, and the root `.vs/` or `.idea/` folder if present. Do this only after step 3, or the settings files are lost. Keep `Logs/ErrorLogs.txt` first if the old error log is wanted.
5. Open `backend/aspnetproject.slnx`.
6. Run `dotnet test` from `backend/`.

## Acceptance

- [x] Tag `api-only` exists on the remote at the commit named above, created before the moves
- [x] The layout matches the target layout
- [x] The first commit contains moves only; the second contains the path edits
- [x] `dotnet build aspnetproject.slnx` succeeds from `backend/`
- [x] `scripts/test-linux.sh` passes from the repo root in the cloud session
- [ ] CI passes on the pull request
- [x] A search of tracked files for `Markdowns` and for root-relative `aspnetproject/` paths finds them only in the `backend-frontend-readiness` spec and its done tickets
- [ ] `CLAUDE.md` commands, run exactly as written, work
- [ ] After merging, the owner runs the suite once on Windows (a confirmation, not a gate)

## Comments

2026-11-12, implementation:

- The owner pushed the annotated tag `api-only` at `5c7ad3b` from their own clone, because this cloud session may push only to its working branch. The moves came after it.
- The root also holds `PROJECT_DESCRIPTION.md`, merged from branch `claude/hopeful-franklin-dj3rq4` at the owner's request just before the moves. It is outside this ticket and names no backend path. Otherwise the layout matches.
- The path search also finds the old paths in this ticket, which has to name them to describe the move.
- `dotnet build aspnetproject.slnx` from `backend/` succeeded. `scripts/test-linux.sh` from the root passed 121 of 121, and its `--filter` form passed 25 of 25.
- Not ticked: "The first commit contains moves only; the second contains the path edits". The moves commit is pure renames and the next commit holds the path edits, but a later `fixup!` commit removes a redundant sentence from `CLAUDE.md`. Squashing it into the path-edits commit needs a force-push to this branch, which the session was not allowed to do.
- Not ticked: "CI passes on the pull request", because no pull request is open yet.
- Not ticked: "`CLAUDE.md` commands, run exactly as written, work". The build, the script lines and the path resolution of every `dotnet` line were checked from the stated folders, but the plain `dotnet test`, `dotnet run` and `dotnet ef` lines need the Windows SQL Server setup and settings, so the owner's Windows run is what confirms them.
- Not ticked: "The owner runs the suite once on Windows", which comes after merging.

2026-11-12, Windows session on the owner's laptop:

- With the owner's approval, the `fixup!` commit was folded into "Point the build, test script and docs at the monorepo layout" and the branch was force-pushed with a lease. The tree is unchanged, and the moves commit `92f1b93` is 162 pure renames, so the moves-then-path-edits box is ticked.
- The owner's local commit "Trim CLAUDE.md and state what belongs in it" was cherry-picked onto the branch. Its intro conflicted with the monorepo sentence; the resolution keeps the monorepo sentence and drops the "Temporary: about to become a monorepo" note. No line in `CLAUDE.md` or `docs/agents/skills.md` names a root-level `aspnetproject/` path or `Markdowns/`.
- The checklist ran on the laptop: the three `appsettings*.json` files, `Logs/ErrorLogs.txt` and `aspnetproject.csproj.user` moved into `backend/aspnetproject/`, and only then were the old root folders deleted.
- From `backend/`: `dotnet build aspnetproject.slnx` succeeded, `dotnet test aspnetproject.IntegrationTests` passed 121 of 121, the single-test `--filter` line passed 1 of 1, and `dotnet ef migrations list --project aspnetproject` resolved the project and the database.
- Still not ticked: "`CLAUDE.md` commands, run exactly as written, work". `dotnet run` was not started, because its background cleanup jobs act on the dev database, and the two `dotnet ef` lines that change files or the database were not run.
