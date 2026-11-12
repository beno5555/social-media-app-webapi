# 03: README describes the backend as it is now, and the repository is called webcity

**What to build:** A visitor to the repository reads about WebCity's backend as it is today, not about the console application it grew out of. That history belongs to the API-only version, which stays readable at tag `api-only` (ADR 0002). It leaves the README and is replaced by one line pointing to the tag. The console history is not confined to the migration section: it also runs through the opening paragraph, the description of the model layer, the "carried over / new to the API version" split in the design decisions, and the status paragraph. Everything that remains is checked against the code, because the README has not been reviewed since the monorepo move. The folder tree and the startup excerpt are the parts most likely to be stale. Design decisions that are still true and useful stay, or move to the matching doc in `docs/backend/`; they are not simply deleted. This ticket does not describe the frontend, which does not exist yet; ticket 04 turns the README into a product README.

The GitHub repository `social-media-app-webapi` is renamed `webcity` (name agreed with the owner on 2026-11-12). The owner renames it in GitHub's settings, because an agent cannot. Before changing anything locally, the agent confirms the rename has happened: the remote answers under the new name. Then it points the local `origin` remote at the new URL. GitHub redirects the old URL. The local root folder is not renamed, because Claude Code's project memory is keyed to its path. A cloud environment tied to the old name may need the repository selected again. No file in the repository names the old repository today, but recheck before finishing. `CLAUDE.md` says the README "covers history and design decisions", and that line must change.

Together with 04, this replaces the former ticket 02.

**Blocked by:** None (can start immediately)

**Status:** done

**Difficulty:** routine: writing checked against the code, with no new pattern; the rename itself is done by the owner.

- [x] The console-application history is gone from the README, with one line pointing to tag `api-only` for the earlier state
- [x] Every statement in the README is true of the code
- [x] Design decisions removed from the README that are still true now live in `docs/backend/`
- [ ] The repository is named `webcity` on GitHub, and the local `origin` remote uses the new URL
- [x] `CLAUDE.md` describes what the README now covers, and no file names the old repository

## Comments

**2026-11-13, implementation.** The GitHub rename is confirmed: `git ls-remote git@github.com:beno5555/webcity.git` answers. The local `origin` remote still uses the old URL, because the agent's `git remote set-url` was denied by the permission classifier; the owner has to run `git remote set-url origin git@github.com:beno5555/webcity.git`. That is the one unticked box. Spec review (standards and spec axes) found no hard violations; two README inaccuracies it found (`AuthController` and the early-return rule's scope) were corrected. `docs/backend/auth.md` still mentions the console app's hashing scheme; left alone, since the ticket scopes the history removal to the README.
