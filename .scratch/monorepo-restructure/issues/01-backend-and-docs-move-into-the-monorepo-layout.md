# 01: Backend and docs move into the monorepo layout

**What to build:** The repository takes its monorepo shape, so the Angular frontend can be added beside the backend. The backend's solution file and both projects move into `backend/`, and the per-area docs move to `docs/backend/`. Nothing about what WebCity does changes: this is a move, plus the few path edits that keep the build, the tests and the docs true. Before anything moves, the backend-only state is preserved as the tag `api-only` (ADR 0002).

Every decision below was settled with the owner in a grilling session on 2026-11-12. Do not reopen them; if one turns out to be impossible, stop and ask.

**Blocked by:** None (can start immediately)

**Status:** ready-for-agent

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

- [ ] Tag `api-only` exists on the remote at the commit named above, created before the moves
- [ ] The layout matches the target layout
- [ ] The first commit contains moves only; the second contains the path edits
- [ ] `dotnet build aspnetproject.slnx` succeeds from `backend/`
- [ ] `scripts/test-linux.sh` passes from the repo root in the cloud session
- [ ] CI passes on the pull request
- [ ] A search of tracked files for `Markdowns` and for root-relative `aspnetproject/` paths finds them only in the `backend-frontend-readiness` spec and its done tickets
- [ ] `CLAUDE.md` commands, run exactly as written, work
- [ ] After merging, the owner runs the suite once on Windows (a confirmation, not a gate)
