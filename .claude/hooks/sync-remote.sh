#!/usr/bin/env bash
# SessionStart hook: fetch origin and fast-forward the current branch when it is safe.
# Never merges diverged history, never touches a dirty tree, never fails the session.

cd "${CLAUDE_PROJECT_DIR:-.}" || exit 0

report=()

if ! timeout 20 git fetch --prune --quiet origin 2>/dev/null; then
  report+=("git sync: fetch from origin failed (offline or no access); local state unchanged.")
else
  branch=$(git symbolic-ref --quiet --short HEAD)
  upstream=$(git rev-parse --abbrev-ref --symbolic-full-name '@{u}' 2>/dev/null)

  if [ -z "$branch" ]; then
    report+=("git sync: detached HEAD, nothing fast-forwarded.")
  elif [ -z "$upstream" ]; then
    report+=("git sync: '$branch' has no upstream, nothing fast-forwarded.")
  else
    read -r ahead behind < <(git rev-list --left-right --count "HEAD...$upstream")
    clean=true
    if ! git diff --quiet || ! git diff --cached --quiet; then
      clean=false
    fi

    if [ "$behind" -eq 0 ]; then
      report+=("git sync: '$branch' is up to date with $upstream (ahead $ahead).")
    elif [ "$ahead" -gt 0 ]; then
      report+=("git sync: '$branch' has DIVERGED from $upstream (ahead $ahead, behind $behind). Not touched; rebase or merge by hand.")
    elif [ "$clean" = false ]; then
      report+=("git sync: '$branch' is behind $upstream by $behind, but the working tree has uncommitted changes. Not fast-forwarded.")
    elif git merge --ff-only --quiet "$upstream" 2>/dev/null; then
      report+=("git sync: fast-forwarded '$branch' by $behind commit(s) from $upstream.")
    else
      report+=("git sync: fast-forward of '$branch' to $upstream failed (likely untracked files in the way). Not touched.")
    fi
  fi

  # Remote branches with work not in HEAD, e.g. branches pushed from a phone/cloud session.
  unmerged=$(git for-each-ref --no-merged=HEAD --sort=-committerdate --count=10 \
    --format='%(refname:short) (%(committerdate:relative))' refs/remotes/origin \
    | grep -v '^origin/HEAD\b' | grep -v '^origin ')
  if [ -n "$unmerged" ]; then
    report+=("git sync: remote branches with commits not in HEAD:")
    while IFS= read -r line; do
      report+=("  - $line")
    done <<< "$unmerged"
  fi
fi

text=$(printf '%s\n' "${report[@]}")
escaped=$(printf '%s' "$text" | sed -e 's/\\/\\\\/g' -e 's/"/\\"/g' -e 's/\t/\\t/g' | awk 'BEGIN{ORS=""} NR>1{print "\\n"} {print}')

printf '{"systemMessage":"%s","hookSpecificOutput":{"hookEventName":"SessionStart","additionalContext":"%s"}}\n' "$escaped" "$escaped"
exit 0
