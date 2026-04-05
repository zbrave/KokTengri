# Enemy Behaviors

> **Status**: Draft
> **Author**: gameplay-programmer
> **Last Updated**: 2026-04-04
> **Implements Pillar**: Clarity Through Consistency; Death Teaches, Never Punishes

## Overview

Enemy Behaviors defines how MVP-1 enemies navigate toward the player, maintain readable spacing, and apply continuous battlefield pressure in Kök Tengri. For MVP-1, this system includes only two enemy archetypes: Kara Kurt (fast, fragile pack pressure) and Yek Uşağı (slow, durable group pressure). The behavior loop is intentionally lightweight: enemies sample player position on a configurable AI tick, compute chase plus separation, and move using interpolated motion between ticks for smooth visuals. The system exists to create predictable but threatening pursuit dynamics at survivor-scale densities while staying within the project AI budget of 2ms total per frame at 300 enemies.

## Player Fantasy

The player should feel hunted by living forces, not random colliders. Kara Kurt groups should feel like restless predators trying to surround and bite from multiple angles, forcing constant repositioning. Yek Uşağı groups should feel like an oppressive underworld wall that slowly compresses safe space through weight and persistence. In both cases, pressure should be readable: players should quickly understand why enemies are where they are, how to break surrounding patterns, and how movement choices affect survival.

## Detailed Design

### Detailed Rules

1. **Scope boundary (MVP-1 only)**
   - This document defines behaviors for:
     - Kara Kurt
     - Yek Uşağı
   - Albastı, Çor, Demirci Cin, and Göl Aynası are explicitly out of scope for this system version.

2. **Behavior ownership and boundaries**
   - Enemy Behaviors owns movement intent, chase logic, separation, and per-type behavior tuning.
   - Enemy Behaviors does not own HP, death, damage formulas, or element multipliers.
   - Contact damage trigger ownership remains in Enemy Health & Damage; this system only places enemies in contact range.

3. **Target acquisition model**
   - All active enemies chase the latest known player world position.
   - Player position source is read-only from Player Movement event/state output.
   - Behavior target updates occur on AI ticks, not every render frame.

4. **Pathing model (MVP simplification)**
   - No navmesh and no expensive pathfinding in MVP-1.
   - Movement direction is direct chase vector plus separation influence.
   - Arena collision and clamping enforce valid positions.

5. **AI cadence and interpolation**
   - AI logic runs at configurable `tickRateHz` (default `5 Hz`, every `0.2s`).
   - Between ticks, visible motion uses interpolation from previous tick result to current tick target.
   - Interpolation is visual smoothing only; authoritative behavior updates remain tick-based.

6. **Separation behavior**
   - Enemies apply local repulsion force to avoid exact overlap and reduce stack artifacts.
   - Separation applies to all enemy instances in range, including mixed enemy types.
   - Separation strength is data-driven and can vary by enemy type.

7. **Kara Kurt behavior profile**
   - First appearance: minute 0.
   - Tactical role: fast, weak, high-count pack pursuer.
   - Base combat references (owned by Enemy Health & Damage): HP 8, base contact damage 5.
   - Element profile reference: weak to Od, resistant to Yel.
   - Movement style:
     - High move speed relative to MVP baseline.
     - Moderate separation to create loose ring pressure around player.
     - Intended to avoid full point-stacking while still feeling swarming.

8. **Yek Uşağı behavior profile**
   - First appearance: minute 2.
   - Tactical role: slow, durable, heavy pressure pusher.
   - Base combat references (owned by Enemy Health & Damage): HP 25, base contact damage 10.
   - Element profile reference: weak to Yel, resistant to Temür.
   - Movement style:
     - Lower move speed than Kara Kurt.
     - Lower separation than wolves so the group holds denser formation.
     - Includes slight knockback resistance multiplier (consumed from movement reaction integration).
     - On near-contact, contributes to group push pressure behavior.

9. **Group pressure behavior intent**
   - Kara Kurt packs should naturally distribute with slight angular spread around player position.
   - Yek Uşağı groups should tend to compress lanes and apply sustained area denial through density.
   - Neither archetype should permanently lock into exact same coordinates.

10. **Arena boundaries and clamping**
    - Enemy desired position is clamped to playable arena bounds.
    - Chasing a player near an arena edge keeps enemies valid while preserving pursuit intent.

11. **Player death behavior gate**
    - On player death, all enemy movement transitions to non-combat behavior gate:
      - Option A (default): full stop.
      - Option B (tunable): slow disperse vector at low speed.
    - No further chase integration while player is dead.

12. **Data-driven architecture**
    - All behavior tuning values are authored in ScriptableObjects.
    - Runtime behavior code reads from per-type `EnemyBehaviorConfigSO` instances.
    - No hardcoded behavior numbers in production runtime paths.

13. **Performance constraints**
    - At 300 concurrent enemies, total enemy AI update budget is 2ms per frame equivalent.
    - Tick-rate and neighborhood evaluation parameters must be tunable to preserve budget.
    - Behavior implementation must avoid high-cost allocations and avoid per-frame expensive lookups.

### States and Transitions

Per-enemy behavior state machine for MVP-1 movement:

| State | Entry Condition | Exit Condition | Behavior |
|---|---|---|---|
| Spawned | Enemy activated from pool | Spawn setup complete | Initializes behavior runtime cache and config references |
| Pursuing | Spawn setup complete and player alive | Contact range reached OR player dead OR enemy disabled | Tick-based chase + separation integration toward player |
| Pressuring | Within near-contact distance to player | Leaves near-contact distance OR player dead OR enemy disabled | Maintains pursuit with higher contact persistence intent |
| Halted | Player death gate active | Run reset or enemy despawn | Movement disabled (or low-speed disperse if enabled) |
| Disabled | Enemy returned to pool or system deactivated | Re-activation from pool | No behavior updates |

Valid transitions:
- `Spawned -> Pursuing`
- `Pursuing -> Pressuring`
- `Pressuring -> Pursuing`
- `Pursuing -> Halted`
- `Pressuring -> Halted`
- `Halted -> Disabled`
- `Pursuing -> Disabled`
- `Pressuring -> Disabled`

Invalid transitions:
- `Disabled -> Pressuring` (must pass through Spawned/Pursuing setup)
- `Halted -> Pressuring` while player remains dead

### Interactions with Other Systems

| System | Interface In | Interface Out | Responsibility Split |
|---|---|---|---|
| Player Movement | Current player world position, player alive/dead state, arena bounds context | None | Player Movement is source of player position authority; Enemy Behaviors consumes read-only |
| Enemy Health & Damage | Near-contact status for contact opportunities (implicit via proximity) | Enemy position and proximity context | Health & Damage owns hit application; Enemy Behaviors owns movement into range |
| Enemy Spawner/Waves | Spawned enemy type and lifecycle activation/deactivation | None | Spawner decides what/when to spawn; Enemy Behaviors decides how each active enemy moves |
| Difficulty Scaling | Optional global behavior multipliers (if enabled) | None | Difficulty owns multiplier policy; Enemy Behaviors applies received movement modifiers |
| Arena/Level Bounds | Arena clamp limits and blocked-region constraints | Clamped movement result | Arena system defines legal space; Enemy Behaviors respects legal bounds |
| Object Pool | Activation and return-to-pool lifecycle events | Reset-ready behavior runtime state | Pool owns memory lifecycle; Enemy Behaviors resets cached per-instance state |
| Performance Telemetry | Frame/tick profiling hooks | AI cost metrics by type/count | Telemetry captures cost; Enemy Behaviors uses data to tune tick frequency and neighborhood checks |

## Formulas

### Chase Direction

```text
chase_direction = normalize(player_position - enemy_position)
```

| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| `player_position` | Vector2 | arena bounds | Player Movement output | Current target position for enemy pursuit |
| `enemy_position` | Vector2 | arena bounds | Enemy runtime state | Current enemy world position |
| `chase_direction` | Vector2 | unit vector or zero | calculated | Direct pursuit direction |

Expected output range: unit vector magnitude `0..1`.

### Separation Force

```text
separation_force = Σ( normalize(enemy_pos - other_pos) / distance_squared )
for all other enemies within separation_radius and distance_squared > epsilon
```

| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| `enemy_pos` | Vector2 | arena bounds | Enemy runtime state | Subject enemy position |
| `other_pos` | Vector2 | arena bounds | Nearby enemy positions | Neighbor candidate position |
| `distance_squared` | float | >0 | calculated | Squared distance for inverse weighting |
| `separation_radius` | float | 0.25-3.0 | EnemyBehaviorConfigSO | Neighborhood radius for repulsion |
| `epsilon` | float | small positive | config/runtime constant | Prevents divide-by-zero near exact overlap |
| `separation_force` | Vector2 | unbounded pre-clamp | calculated | Aggregate local repulsion vector |

Expected behavior: stronger repulsion at very small distances, fading with distance.

### Final Steering Direction

```text
final_direction = normalize(chase_direction + separation_weight × separation_force)
```

| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| `chase_direction` | Vector2 | unit vector or zero | calculated | Goal-seeking component |
| `separation_weight` | float | 0.0-5.0 | EnemyBehaviorConfigSO | Relative repulsion influence |
| `separation_force` | Vector2 | runtime vector | calculated | Aggregated local spacing force |
| `final_direction` | Vector2 | unit vector or zero | calculated | Steering result used for movement |

Expected behavior: maintains pursuit intent while reducing overlap and stack artifacts.

### Tick Movement Integration

```text
position_next = position_current + final_direction × move_speed × tick_delta_time
```

| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| `position_current` | Vector2 | arena bounds | Enemy runtime state | Current authoritative position |
| `final_direction` | Vector2 | unit vector or zero | calculated | Steering direction |
| `move_speed` | float | 0.1-10.0 | EnemyBehaviorConfigSO | Per-type movement speed |
| `tick_delta_time` | float | >0 | from tick cadence | Time elapsed since last AI tick |
| `position_next` | Vector2 | clamped to arena | calculated | Next authoritative position before interpolation |

Expected output range: step distance depends on speed and tick duration; clamped to arena.

### Interpolated Render Position

```text
render_position = lerp(last_tick_position, current_tick_position, tick_alpha)
```

Where:

```text
tick_alpha = clamp01( time_since_last_tick / tick_interval )
```

| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| `last_tick_position` | Vector2 | arena bounds | cached runtime state | Previous authoritative tick result |
| `current_tick_position` | Vector2 | arena bounds | current tick result | Latest authoritative tick result |
| `time_since_last_tick` | float | 0..tick_interval | runtime timer | Elapsed render time after tick |
| `tick_interval` | float | 0.05-0.5 | from `tickRateHz` | Duration between AI ticks |
| `tick_alpha` | float | 0..1 | calculated | Interpolation factor |

Expected behavior: smooth visual motion despite coarse AI logic cadence.

### AI Tick Budget Envelope

```text
avg_tick_cost_ms = total_ai_tick_time_ms / active_enemy_count
total_frame_equivalent_ai_ms = total_ai_tick_time_ms × tick_rate_hz / target_fps
```

Budget target:

```text
total_frame_equivalent_ai_ms <= 2.0
```

| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| `total_ai_tick_time_ms` | float | >=0 | profiler telemetry | Total AI update duration for one tick |
| `active_enemy_count` | int | 1-300+ | runtime | Number of active enemies evaluated |
| `tick_rate_hz` | float | 2-10 | EnemyBehaviorConfigSO/global | Logic update frequency |
| `target_fps` | int | 30-60 | technical preference | Frame normalization basis |
| `total_frame_equivalent_ai_ms` | float | >=0 | calculated | AI cost normalized per render frame |

## Edge Cases

| Scenario | Expected Behavior | Rationale |
|---|---|---|
| Player at arena edge while pursued | Enemies continue chase intent, but position results are clamped to legal arena bounds | Maintains pressure without leaving playable space |
| 300 Kara Kurt active simultaneously | AI remains under 2ms/frame-equivalent by tick cadence and lightweight steering math; tick rate can be reduced within safe range if needed | Preserves mobile performance target |
| Enemy trapped in dense overlap cluster | Separation force resolves overlap over subsequent ticks and prevents permanent exact stacking | Prevents unreadable collision piles |
| Player dies mid-wave | All enemies leave chase behavior and enter Halted gate (stop or low-speed disperse based on config) | Ensures deterministic end-of-run behavior |
| Kara Kurt and Yek Uşağı mixed in same area | Separation queries include all nearby enemies, not same-type only | Avoids cross-type clipping and stack artifacts |
| Enemy spawns exactly on another enemy | Immediate first-tick separation impulse pushes entities apart before sustained pursuit | Stabilizes spawn collisions |
| Very low tick rate tuning (e.g., 2 Hz) | Authoritative logic remains correct but interpolation smooths visible movement; warn in tuning review if responsiveness drops too far | Allows emergency performance fallback while preserving readability |
| Very high separation weight on Kara Kurt | Wolves spread too far and reduce threat concentration; tune back toward pack pressure profile | Protects intended fantasy of predatory swarm |
| Too low separation weight on Yek Uşağı | Group collapses into unreadable stack and push pressure becomes visually noisy | Keeps pressure readable and fair |
| Arena corner congestion with both enemy types | Clamp + separation prevents infinite pinning loops; enemies continue to seek nearest valid pursuit vectors | Avoids deadlock behavior near bounds |

## Dependencies

| System | Direction | Nature of Dependency |
|---|---|---|
| Player Movement | Enemy Behaviors depends on Player Movement | Reads authoritative player position and alive/dead state for chase gating |
| Enemy Health & Damage | Lateral integration | Supplies proximity/contact opportunities; does not own hit resolution |
| Enemy Spawner/Waves | Enemy Behaviors depends on Spawner | Receives enemy type activation, spawn timing, and despawn lifecycle |
| Arena/Level Bounds | Enemy Behaviors depends on Arena | Uses playable bounds for movement clamping |
| Difficulty Scaling | Optional dependency | Applies optional global speed/tick multipliers if enabled by run profile |
| Object Pool | Enemy Behaviors depends on Pool | Resets and reinitializes behavior state per pooled lifecycle |
| Data Layer (`EnemyBehaviorConfigSO`) | Enemy Behaviors depends on data config | Sources per-type move speed, separation parameters, tick rate, and special toggles |
| Performance Telemetry/Profiler | Telemetry depends on Enemy Behaviors outputs | Collects AI runtime metrics for budget compliance checks |

## Tuning Knobs

| Parameter | Current Value | Safe Range | Effect of Increase | Effect of Decrease |
|---|---:|---:|---|---|
| `GlobalTickRateHz` | 5.0 | 2.0-10.0 | More responsive steering, higher CPU cost | Lower CPU cost, less responsive pursuit |
| `KaraKurtMoveSpeed` | 3.8 | 2.8-5.0 | Faster encirclement and chase threat | Easier kiting and escape windows |
| `KaraKurtSeparationWeight` | 0.55 | 0.20-1.20 | Wider pack spread, less stacking | Tighter swarms, higher overlap risk |
| `KaraKurtSeparationRadius` | 0.9 | 0.5-1.8 | Earlier spacing response | Later spacing response, tighter clumps |
| `YekUsagiMoveSpeed` | 1.9 | 1.2-3.0 | Stronger map pressure and lane closure | Slower pressure buildup |
| `YekUsagiSeparationWeight` | 0.25 | 0.10-0.80 | Less clumping, more distributed wall | Denser clumping and stronger push mass |
| `YekUsagiSeparationRadius` | 0.7 | 0.4-1.5 | Earlier anti-overlap behavior | Later anti-overlap behavior |
| `YekUsagiKnockbackResistance` | 0.25 | 0.0-0.6 | Harder to displace, stronger relentless pressure | Easier displacement, less tank identity |
| `MixedTypeSeparationEnabled` | true | true/false | Reduces cross-type overlaps | If false, more clipping and visual stacking |
| `HaltBehaviorMode` | Stop | Stop/Disperse | Disperse can improve visual readability after death | Stop gives deterministic freeze |
| `DisperseSpeedMultiplier` | 0.25 | 0.1-0.5 | Faster post-death drift | Slower post-death drift |
| `MaxNeighborsPerEnemy` | 12 | 4-24 | Better spacing quality, more CPU cost | Lower CPU cost, rougher spacing |

## Acceptance Criteria

- [ ] MVP-1 enemy behavior scope contains only Kara Kurt and Yek Uşağı.
- [ ] All active enemies chase authoritative player position on configurable AI ticks (default 5Hz).
- [ ] Behavior uses direct chase steering (no navmesh/pathfinding in MVP-1).
- [ ] Separation force prevents persistent exact overlap and visible hard stacking.
- [ ] Separation applies across mixed enemy types, not same-type only.
- [ ] Kara Kurt profile demonstrates fast pursuit with loose pack spread around player.
- [ ] Yek Uşağı profile demonstrates slower, denser pressure with slight knockback resistance.
- [ ] Enemy motion remains visually smooth via interpolation between AI ticks.
- [ ] Position results are clamped to arena bounds even during edge pursuit.
- [ ] On player death, enemies transition out of chase to configured Halt behavior.
- [ ] All behavior parameters are sourced from `EnemyBehaviorConfigSO` (data-driven tuning).
- [ ] No hardcoded gameplay balance values in runtime behavior logic.
- [ ] Runtime path avoids forbidden patterns (`FindObjectOfType`, gameplay-critical coroutines, direct UI ownership).
- [ ] Performance target validated: at 300 active enemies, AI stays within 2ms/frame-equivalent budget.
