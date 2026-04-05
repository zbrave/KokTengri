---
name: design-doc-sections
enabled: true
event: file
conditions:
  - field: file_path
    operator: regex_match
    pattern: ^design/gdd/.*\.md$
  - field: new_text
    operator: not_contains
    pattern: Overview
---

⚠️ **Design Document Section Check**

You're editing a design document in `design/gdd/`. Ensure it has all 8 required sections:

1. **Overview** — one-paragraph summary
2. **Player Fantasy** — intended feeling and experience
3. **Detailed Rules** — unambiguous mechanics
4. **Formulas** — all math defined with variables
5. **Edge Cases** — unusual situations handled
6. **Dependencies** — other systems listed
7. **Tuning Knobs** — configurable values identified
8. **Acceptance Criteria** — testable success conditions

**Additional requirements:**
- Balance values must link to their source formula or rationale
- Edge cases must explicitly state what happens
- Dependencies must be bidirectional
- Acceptance criteria must be testable (QA-verifiable pass/fail)

**Incremental writing:** Create skeleton first, then fill one section at a time with user approval.

See **AGENTS.md > Coding Standards > Design Document Standards** for details.
