# Active Hooks

## OpenCode Hookify Rules (Active)

Automated validation rules in `.claude/hookify.*.local.md`:

| Rule | Event | Trigger | Action |
| ---- | ----- | ------- | ------ |
| `dangerous-commands` | bash | `rm -rf`, `git push --force`, `sudo`, etc. | **BLOCKS** dangerous commands |
| `protected-files` | file | `.env`, credentials, `.pem`, `.key` files | **BLOCKS** sensitive file access |
| `validate-commit` | bash | `git commit` | Warns: design doc sections, JSON validity, hardcoded values, TODO format |
| `validate-push` | bash | `git push` | Warns on pushes to protected branches (main/master/develop) |
| `validate-assets` | file | Files in `assets/` | Warns: naming conventions, JSON validity |
| `gameplay-hardcoded-values` | file | Files in `src/gameplay/` with hardcoded numbers | Warns about hardcoded gameplay values |
| `design-doc-sections` | file | Files in `design/gdd/` | Warns about missing required sections |
| `session-stop-checklist` | stop | Session end | Completion checklist before stopping |

## Legacy Claude Code Hooks (Reference Only)

These bash hooks in `.claude/hooks/` are the original Claude Code versions:

| Hook | Event | Trigger | Action |
| ---- | ----- | ------- | ------ |
| `validate-commit.sh` | PreToolUse (Bash) | `git commit` commands | Validates design doc sections, JSON data files, hardcoded values, TODO format |
| `validate-push.sh` | PreToolUse (Bash) | `git push` commands | Warns on pushes to protected branches (develop/main) |
| `validate-assets.sh` | PostToolUse (Write/Edit) | Asset file changes | Checks naming conventions and JSON validity for files in `assets/` |
| `session-start.sh` | SessionStart | Session begins | Loads sprint context, milestone, git activity; detects and previews active session state file for recovery |
| `detect-gaps.sh` | SessionStart | Session begins | Detects fresh projects (suggests /start) and missing documentation when code/prototypes exist |
| `pre-compact.sh` | PreCompact | Context compression | Dumps session state into conversation before compaction |
| `session-stop.sh` | Stop | Session ends | Summarizes accomplishments and updates session log |
| `log-agent.sh` | SubagentStart | Agent spawned | Audit trail of all subagent invocations with timestamps |

> **Note**: In OpenCode, session lifecycle hooks (start, compact, stop) are handled
> as instructions in `AGENTS.md > Session Lifecycle Instructions` rather than scripts.

Hook input schema documentation: `.claude/docs/hooks-reference/hook-input-schemas.md`
