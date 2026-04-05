---
name: validate-push
enabled: true
event: bash
pattern: git\s+push
---

⚠️ **Push Validation — Protected Branch Check**

Before pushing, verify:

**If pushing to `main`, `master`, or `develop`:**
- [ ] Build passes successfully
- [ ] All unit tests pass
- [ ] No S1/S2 severity bugs exist
- [ ] Code has been reviewed

**General push checklist:**
- [ ] All intended changes are committed
- [ ] Commit messages are descriptive
- [ ] No accidentally staged files (secrets, temp files)

**Protected branches:** `main`, `master`, `develop`

See **AGENTS.md > Security & Safety Rules > Protected Branches** for details.
