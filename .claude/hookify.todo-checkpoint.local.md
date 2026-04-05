---
name: todo-checkpoint-reminder
enabled: true
event: bash
pattern: todowrite
action: warn
---

📌 **TODO Checkpoint Reminder**

A TODO item was just updated. If any item was marked `completed`, this is a checkpoint opportunity.

**Checkpoint Protocol (from AGENTS.md):**
When you complete a TODO item:
1. Verify the completed work (run diagnostics/tests if applicable)
2. Stage the changes: `git add -A`
3. Commit: `git commit -m "checkpoint: [area]: [what was done]"`
4. Then continue with the next TODO item

**Important:** Each completed TODO = one checkpoint commit. Do NOT batch multiple completed items.
