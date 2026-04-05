---
name: validate-commit
enabled: true
event: bash
pattern: git\s+commit
---

⚠️ **Commit Validation Checklist**

Before committing, verify:

**Design Documents** (files in `design/gdd/`):
- [ ] All 8 required sections present: Overview, Player Fantasy, Detailed Rules, Formulas, Edge Cases, Dependencies, Tuning Knobs, Acceptance Criteria
- [ ] Balance values link to source formula or rationale

**Data Files** (files in `assets/data/*.json`):
- [ ] All JSON is valid — broken JSON blocks the build pipeline
- [ ] File naming follows `[system]_[name].json` pattern (lowercase, underscores)

**Gameplay Code** (files in `src/gameplay/`):
- [ ] No hardcoded gameplay values — all values from config/data files
- [ ] Delta time used for time-dependent calculations

**Source Code** (files in `src/`):
- [ ] TODO/FIXME uses owner tag format: `TODO(name)` not bare `TODO`
- [ ] Commit message references relevant design doc or task ID

See **AGENTS.md > Path-Scoped Rules** for full coding standards per path.
