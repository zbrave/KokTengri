# XP & Leveling

> **Status**: Approved
> **Author**: zbrave + game-designer
> **Last Updated**: 2026-04-04
> **Implements Pillar**: Clarity Through Consistency; Death Teaches, Never Punishes; Build Diversity Over Build Power

## Overview

XP & Leveling governs how run-time combat actions convert into progression power spikes in Kök Tengri. The system listens to enemy deaths and XP pickup events, spawns and manages pooled XP gems, accumulates XP toward a level threshold, carries overflow between levels, and emits `LevelUpEvent` when thresholds are crossed. The design intent is to provide a smooth, readable progression arc where players feel frequent early gains, meaningful mid-run decisions, and stable late-run scaling without abrupt dead zones or runaway inflation. This system does not design the level-up selection content itself; it only determines when level-up selection is triggered and with what payload.

## Player Fantasy

The player should feel that every kill matters and momentum is always building. When enemies fall, gems appear as visible rewards, then magnetize toward the hero like spiritual energy returning to the shaman. Early levels should arrive quickly to create excitement and unlock build identity fast; later levels should still arrive reliably so the run keeps evolving instead of stalling. Level-ups should feel earned, predictable enough to plan around, and dramatic enough to celebrate through synchronized event, VFX, and audio responses.

## Detailed Rules

### Detailed Rules

1. **XP sources are enemy-type driven and data-owned**
   - XP values come from enemy definitions/config, not runtime hardcoding.
   - MVP-active sources:
     - Kara Kurt: `1 XP`
     - Yek Uşağı: `3 XP`
   - MVP-2 sources (already specified for forward compatibility):
     - Albastı: `5 XP`
     - Çor: `4 XP` (split halves: `2 XP` each)
     - Demirci Cin: `8 XP`
     - Göl Aynası: `6 XP` (clones: `0 XP`)
   - Elite variant rule: `elite_xp = normal_xp × 3` for any elite enemy type.

2. **Event-driven XP flow is authoritative**
   1) Enemy dies and Enemy Health publishes `EnemyDeathEvent`.
   2) XP system requests an XP gem instance from `XPGemPool` at death position.
   3) Spawn position is clamped to arena bounds before activation.
   4) XP gem enters world in idle state, then checks magnet behavior each update tick.
   5) On touch or magnet pull completion, gem publishes `XPCollectedEvent(amount, collectorId, position, runTime)`.
   6) XP system receives `XPCollectedEvent` and adds `amount` to current XP.
   7) If threshold reached, process level-up resolution loop (including overflow).
   8) Publish `LevelUpEvent(newLevel, overflowXp, runTime)` for each resolved level gain.
   9) Level-Up Selection system reacts to event and opens selection UI.

3. **XP gems are pooled gameplay entities**
   - All XP gems come from Object Pool (`XPGemPool`), never instantiate/destroy in combat.
   - Pool pre-warm target for expected screen density: `50-100` gems.
   - If pool is exhausted, system resolves overflow safely (see Edge Cases).

4. **XP gem world behavior**
   - Visual identity: small colored gem with glow for immediate readability.
   - Magnet pickup radius is configurable and modified by meta progression.
   - Pull speed accelerates as distance to player decreases.
   - Gems auto-collect after configurable timeout to prevent clutter.
   - Multiple gems may be collected in the same frame or tick.

5. **Level threshold and overflow rules**
   - Level starts at `1`.
   - Required XP per level uses exponential curve:
     - `xp_needed(level) = 10 × level^1.4`
   - On threshold crossing:
     - Subtract current threshold from accumulated XP.
     - Increase level by `1`.
     - Recompute next threshold for new level.
     - Continue while remaining XP still exceeds next threshold.
   - Overflow XP is never discarded.

6. **Level-up pacing targets (balance guardrails)**

| Run Time | Expected Level | Build State Expectation |
|---|---:|---|
| 2:00 | 5 | 2-3 spells online |
| 5:00 | 10 | 4-5 spells online |
| 10:00 | 16 | 6 spells full, upgrades begin |
| 20:00 | 24 | 6 spells, most at level 3-4 |
| 30:00 | 30-32 | 6 spells, 2-3 maxed |

7. **Magnet range baseline and meta interaction**
   - Base magnet radius: `2.0` world units.
   - Meta progression upgrade grants `+5%` radius per level.
   - Meta magnet upgrade max level: `10`.
   - XP system reads upgrade level from progression profile each run start and caches effective radius.

8. **Authority boundaries**
   - XP & Leveling owns XP accounting, threshold evaluation, overflow, and `LevelUpEvent` publishing.
   - Level-Up Selection system owns option generation, UI pause flow, and post-choice completion.
   - XP & Leveling must not own spell selection logic or elemental choice design.

### States and Transitions

| State | Entry Condition | Exit Condition | Behavior |
|---|---|---|---|
| `Accumulating` | Run starts or level-up flow completes | Threshold reached while not already waiting on selection | Accepts `XPCollectedEvent`, updates XP total, evaluates thresholds |
| `LevelUpPending` | At least one level-up resolved and selection is required | Level-Up Selection signals completion | Holds progression gate for choice resolution, can queue additional pending levels |

Valid transitions:
- `Accumulating -> LevelUpPending`
- `LevelUpPending -> Accumulating`
- `LevelUpPending -> LevelUpPending` (additional pending levels queued while selection is open)

### Transition Logic Notes

1. If one XP pickup crosses multiple thresholds, resolve all level increments sequentially in the same processing pass.
2. For each increment, publish one `LevelUpEvent` payload with post-increment level and current overflow snapshot.
3. If selection UI is already active, new level-ups increment an internal pending counter instead of reopening UI repeatedly.
4. When selection UI confirms completion, consume one pending level and either:
   - remain `LevelUpPending` if more pending level-ups exist, or
   - return to `Accumulating` when queue reaches zero.

### Interactions with Other Systems

| System | Interface In | Interface Out | Responsibility Split |
|---|---|---|---|
| Enemy Health & Damage | `EnemyDeathEvent(enemyType, position, isElite, runTime)` | XP gem spawn request | Enemy system declares death; XP system resolves reward |
| Object Pool | `TryTake(XPGemPool)` / `Return(gem)` | pooled gem instances | Pool owns memory lifecycle; XP system owns gem value/behavior |
| Event Bus | Subscribe to `EnemyDeathEvent`, `XPCollectedEvent` | Publish `LevelUpEvent` | Event Bus routes; XP system owns progression authority |
| Meta Progression | magnet upgrade level at run start | effective magnet radius input | Meta owns permanent upgrades; XP system applies runtime modifier |
| Level-Up Selection | receives `LevelUpEvent` | completion callback/signal | XP system triggers; selection system presents and resolves choice |
| HUD | current level and XP fraction read model | visual updates only | HUD displays values; never mutates XP state |
| Audio/VFX | `LevelUpEvent` and gem pickup context | feedback playback only | Presentation responds to events; no gameplay authority |

## Formulas

### Level Threshold Formula

```text
xp_needed(level) = 10 × level^1.4
```

| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| `level` | int | 1-100+ | XP runtime state | Current player level used for threshold |
| `xp_needed` | float | >=10 | calculated | XP required to advance from current level |

Expected output range (first 32 levels): approximately `10` to `~1278`.

### Overflow and Multi-Level Resolution

```text
overflow = current_xp - xp_needed(level)
current_xp = overflow
level = level + 1
repeat while current_xp >= xp_needed(level)
```

| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| `current_xp` | float | >=0 | XP runtime state | Current progress carried between checks |
| `xp_needed(level)` | float | >=10 | calculated | Threshold for current level |
| `overflow` | float | >=0 on success path | calculated | Remaining XP after one level gain |

### Magnetic Pull Speed

```text
pull_speed = base_pull_speed × (1 + proximity_acceleration × (1 - distance / magnet_radius))
```

| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| `base_pull_speed` | float | config-driven | XP gem config | Baseline gem movement speed while magnetized |
| `proximity_acceleration` | float | config-driven | XP gem config | How strongly speed increases near player |
| `distance` | float | 0..magnet_radius | runtime | Current gem-to-player distance |
| `magnet_radius` | float | >0 (or 0 edge case) | calculated | Effective pickup influence radius |

Behavior intent: gem motion starts readable at range and becomes snappy near collection.

### Meta Magnet Radius Scaling

```text
magnet_radius = base_radius × (1 + 0.05 × magnet_upgrade_level)
```

| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| `base_radius` | float | default 2.0 | progression config | Base pickup radius before upgrades |
| `magnet_upgrade_level` | int | 0-10 | meta progression save data | Permanent magnet upgrade level |
| `magnet_radius` | float | 2.0-3.0 (default tuning) | calculated | Effective in-run magnet pickup radius |

At max upgrade level 10: `magnet_radius = 2.0 × 1.5 = 3.0` units.

## Edge Cases

| Scenario | Expected Behavior | Rationale |
|---|---|---|
| Player levels up multiple times from one pickup | Process all threshold crossings sequentially in one resolution loop; queue corresponding level-up steps | Prevents loss of earned progression and keeps event order deterministic |
| XP gem spawns outside arena bounds | Clamp spawn to nearest valid arena position before activation | Prevents unreachable rewards |
| Magnet range is zero | Disable attraction; gems stay idle until direct manual pickup or timeout auto-collect | Ensures system remains functional under extreme tuning |
| Level-Up Selection is open while more XP is collected | Add level-up count to pending queue; do not interrupt current selection flow | Preserves UI clarity and avoids duplicated modal stacking |
| Large magnet burst collects many gems instantly | Support simultaneous `XPCollectedEvent` processing without dropped events or visual lockups | Maintains responsiveness during high-density rewards |
| XP gem timeout occurs during boss fight | Timed-out gem auto-collects and dispatches XP normally | Prevents clutter and unreachable progression in high-pressure encounters |
| XP gem pool limit reached (exhausted) | Oldest active gem auto-collects and returns to pool, then spawn request is retried once | Preserves reward economy while respecting memory/performance limits |
| Enemy grants zero XP variant (e.g., Göl Aynası clone) | Spawn no XP gem and publish no XP gain event for that reward source | Prevents farming exploits and preserves intended balance |
| Player dies while uncollected gems exist | End-of-run flow resolves according to run-end authority; no post-run leveling | Prevents invalid progression after run termination |

## Dependencies

| System | Direction | Nature of Dependency |
|---|---|---|
| Event Bus | XP & Leveling depends on | Consumes `EnemyDeathEvent`/`XPCollectedEvent`; emits `LevelUpEvent` |
| Enemy Health & Damage | XP & Leveling depends on | Provides authoritative death events including enemy context and position |
| Object Pool | XP & Leveling depends on | Provides and recycles XP gem instances via `XPGemPool` |
| Level-Up Selection | Level-Up Selection depends on XP & Leveling | Opens when level-up event arrives and closes with completion signal |
| Meta Progression | XP & Leveling depends on | Supplies magnet upgrade level for run-time radius scaling |
| HUD | HUD depends on XP & Leveling | Displays current level and XP progress bar |
| Audio/VFX | Audio/VFX depends on XP & Leveling | Plays pickup and level-up feedback using event payloads |
| Core progression config data | XP & Leveling depends on | Holds curve parameters, gem behavior values, timeout, and queue limits |

## Tuning Knobs

| Parameter | Current Value | Safe Range | Effect of Increase | Effect of Decrease |
|---|---:|---:|---|---|
| `xp_curve_base` | 10 | 6-20 | Faster early levels if reduced scaling pressure not changed | Slower opening progression if too low with high exponent mismatch |
| `xp_curve_exponent` | 1.4 | 1.2-1.7 | Steeper late-run pacing, fewer very high levels | Flatter progression, risk of over-leveling |
| `base_magnet_radius` | 2.0 units | 1.0-4.0 | Easier collection, less movement tax | More manual pickup friction |
| `magnet_upgrade_step` | 5% per level | 2%-8% | Meta upgrades feel stronger | Meta upgrades feel weaker |
| `magnet_upgrade_max_level` | 10 | 5-15 | Higher long-term scaling cap | Lower cap, less late retention value |
| `base_pull_speed` | config-driven | per feel tests | Gems arrive faster once attracted | Pickup feedback feels sluggish |
| `proximity_acceleration` | config-driven | per feel tests | Stronger close-range snap-in | More uniform and less dynamic movement |
| `gem_auto_collect_timeout` | config-driven | 2-20s | Less clutter, fewer missed pickups | More persistent world pickups |
| `xp_gem_pool_prewarm` | 50-100 | 30-150 | Better burst stability, higher memory | Lower memory, more pool pressure |
| `levelup_pending_queue_cap` | config-driven | 1-20 | Handles extreme burst XP safely | Risk of dropped pending levels if cap too low |

## Acceptance Criteria

- [ ] XP values are sourced from enemy data and support all specified enemy types (including MVP-2 mappings and elite multiplier rule).
- [ ] On `EnemyDeathEvent`, XP gem spawn is requested from Object Pool and position is clamped to arena bounds.
- [ ] XP gems use magnetic pickup with configurable radius, accelerating pull speed, timeout auto-collect, and simultaneous collection support.
- [ ] Effective magnet radius follows `base_radius × (1 + 0.05 × magnet_upgrade_level)` with base radius `2.0` and max upgrade level `10`.
- [ ] XP accumulation uses `xp_needed(level) = 10 × level^1.4` with level starting at `1`.
- [ ] Overflow XP is preserved and supports multiple level-ups from a single pickup event.
- [ ] XP state machine supports `Accumulating` and `LevelUpPending`, including queued level-ups while selection is already open.
- [ ] `LevelUpEvent(newLevel, overflowXp, runTime)` is published for each resolved level gain in deterministic order.
- [ ] If XP gem pool is exhausted, system applies documented fallback (`oldest auto-collect`) and does not lose earned XP.
- [ ] Progression pacing can be tuned to match expected targets: level 5 @2m, 10 @5m, 16 @10m, 24 @20m, 30-32 @30m.
- [ ] System contains no direct level-up option design logic (owned by Level-Up Selection system).
- [ ] All adjustable values are config-driven; no hardcoded gameplay tuning in implementation.
