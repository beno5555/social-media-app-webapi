# Agent skills

Matt Pocock's engineering skills are committed under `.claude/skills/` so cloud sessions get them.

The copies in `~/.claude/skills/` with the same names are **Windows directory junctions into this repo**, so there is one copy on disk and editing through either path changes both.

- Never replace a junction with a real folder.
- Remove a junction with `cmd /c rmdir <path>`, not `rm -rf`, which can delete the repo copy through the link.
- The global SessionStart hook (`~/.claude/hooks/sync-remote.sh`) warns if a junction becomes a separate copy.
