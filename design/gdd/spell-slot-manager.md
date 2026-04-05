# Spell Slot Manager System

> **Status**: Approved
> **Author**: zbrave + game-designer
> **Last Updated**: 2026-04-04
> **Implements Pillar**: Build Diversity Over Build Power, Discovery, Death Teaches Never Punishes

## Overview

Spell Slot Manager owns the full runtime lifecycle of active spells during a run. It is the execution layer that stores spell instances, levels, cooldown timers, and activation readiness for up to a configurable maximum number of slots (default 6). Spell Crafting decides when a spell should be created or upgraded, but Spell Slot Manager is the authoritative system that applies those changes, tracks per-spell timing, and triggers Spell Effects when activations occur. This system exists to make auto-attacking spell behavior deterministic, data-driven, and scalable as spell count and encounter intensity increase.

## Player Fantasy

The player should feel like a disciplined mythic caster whose prepared powers operate with relentless rhythm. Crafted spells become a living arsenal that cycles automatically, creating the sensation of a self-sustaining ritual engine while the player focuses on movement, positioning, and survival. Upgrading a spell should feel like tightening that ritual: shorter downtime, more frequent impact, and visibly stronger combat cadence without requiring extra mechanical input.

## Detailed Rules

### Detailed Rules

1. **System ownership boundary**
   - Spell Slot Manager owns active spell runtime instances and per-slot state.
   - Spell Crafting remains decision authority for create/upgrade choices.
   - Spell Effects owns behavior implementation (projectile pathing, orbit motion, AoE shape, aura footprint).

2. **Slot capacity and slot model**
   - Maximum active spell slots per run are provided by `SpellSlotConfigSO.MaxSlots` (default 6, tunable).
   - Each occupied slot stores:
     - `spellId`
     - `level` (1-5)
     - `cooldownRemaining`
     - `activationElapsed`
     - `isActive`
     - `isContinuous`
   - Empty slots store no runtime spell state.

3. **Mandatory external interface contract (called by Spell Crafting)**

```text
bool HasFreeSpellSlot()
bool TryCreateSpell(spellId)
bool TryUpgradeSpell(spellId)
```

   - `HasFreeSpellSlot()` returns `true` when occupied slots are below configured max.
   - `TryCreateSpell(spellId)` creates a new spell at level 1 in an empty slot; returns `false` if no free slot.
   - `TryUpgradeSpell(spellId)` increments level by +1 up to max level 5; returns `false` if spell is already level 5 or missing.

4. **Initialization and lifecycle**
   - On `RunStartEvent`, initialize slot container to empty and load config references.
   - During run update, process per-spell timer state using `Update + deltaTime` only.
   - On `RunEndEvent`, stop processing and clear all slots, including active cooldown state.

5. **Cooldown and activation model**
   - Periodic spells maintain independent cooldown timers.
   - When a periodic spell reaches ready state, Slot Manager requests Spell Effects execution.
   - After activation request, periodic spell re-enters cooldown using current level-scaled duration.
   - Continuous spells do not enter cooldown loop and remain active while the slot is occupied.

6. **Spell type timing policy (execution delegated to Spell Effects)**
   - **Orbit** (`Alev Halkası`, `Kaya Kalkanı`): continuous, no cooldown cycle.
   - **Projectile** (`Kılıç Fırtınası`, `Ok Yağmuru`): periodic, cooldown-based trigger.
   - **AoE** (`Deprem`, `Buhar Patlaması`): periodic pulse, cooldown-based trigger.
   - **Aura** (`Batanlık`): continuous area effect, no cooldown cycle.
   - **Passive** (`Şifa Pınarı`): periodic activation, no visible projectile.

7. **Failure containment rule**
   - If Spell Effects reports execution failure for an activation request, Slot Manager still consumes that cycle and continues normal timer progression.
   - A single effect failure must never freeze slot progression.

8. **No forbidden implementation patterns**
   - No UnityEvents in this system's runtime flow.
   - No coroutines for cooldown/activation timing.
   - No `FindObjectOfType` in runtime updates.
   - No hardcoded balance values; all tunables come from ScriptableObjects.

### States and Transitions

#### A) Slot Occupancy State

| State | Entry Condition | Exit Condition | Behavior |
|---|---|---|---|
| Empty | Run start or slot cleanup | `TryCreateSpell` success | Holds no spell runtime data |
| Occupied | Spell created in slot | Run end cleanup | Contains active per-spell runtime state |

#### B) Per-Spell Progression State

| State | Entry Condition | Exit Condition | Behavior |
|---|---|---|---|
| Active-Lv1 | `TryCreateSpell` success | `TryUpgradeSpell` success | Spell operates at level 1 scaling |
| Active-Lv2 | Upgrade from Lv1 | Upgrade to Lv3 | Spell operates at level 2 scaling |
| Active-Lv3 | Upgrade from Lv2 | Upgrade to Lv4 | Spell operates at level 3 scaling |
| Active-Lv4 | Upgrade from Lv3 | Upgrade to Lv5 | Spell operates at level 4 scaling |
| Maxed-Lv5 | Upgrade from Lv4 | Slot removed only on run cleanup | Cannot be upgraded further |

Primary progression path:

`Empty -> Active-Lv1 -> Active-Lv2 -> Active-Lv3 -> Active-Lv4 -> Maxed-Lv5`

#### C) Per-Spell Activation Sub-State (periodic spells)

| State | Entry Condition | Exit Condition | Behavior |
|---|---|---|---|
| Ready | Spell created or cooldown finished | Activation request sent | Eligible for immediate activation |
| OnCooldown | Activation request sent | `cooldownRemaining <= 0` | Timer decreases by delta time |

Sub-state loop:

`Ready -> OnCooldown -> Ready`

#### D) Continuous Spell Timing Sub-State

| State | Entry Condition | Exit Condition | Behavior |
|---|---|---|---|
| ContinuousActive | Continuous spell created | Run end cleanup | No cooldown loop; always active |

### Interactions with Other Systems

| System | Interface In | Interface Out | Responsibility Split |
|---|---|---|---|
| Spell Crafting | `HasFreeSpellSlot()`, `TryCreateSpell(spellId)`, `TryUpgradeSpell(spellId)` | Success/failure and updated level state | Crafting decides, Slot Manager executes and persists runtime state |
| Event Bus | Subscribes to `RunStartEvent`, `RunEndEvent` | Optional publish: slot lifecycle diagnostics/events | Event Bus handles transport; Slot Manager handles state transitions |
| Spell Effects | `RequestSpellActivation(spellId, level, context)` | `ActivationSucceeded/ActivationFailed` result | Slot Manager determines when to activate; Effects system determines what activation does |
| HUD | `GetSpellSlotSnapshot()` (icon id, level, cooldown ratio, active state) | None required | Slot Manager is authoritative data source; HUD is display-only |
| Damage Calculator | `GetSpellLevel(spellId)` and runtime context for activation | Damage output values used by effects | Damage Calculator computes magnitude; Slot Manager supplies level identity context |
| Pause/Game State | `OnPauseChanged(isPaused)` | Timer freeze/resume status | Pause authority controls time progression; Slot Manager freezes timer deltas when paused |

## Formulas

### Cooldown Duration by Spell Level

```text
cooldown_duration = base_cooldown × (1 - cooldown_reduction_per_level × (level - 1))
```

| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| `base_cooldown` | float | 0.1-20.0 s | `SpellDefinitionSO` | Base cooldown at level 1 |
| `cooldown_reduction_per_level` | float | 0.00-0.20 | `SpellSlotConfigSO` or `SpellDefinitionSO` | Percentage reduction per level step |
| `level` | int | 1-5 | Slot runtime state | Current spell level |

**Expected output range**: `0.1 s` to `20.0 s` after clamping.

**Clamping rule**: if computed duration drops below minimum allowed cooldown, clamp to configured minimum to avoid zero/negative cooldown.

### Activation Timer Progression (Periodic Spells)

```text
activation_elapsed_next = activation_elapsed_current + delta_time_when_unpaused
cooldown_remaining_next = max(0, cooldown_duration - activation_elapsed_next)
```

| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| `activation_elapsed_current` | float | 0-cooldown_duration | Slot runtime state | Time accumulated since last activation |
| `delta_time_when_unpaused` | float | 0-0.1 s/frame | Unity Update delta | Zero while paused |
| `cooldown_remaining_next` | float | 0-cooldown_duration | Derived | Time left until next activation |

### DPS Contribution Estimate (Per Spell)

```text
dps = (damage_per_hit × hits_per_activation) / cooldown_duration
```

| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| `damage_per_hit` | float | 1-9999 | Damage Calculator | Effective damage after multipliers |
| `hits_per_activation` | int | 1-100 | Spell Effects definition | Number of hit events per activation |
| `cooldown_duration` | float | 0.1-20.0 s | Cooldown formula output | Time between activations |

**Expected output range**: spell-specific; used for internal balance validation, not direct player UI.

### Slot Utilization

```text
slot_utilization = occupied_slots / max_slots
```

| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| `occupied_slots` | int | 0-max_slots | Slot container | Number of currently occupied slots |
| `max_slots` | int | 4-8 (default 6) | `SpellSlotConfigSO` | Configured cap for run |

**Expected output range**: `0.0` to `1.0`.

## Edge Cases

| Scenario | Expected Behavior | Rationale |
|---|---|---|
| All 6 slots full and Spell Crafting calls `TryCreateSpell` | Return `false`, no slot mutation | Preserve deterministic capacity rules |
| Spell is already level 5 and `TryUpgradeSpell` is called | Return `false`, level remains 5 | Enforce max-level cap |
| All 6 spells are level 5 | Slot Manager reports no valid spell progression | Level-up flow routes to stat boosts outside this system |
| Spell effect execution fails during activation | Consume cycle and continue cooldown progression | Prevent deadlock/frozen slot behavior |
| Run ends during active cooldown | Clear all runtime spell states on `RunEndEvent` | Prevent state leak into next run |
| Multiple periodic spells become ready in same frame | Process activations sequentially in deterministic slot index order | Keep timing deterministic and testable |
| Pause toggled while cooldown is running | Freeze timer deltas while paused, resume from same remaining time | Preserve player expectation and fairness |
| Spell definition missing or invalid at creation time | `TryCreateSpell` returns `false` and logs validation error | Fail safely without corrupting slot state |

## Dependencies

| System | Direction | Nature of Dependency |
|---|---|---|
| Spell Crafting | Spell Slot Manager receives commands from Spell Crafting | Crafting decides create/upgrade intent; Slot Manager executes lifecycle state changes |
| Event Bus | Spell Slot Manager depends on Event Bus | Subscribes to run lifecycle events and optional diagnostics publishing |
| Spell Effects | Spell Slot Manager depends on Spell Effects | Slot Manager requests activation; Effects executes mechanics and visuals |
| HUD | HUD depends on Spell Slot Manager | HUD reads slot snapshots (icon, level, cooldown, active state) |
| Damage Calculator | Spell Effects and damage pipeline depend on Slot Manager context | Slot Manager provides spell id + level context for damage computation |
| Pause/Game State System | Spell Slot Manager depends on pause state input | Pause state controls timer freeze/resume behavior |
| Config Data (`SpellSlotConfigSO`, `SpellDefinitionSO`) | Spell Slot Manager depends on config assets | Capacity, cooldown bases, and reduction parameters are data-driven |

## Tuning Knobs

| Parameter | Current Value | Safe Range | Effect of Increase | Effect of Decrease |
|---|---|---|---|---|
| `maxSlots` | 6 | 4-8 | More build breadth, higher UI and balance complexity | Fewer simultaneous spells, stronger build commitment |
| `maxSpellLevel` | 5 | 3-7 | Longer progression runway per spell | Faster max-out, earlier shift to stat boosts |
| `baseCooldown` (per spell) | From `SpellDefinitionSO` | 0.1-20.0 s | Slower activation cadence, lower burst frequency | Faster cadence, higher pressure on performance/balance |
| `cooldownReductionPerLevel` | Config-driven per spell family | 0.00-0.20 | Stronger reward for upgrades, larger late-level tempo gain | Flatter progression tempo |
| `minCooldownClamp` | Config-driven | 0.05-1.0 s | Safer performance floor and readability | Higher potential activation spam risk |
| `sameFrameActivationCap` (optional safety cap) | Config-driven | 1-6 | Limits burst spikes and frame congestion | More simultaneous burst behavior |
| `pauseFreezesCooldowns` | Enabled | Enabled/Disabled | Predictable tactical pauses | Disabled mode keeps world simulation strict but less player-friendly |

## Visual/Audio Requirements

| Event | Visual Feedback | Audio Feedback | Priority |
|---|---|---|---|
| Spell created into empty slot | Slot icon appears with quick fill animation | Creation chime | High |
| Spell upgraded | Slot icon pulse + level number increment flash | Upgrade tone | High |
| Spell enters cooldown | Cooldown radial starts draining | Soft tick/none | Medium |
| Spell ready state reached | Cooldown radial full and brief highlight | Ready ping (subtle) | Medium |
| Activation request fired | Slot flashes to indicate trigger | Spell-specific trigger audio (from Spell Effects) | High |
| Create denied (full slots) | Small warning text near slots | Soft warning cue | Medium |

## UI Requirements

| Information | Display Location | Update Frequency | Condition |
|---|---|---|---|
| Slot occupancy (0-6) | HUD spell bar | On create/cleanup | During run |
| Per-slot icon | HUD spell bar | On create | Occupied slot only |
| Per-slot level (1-5) | HUD spell icon overlay | On upgrade | Occupied slot only |
| Per-slot cooldown ratio | HUD radial overlay | Every frame while periodic and unpaused | Occupied periodic slot only |
| Active/continuous state marker | HUD slot badge | On state change | Continuous spell slots |
| Full-slot warning message | HUD toast region | On denied create attempt | When `TryCreateSpell` returns false due to capacity |

## Acceptance Criteria

- [ ] `HasFreeSpellSlot()` returns accurate capacity state against `SpellSlotConfigSO.MaxSlots`.
- [ ] `TryCreateSpell(spellId)` creates level 1 spell state only when a free slot exists.
- [ ] `TryCreateSpell(spellId)` returns `false` and leaves state unchanged when slots are full.
- [ ] `TryUpgradeSpell(spellId)` increments level by exactly +1 up to level 5.
- [ ] `TryUpgradeSpell(spellId)` returns `false` for missing spell or maxed level 5 spell.
- [ ] Periodic spells run `Ready -> OnCooldown -> Ready` using Update + delta time only.
- [ ] Continuous spells never enter cooldown cycle and remain active while slot exists.
- [ ] Cooldown duration is computed from formula using config values, with minimum clamp protection.
- [ ] Activation requests are sent to Spell Effects when periodic cooldown expires.
- [ ] If Spell Effects reports failure, slot timing continues without freeze.
- [ ] Multiple ready activations in one frame are processed sequentially in deterministic slot order.
- [ ] On pause, cooldown timers freeze; on unpause, timers continue from preserved remaining value.
- [ ] On `RunStartEvent`, slot state initializes cleanly; on `RunEndEvent`, all slot state is fully cleared.
- [ ] HUD can query per-slot spell id, level, cooldown state, and active marker without mutating gameplay state.
- [ ] All tunable values are data-driven (`SpellSlotConfigSO`, `SpellDefinitionSO`) with zero hardcoded gameplay balance constants.

## Open Questions

| Question | Owner | Deadline | Resolution |
|---|---|---|---|
| Should `sameFrameActivationCap` ship enabled in MVP or remain disabled by default? | game-designer | Before combat polish pass | Pending playtest load data |
| Should failed activation attempts produce player-facing warning, or remain silent telemetry-only? | ux-designer | Before HUD polish lock | Pending readability test |
