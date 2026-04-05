---
name: dangerous-commands
enabled: true
event: bash
pattern: (rm\s+-rf|git\s+push\s+--force|git\s+push\s+-f|git\s+reset\s+--hard|git\s+clean\s+-f|sudo\s+|chmod\s+777)
action: block
---

⛔ **Dangerous command detected!**

This command is blocked for your safety and the project's integrity.

**Why this matters:**
- `rm -rf` — Can recursively delete critical files or directories
- `git push --force` / `-f` — Overwrites remote history, destroys others' work
- `git reset --hard` — Discards all uncommitted changes permanently
- `git clean -f` — Removes untracked files without recovery
- `sudo` — Privilege escalation can cause system-wide damage
- `chmod 777` — Insecure permissions that expose files to all users

**Alternatives:**
- Use targeted `rm` on specific files instead of `rm -rf`
- Use `git push` (without force) or create a new branch
- Use `git stash` instead of `git reset --hard`
- Use `git clean -i` for interactive selection of files to clean
- Work without `sudo` — configure your environment properly

See **AGENTS.md > Security & Safety Rules** for the full list of forbidden commands.
