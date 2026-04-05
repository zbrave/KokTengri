---
name: gameplay-hardcoded-values
enabled: true
event: file
conditions:
  - field: file_path
    operator: regex_match
    pattern: src/gameplay/
  - field: new_text
    operator: regex_match
    pattern: (damage|health|speed|rate|chance|cost|duration)\s*[:=]\s*[0-9]+
---

⚠️ **Hardcoded Gameplay Value Detected**

You're adding a hardcoded gameplay value in `src/gameplay/`. This violates the project's data-driven architecture.

**Why this matters:**
- Hardcoded values make balancing impossible without code changes
- Designers can't tweak values without programmer intervention
- No runtime adjustment possible for playtesting
- Values are scattered across code instead of centralized

**Correct approach — use data files:**

```gdscript
# WRONG — hardcoded
var damage: float = 25.0

# CORRECT — data-driven
var damage: float = config.get_value("combat", "base_damage", 10.0)
# OR
var damage: float = stats_resource.base_damage
```

**If this is intentional** (e.g., a constant that never changes), document why it's not data-driven.

See **AGENTS.md > Path-Scoped Rules > src/gameplay/** for full gameplay code standards.
