# Damage Calculator System

> **Status**: Approved
> **Author**: zbrave + game-designer
> **Last Updated**: 2026-04-04
> **Implements Pillar**: Every Element Matters, Build Diversity Over Build Power, Clarity Through Consistency

## Overview

Damage Calculator is a pure, stateless math utility that centralizes all runtime damage formulas for Kök Tengri. It receives fully-formed inputs (spell data, enemy affinity context, class context, elapsed time, and meta modifiers), returns deterministic integer damage values, and owns no gameplay state. This system exists to guarantee formula consistency across Spell Effects, Enemy Health & Damage, Boss logic, and HUD number display while keeping balance tuning data-driven through ScriptableObjects rather than scattered per-feature calculations.

## Player Fantasy

Players should feel that elemental knowledge and build choices produce reliable, understandable outcomes: weakness hits feel clearly stronger, resistance feels clearly weaker, and class identity meaningfully changes offensive output without hidden randomness. The fantasy is that the shaman's power is coherent and learnable: if a player masters elements and class synergies, they can predict and optimize damage intentionally. Consistency is the emotional payoff here; the world follows clear ritual math.

## Detailed Design

### Detailed Rules

1. **Stateless calculation contract**
   - Damage Calculator stores no runtime state and performs no caching that affects correctness.
   - Every output is a pure function of provided inputs.

2. **Single source of truth for damage math**
   - Spell Effects, Enemy Health & Damage, Boss System, and HUD consumers must use Damage Calculator outputs.
   - No system may re-implement formula variants locally.

3. **Input ownership boundaries**
   - Damage Calculator does not query game objects directly.
   - Callers provide resolved data from ScriptableObjects and runtime context.

4. **Integer output policy**
   - All returned damage values are integers.
   - Flooring occurs after all multiplications.
   - Valid hits can never return 0; minimum final damage is 1.

5. **MVP scope limitations**
   - Critical hit logic is out of scope for MVP.
   - No random variance term is applied in MVP formulas.

6. **Class bonus stacking policy**
   - If multiple class bonus rules could match in a future mixed-rule context, bonuses do not stack.
   - The highest applicable class multiplier is used.

7. **Element affinity fallback policy**
   - Unknown enemy type or missing affinity mapping defaults to element multiplier `1.0`.

### States and Transitions

Although Damage Calculator is stateless, call sites can be modeled with deterministic request states.

| State | Entry Condition | Exit Condition | Behavior |
|---|---|---|---|
| Idle | No active request | Calculation request received | No work performed |
| ValidateInput | Caller submits damage context | Inputs valid or fallback defaults applied | Normalize level, enemy type mapping, class flags |
| ResolveMultipliers | Validation complete | Multipliers resolved | Resolve level, element, class, meta multipliers |
| ComputeRaw | All multipliers available | Raw float computed | Multiply base and all applicable factors |
| QuantizeOutput | Raw float available | Integer output emitted | Floor result, apply minimum clamp to 1 |
| Return | Final integer ready | Caller receives value | Return deterministic damage payload |

### Interactions with Other Systems

| System | Interface In | Interface Out | Responsibility Split |
|---|---|---|---|
| Spell Effects | Spell recipe elements, spell level, base damage, target enemy type | Final integer spell damage | Spell Effects chooses targets and timing; Damage Calculator provides number only |
| Enemy Health & Damage | Enemy base contact damage, elapsed minutes | Final integer enemy-to-player damage | Enemy system chooses hit events; Damage Calculator resolves scaling |
| Boss System | Boss spell/contact inputs using same formulas | Final integer damage values | Boss uses same utility to prevent formula drift |
| HUD (damage numbers) | Damage result from authority system | Displayable integer | HUD never computes; it renders authoritative output |
| Meta Progression | Meta power bonus ratio (resolved from progression data) | Included multiplier in spell formula | Meta system owns progression state; calculator only multiplies provided value |

## Formulas

### Spell Damage (MVP)

```text
level_multiplier = 1 + 0.25 × (spell_level - 1)
raw_spell_damage = base_damage × level_multiplier × element_multiplier × class_bonus × meta_power_multiplier
final_spell_damage = max(1, floor(raw_spell_damage))
```

| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| `base_damage` | float | 5-25 | `SpellDefinitionSO` | Base per-spell damage at level 1 |
| `spell_level` | int | 1-5 | Spell runtime state | Current level of the spell |
| `level_multiplier` | float | 1.0-2.0 | Calculated | +25% per level above 1 |
| `element_multiplier` | float | 0.6 / 1.0 / 1.5 | Enemy affinity lookup | Resistance / Normal / Weakness |
| `class_bonus` | float | 1.0 / 1.15 / 1.25 | Class rules | Class-specific damage multiplier |
| `meta_power_multiplier` | float | 1.0-1.60 | Meta progression data | Separate power multiplier from progression |

**Expected output range (MVP baseline without temporary buffs):**
- Low practical: `5 × 1.0 × 0.6 × 1.0 × 1.0 = 3.0 -> 3`
- High practical at max meta: `25 × 2.0 × 1.5 × 1.25 × 1.60 = 150.0 -> 150`

### Enemy Contact Damage to Player (MVP)

```text
time_multiplier = 1 + 0.08 × elapsed_minutes
raw_enemy_damage = base_contact_damage × time_multiplier
final_enemy_damage = max(1, floor(raw_enemy_damage))
```

| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| `base_contact_damage` | float | 5-15 | `EnemyDefinitionSO` | Base collision/contact damage by enemy type |
| `elapsed_minutes` | float | 0.0-30.0+ | Run timer context | Minutes since run start |
| `time_multiplier` | float | 1.0-3.4 (at 30 min) | Calculated | +8% contact scaling per minute |

**Expected output range (0-30 min):**
- At 0 min, low: `5 × 1.0 = 5`
- At 30 min, high: `15 × 3.4 = 51`

### Element Multiplier Lookup Table (5 elements x 6 enemy types)

Table value = multiplier used in `spell_damage` formula for the attacking spell element against target enemy type.

| Enemy Type | Od | Sub | Yer | Yel | Temür |
|---|---:|---:|---:|---:|---:|
| Kara Kurt | 1.5 | 1.0 | 1.0 | 0.6 | 1.0 |
| Yek Uşağı | 1.0 | 1.0 | 1.0 | 1.5 | 0.6 |
| Albastı | 0.6 | 1.0 | 1.0 | 1.0 | 1.5 |
| Çor | 1.0 | 0.6 | 1.0 | 1.0 | 1.5 |
| Demirci Cin | 1.0 | 1.5 | 0.6 | 1.0 | 1.0 |
| Göl Aynası | 1.5 | 0.6 | 1.0 | 1.0 | 1.0 |

Fallback rule:
- Unknown enemy type or missing row => `element_multiplier = 1.0`.

### Class Bonus Lookup Logic

```text
if class == Kam and spell has two different elements: class_bonus = 1.15
if class == Batur and spell contains Sub or Yer: class_bonus = 1.25
if class == Mergen and spell contains Od or Temür: class_bonus = 1.25
if class == Otacı and spell contains Yel or Od: class_bonus = 1.25
if multiple rules match in mixed-rule contexts: class_bonus = max(applicable_bonuses)
if no rule matches: class_bonus = 1.0
```

Notes:
- Kam applies only to combined spells (two different elements), never to basic same-element spells.
- Batur, Mergen, and Otacı match by element inclusion in the spell recipe.

### Meta-Progression Multiplier Application Order

```text
1) Resolve base_damage
2) Apply spell level multiplier
3) Apply element multiplier
4) Apply class bonus
5) Apply meta_power_multiplier (separate, last multiplier)
6) Floor and clamp minimum 1
```

Design intent:
- Applying meta progression as a separate multiplier preserves clean tuning isolation between per-run build power and account progression power.

### Worked Examples

1) **Mid-run normal hit**
- Inputs: `base=12`, `level=3`, `element=Normal(1.0)`, `class=1.0`, `meta=1.12`
- `level_multiplier = 1 + 0.25 × (3 - 1) = 1.5`
- `raw = 12 × 1.5 × 1.0 × 1.0 × 1.12 = 20.16`
- `final = floor(20.16) = 20`

2) **Weakness + class bonus spike (non-max meta)**
- Inputs: `base=20`, `level=4`, `element=1.5`, `class=1.25`, `meta=1.30`
- `level_multiplier = 1.75`
- `raw = 20 × 1.75 × 1.5 × 1.25 × 1.30 = 85.3125`
- `final = 85`

3) **Level 5 + weakness + class bonus + max meta (ceiling check)**
- Inputs: `base=25`, `level=5`, `element=1.5`, `class=1.25`, `meta=1.60`
- `level_multiplier = 2.0`
- `raw = 25 × 2.0 × 1.5 × 1.25 × 1.60 = 150.0`
- `final = 150`

4) **Resistance + no class bonus minimum meaningful check**
- Inputs: `base=5`, `level=1`, `element=0.6`, `class=1.0`, `meta=1.0`
- `raw = 5 × 1.0 × 0.6 × 1.0 × 1.0 = 3.0`
- `final = 3` (still above minimum clamp)

## Edge Cases

| Scenario | Expected Behavior | Rationale |
|---|---|---|
| All multipliers produce value below 1 | After floor, clamp with `max(1, value)` | Valid hits must always deal at least 1 damage |
| Multiple class bonuses could apply | Use highest applicable class bonus only; never stack | Prevents runaway multiplicative stacking |
| Unknown enemy type in affinity lookup | Use `element_multiplier = 1.0` | Fails safe and keeps combat deterministic |
| Level 5 spell + weakness + class bonus | Must match worked ceiling path before integer quantization | Verifies high-end formula integrity |
| Resistance + no class bonus | Must remain meaningful at low bases and still respect minimum clamp | Confirms low-end readability and fairness |
| Meta progression at max level | `meta_power_multiplier` capped by progression config (MVP reference: 1.60) | Prevents unbounded account-scaling inflation |
| Caller sends spell level outside 1-5 | Clamp to nearest valid bound before multiplier resolution | Defensive safety without undefined math |
| Negative elapsed_minutes from bad caller data | Treat as 0.0 before enemy formula | Prevents accidental down-scaling exploit |

## Dependencies

Inbound dependency policy:
- **None.** Damage Calculator is a pure math utility with no runtime dependency on other systems.

Systems that depend on Damage Calculator outputs:

| System | Direction | Nature of Dependency |
|---|---|---|
| Enemy Health & Damage | Depends on Damage Calculator | Uses authoritative outgoing and incoming damage numbers |
| Spell Effects | Depends on Damage Calculator | Uses formula outputs for all spell hit resolution |
| Boss System | Depends on Damage Calculator | Reuses same formulas to avoid special-case drift |
| HUD (damage numbers) | Depends on Damage Calculator (indirect via combat authority) | Displays integer damage results consistently |

## Tuning Knobs

| Parameter | Current Value | Safe Range | Effect of Increase | Effect of Decrease |
|---|---|---|---|---|
| Spell level scaling per level | +25% (`0.25`) | +10% to +40% | Stronger reward for spell upgrades | Flatter progression per spell level |
| Weakness multiplier | 1.5 | 1.2-1.8 | Increases payoff of element targeting | Reduces element matchup importance |
| Resistance multiplier | 0.6 | 0.4-0.9 | Makes wrong element punishment harsher | Softens penalty for mismatch |
| Class bonus (Kam combined) | 1.15 | 1.05-1.25 | Improves Kam diversity payoff | Reduces incentive for combined-spell routing |
| Class bonus (Batur/Mergen/Otacı) | 1.25 | 1.10-1.35 | Sharpens class identity and build expression | Narrows class-driven damage differences |
| Enemy time scaling per minute | +8% (`0.08`) | +4% to +12% | Raises late-run threat curve | Flattens late-run pressure |
| Meta power max multiplier | 1.60 (from progression spec) | 1.20-1.80 | Raises account progression impact on combat | Lowers account progression influence |

## Visual/Audio Requirements

Damage Calculator itself has no direct presentation layer outputs. Presentation systems consume calculated integer values.

| Event | Visual Feedback | Audio Feedback | Priority |
|---|---|---|---|
| Spell hit resolved with weakness | Larger/highlighted damage number style from HUD rules | Existing weakness-hit audio accent | High |
| Spell hit resolved with resistance | Reduced/tempered damage number style from HUD rules | Existing resistant-hit dampened cue | High |
| Enemy contact damage resolved | Player damage number and health bar decrement | Existing player-hit cue | High |

## UI Requirements

| Information | Display Location | Update Frequency | Condition |
|---|---|---|---|
| Final spell damage integer | Floating combat numbers near target | Per valid spell hit | Always during combat |
| Final enemy contact damage integer | Player damage number / health feedback | Per contact event | On enemy-player collision hit |
| Weakness/Resistance interpretation | Combat number style and/or icon indicator | Per hit | When affinity multiplier is not 1.0 |

## Acceptance Criteria

- [ ] Spell damage uses exactly: `base_damage × (1 + 0.25 × (level - 1)) × element_multiplier × class_bonus × meta_power_multiplier`.
- [ ] Enemy contact damage uses exactly: `base_contact_damage × (1 + 0.08 × elapsed_minutes)`.
- [ ] All damage outputs are floored integers and clamped to minimum 1.
- [ ] Element affinity lookup table is implemented exactly for the 6 defined enemy types and 5 elements.
- [ ] Unknown enemy type defaults to `element_multiplier = 1.0`.
- [ ] Class bonus resolution follows Kam/Batur/Mergen/Otacı rules and uses highest applicable bonus only.
- [ ] Meta progression multiplier is applied as a separate multiplier after class bonus and before flooring.
- [ ] Critical hit logic is absent in MVP damage calculations.
- [ ] No gameplay values are hardcoded in call sites; runtime values are sourced from ScriptableObjects/config data.
- [ ] Worked example outputs in this document can be reproduced exactly in validation tests.
