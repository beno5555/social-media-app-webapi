# Parallel local sessions in worktrees

Two local Claude sessions can implement two tickets at once, each in its own git worktree on its own branch. The main checkout stays on `master` and is where branches are merged.

## Start a lane

From the repo root:

```bash
claude --worktree <name>        # e.g. claude -w feed-ranking
```

Claude Code creates `.claude/worktrees/<name>/` on a new branch `worktree-<name>`, based on `origin/master` as just fetched. `.worktreeinclude` copies the gitignored settings files (the three `appsettings*.json` and `.claude/settings.local.json`) into it. Auto memory is shared by every worktree of the repo.

Pick tickets that touch different files. The ticket's "What to build" names the areas, and two tickets that change the same service, query, seeder or doc conflict on merge.

## What differs inside a worktree

- **Test database.** With `ConnectionStrings__DefaultConnection` unset, `DatabaseFixture` names the local database after the worktree folder (`TestSocialMediaWebApiDb_<name>`, with any character other than a letter or digit turned into `_`), and `CustomWebApplicationFactory` gives the app under test the same connection string. The first run creates and migrates it. The main checkout keeps `TestSocialMediaWebApiDb`. So two suites never wipe each other's data.
- **Sync.** Skip CLAUDE.md's "pull on `master`" step: `master` is checked out in the main checkout, and the worktree already starts from a fresh `origin/master`.
- **Skills.** The worktree has its own copy of `.claude/skills/`, separate from the junctions in `~/.claude/skills/` (see `skills.md`). Editing a skill there is fine: the edit is committed on the lane's branch and reaches the shared copy when the branch is merged.

## Finish a lane

1. In the worktree, commit on `worktree-<name>`.
2. In the main checkout, on `master`: `git merge --no-ff worktree-<name>`.
3. Run the full suite on `master` once both lanes are merged, since each lane was tested only against its own base.
4. Push `master`.
5. Exit the worktree session. Claude Code asks whether to keep or remove the worktree. Deleting the branch is a separate step that the owner confirms.

A worktree's test database stays behind. Drop it when the lane is done:

```bash
sqlcmd -S localhost -E -Q "DROP DATABASE [TestSocialMediaWebApiDb_<name_with_underscores>]"
```
