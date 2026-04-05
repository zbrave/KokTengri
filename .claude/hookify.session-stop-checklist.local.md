---
name: session-stop-checklist
enabled: true
event: stop
action: warn
conditions:
  - field: reason
    operator: regex_match
    pattern: .*
---

📋 **Session Completion Checklist**

Before ending the session, ensure:

**Work Progress:**
- [ ] All active tasks completed or state saved
- [ ] `production/session-state/active.md` updated with current progress
- [ ] Key decisions documented

**Code Quality:**
- [ ] No broken code left in the working tree
- [ ] Build still passes (if applicable)
- [ ] Tests still pass (if applicable)

**Documentation:**
- [ ] Design doc sections written to file (not just in conversation)
- [ ] Architecture decisions recorded in `docs/architecture/`

**Session State:**
- [ ] Update `production/session-state/active.md` with:
  - Current task and progress
  - Files being actively worked on
  - Key decisions made this session
  - Open questions
- [ ] Archive state to `production/session-logs/session-log.md`

See **AGENTS.md > Session Lifecycle Instructions > At Session End** for details.
