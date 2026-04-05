---
name: checkpoint-commit
enabled: true
event: stop
action: warn
conditions:
  - field: reason
    operator: regex_match
    pattern: .*
---

🚨 **CHECKPOINT VIOLATION — Uncommitted changes detected!**

You are about to stop the session but there are uncommitted file changes.
Every completed task MUST result in a checkpoint commit before you stop.

**Mandatory actions before stopping:**

1. Run `git status` to see uncommitted changes
2. Run `git add -A` to stage all changes
3. Run `git commit -m "checkpoint: [brief description of what was done]"` 
4. Only THEN may you stop the session

**Commit message format:**
```
checkpoint: [area/system]: [what was completed]

- Specific change 1
- Specific change 2
```

**Examples:**
```
checkpoint: combat: implement hitbox detection

- Add HitboxDetector component
- Add HitboxResponder interface
- Wire up collision events
```

```
checkpoint: ui: add health bar component

- Create HealthBarView with animated fill
- Hook up damage events
- Test at min/max resolutions
```

**DO NOT** stop until all work is committed. This is a hard rule.
See **AGENTS.md > Checkpoint Commit Protocol** for full details.
