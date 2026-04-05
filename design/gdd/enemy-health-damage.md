# Enemy Health & Damage

> **Status**: Approved
> **Author**: zbrave + gameplay-programmer
> **Last Updated**: 2026-04-04
> **Implements Pillar**: Clarity Through Consistency; Death Teaches, Never Punishes

## Overview

Enemy Health & Damage is the combat authority for per-enemy HP lifecycle, damage intake, death resolution, and enemy-to-player contact damage in Kök Tengri. The system initializes each enemy's max HP at spawn from data (`EnemyDefinitionSO`) and run-time difficulty multipliers provided by Enemy Spawner/Difficulty Scaling. During combat, it receives hit requests from spell effects, applies element-adjusted damage from Damage Calculator, transitions enemy state, emits required combat events, spawns drops through pooled objects, and returns dead enemies to Object Pool instead of destroying them. The system also enforces contact damage throttling and brief hit invincibility windows to keep combat fair, deterministic, and readable on mobile performance budgets.

## Player Fantasy

The player should feel that enemies are physically present and tactically readable: matching elements melts weak targets, resistant targets survive longer, elites feel dangerous but rewarding, and boss takedowns feel ceremonial. Every successful hit gives immediate confirmation through damage numbers and HP depletion, while enemy contact damage feels threatening but not cheap due to per-target cooldown timing. Death outcomes should feel consistent: enemies collapse, rewards appear, and the battlefield continues smoothly without hitching or duplicate triggers. The emotional target is "I understand why this enemy died, why I took damage, and what I earned."

## Detailed Rules

### Detailed Rules

1. **System ownership and boundaries**
   - Owns enemy HP state (`maxHp`, `currentHp`, death flags, hit timing gates).
   - Owns enemy contact damage gating and event emission for player damage.
   - Does not own movement, targeting, or attack pattern logic (Enemy Behaviors own that).
   - Does not own XP pickup collection logic (XP & Leveling owns pickup processing).

2. **Spawn-time HP initialization**
   - On enemy spawn, set max HP using:
     - `EnemyDefinitionSO.baseHp`
     - difficulty HP multiplier from Difficulty Scaling/Enemy Spawner context
     - elite multiplier if flagged elite (3x HP)
     - special-case overrides for temporary clones/splits where applicable
   - Set `currentHp = maxHp` at activation from pool.
   - All values are data-driven through ScriptableObjects and run context; no hardcoded combat constants in runtime code.

3. **Damage reception contract**
   - Spell systems call `TakeDamage(amount, sourceElement)` on valid enemy hit.
   - Damage amount entering Enemy Health is authoritative output from Damage Calculator.
   - If enemy is currently in `Dying`, incoming damage is ignored.
   - If enemy is inside post-hit invincibility window, incoming damage is ignored.
   - Otherwise apply damage, clamp, and evaluate death.

4. **Element affinity behavior**
   - Element weakness/resistance multipliers come from Damage Calculator affinity rules.
   - Weakness hit = `1.5x`, resistance hit = `0.6x`, neutral = `1.0x`.
   - Enemy Health does not compute affinity tables itself; it consumes resolved damage values.

5. **Hit invincibility window (anti-multi-hit stacking)**
   - After accepted damage, enemy enters short invincibility gate.
   - Default duration: `0.1s` (configurable via `EnemyDefinitionSO`/combat config).
   - Purpose: prevent same-frame or near-frame duplicate damage from stacked colliders/sources.

6. **Death handling flow**
   - On lethal damage (`currentHp <= 0`):
     1. Clamp HP to `0`.
     2. Transition to `Dying`.
     3. Publish `EnemyDeathEvent` on Event Bus with payload:
        - `enemyId`, `enemyType`, `position`, `isElite`, `runTime`
     4. Spawn XP gem at death position via Object Pool (`XPGemPool`).
     5. Execute optional element-drop hook (MVP-2): evaluate configured drop chance and, if success, request pooled element drop.
     6. Run special enemy death hooks (Çor split, boss flow).
     7. Perform cleanup and return enemy to `EnemyPool`.
   - Enemy objects are never destroyed in combat flow.

7. **Contact damage to player**
   - Trigger condition: enemy overlap/touch with player hit volume.
   - Damage formula input:
     - `base_contact_damage` from `EnemyDefinitionSO`
     - `difficulty_damage_multiplier` from Difficulty Scaling
   - Apply per enemy-player cooldown gate before dealing damage.
   - Default contact interval: `0.5s` (configurable).
   - On valid contact tick:
     - apply resolved damage to player health authority
     - publish `PlayerDamagedEvent` with combat context

8. **Visual feedback integration**
   - On each accepted enemy hit, request floating damage number from VFX/UI feedback layer.
   - Number origin: enemy world position (with configurable offset).
   - Enemy Health provides value and context; VFX system renders and animates.

9. **Elite, boss, and special enemy compatibility**
   - **Elite**: same lifecycle, death event with `isElite=true`, HP multiplier already applied at spawn.
   - **Boss**:
     - uses same HP/damage authority and death pipeline,
     - drives separate boss health bar channel,
     - publishes `BossDefeatedEvent` on boss death,
     - phase transitions are post-MVP extension points.
   - **Çor**: on death, spawn two halves at clamped offset positions, each with half max HP baseline.
   - **Göl Aynası**: clone instances can be spawned periodically by behavior logic; clones have low HP, grant 0 XP, and expire/cleanup through same pooled lifecycle.

### States and Transitions

Per-enemy health lifecycle state machine:

| State | Entry Condition | Exit Condition | Behavior |
|---|---|---|---|
| Alive | Spawn/activation completed | Hit accepted or lethal transition | Can take damage and deal contact damage; behavior sub-states active |
| TakingDamage | Valid hit applied while still alive | Hit reaction window completes | Brief reaction state; may return to Alive |
| Dying | Lethal hit confirmed (`currentHp <= 0`) | Death cleanup tasks complete | Ignores new damage, publishes death events, schedules pool return |
| DeathCleanup | Dying post-event stage | Cleanup complete | Spawns drops/hooks, clears transient refs/timers |
| ReturnedToPool | Cleanup complete | Next pool activation | Object inactive, reusable by Object Pool |

Alive behavior sub-states (owned by Enemy Behaviors, listed for interface clarity):
- Idle
- Moving
- Attacking

Valid transitions:
- `Alive -> TakingDamage -> Alive`
- `Alive -> TakingDamage -> Dying`
- `Alive -> Dying` (single lethal burst)
- `Dying -> DeathCleanup -> ReturnedToPool`

Invalid transitions:
- `Dying -> Alive` (not allowed)
- `ReturnedToPool -> Dying` (not allowed without reactivation)

### Interactions with Other Systems

| System | Interface In | Interface Out | Responsibility Split |
|---|---|---|---|
| Enemy Spawner | Spawn context (`enemyType`, difficulty multipliers, elite flag) | Initialized enemy HP state | Spawner chooses spawn; Enemy Health sets HP authority |
| Damage Calculator | Resolved damage amount and elemental result | None (consumes output) | Calculator owns math; Enemy Health owns HP mutation |
| Difficulty Scaling | HP and damage multipliers by elapsed time | None (consumes output) | Scaling owns multiplier generation |
| Event Bus | Publish death/player-damage/boss-defeat events | Typed gameplay events | Event Bus routes to subscribers |
| Object Pool | `TryTake` for drops, `Return` for enemy cleanup | Pooled instances | Pool owns memory lifecycle |
| XP & Leveling | Subscribes to `EnemyDeathEvent`, collects XP gems | XP progression events | XP system owns XP accounting and pickup logic |
| Element Drop System | Optional hook on enemy death | `ElementDroppedEvent` (if drop occurs) | Drop logic owns chance tables and pickup behavior |
| Boss UI | Reads boss HP state stream | Boss health bar updates | UI displays only, does not mutate gameplay state |
| VFX/UI Feedback | Receives damage-number requests | visual feedback only | Presentation layer renders hit feedback |

## Formulas

### HP Remaining

```text
currentHp = maxHp - Σ(incoming_damage)
currentHp = clamp(currentHp, 0, maxHp)
```

| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| `maxHp` | float | >0 | Enemy spawn initialization | Scaled HP set at spawn |
| `incoming_damage` | float | >=0 | Damage Calculator output | Per accepted hit damage value |
| `currentHp` | float | 0..maxHp | Enemy Health runtime state | Current HP after processing |

Expected output range: `0` to `maxHp`.

### Contact Damage to Player

```text
contact_dmg = base_contact × difficulty_multiplier
final_contact_dmg = max(1, floor(contact_dmg))
```

| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| `base_contact` | float | 5-15 baseline | EnemyDefinitionSO | Enemy base contact damage |
| `difficulty_multiplier` | float | >=1.0 | Difficulty Scaling | Time-based damage scaling |
| `final_contact_dmg` | int | >=1 | Calculated | Applied damage to player |

Expected output range (MVP baseline 0-30 min): approximately `5` to `51`.

### Contact Cooldown Gate

```text
if (time_since_last_contact < contact_interval)
    skip_damage
else
    apply_damage_and_update_last_contact_time
```

| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| `time_since_last_contact` | float sec | >=0 | Runtime timer map | Time since this enemy last damaged this player |
| `contact_interval` | float sec | 0.1-2.0 | Config (`EnemyDefinitionSO`/combat config) | Damage gate interval (default 0.5s) |

### Çor Split HP

```text
half_hp = original_max_hp × 0.5
```

| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| `original_max_hp` | float | >0 | Dying Çor instance | HP before split resolution |
| `half_hp` | float | >0 | Calculated | Initial HP for each spawned half |

### Hit Invincibility Gate

```text
if (time_since_last_hit < invincibility_duration)
    skip_damage
else
    accept_damage_and_refresh_hit_time
```

| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| `time_since_last_hit` | float sec | >=0 | Enemy runtime state | Time elapsed since last accepted hit |
| `invincibility_duration` | float sec | 0.0-0.3 | EnemyDefinitionSO/combat config | Short anti-stack gate (default 0.1s) |

## Edge Cases

| Scenario | Expected Behavior | Rationale |
|---|---|---|
| Overkill damage drives HP below zero | Clamp `currentHp` to 0; execute death flow once | Prevents duplicate death processing and negative HP artifacts |
| Enemy receives hit while already in `Dying` | Ignore hit and do not emit extra events | Death pipeline must be idempotent |
| Çor split near arena boundary | Clamp split spawn positions to navigable arena bounds | Prevents halves spawning out of play space |
| Multiple damage sources in same frame | Process sequentially by arrival order; first lethal hit wins | Deterministic and reproducible kill authority |
| Elite enemy death | Same death flow; event carries `isElite=true` | Keeps integrations simple while preserving elite context |
| Boss death | Standard death flow + publish `BossDefeatedEvent` | Aligns boss completion with shared combat lifecycle |
| Enemy dies before incoming projectile arrives | Projectile resolves no valid target (pass-through or no-op) | Avoids post-mortem damage on pooled/invalid target |
| Contact overlap persists every frame | Cooldown gate enforces interval tick; no per-frame damage spam | Prevents unfair unavoidable damage burst |
| Göl Aynası clone death | Clone follows normal cleanup but yields 0 XP reward | Supports decoy gameplay without economy exploit |
| Enemy returned to pool with stale timers | `OnPoolReturn`/`OnPoolTake` reset hit/contact timers and flags | Prevents cross-spawn state leakage |

## Dependencies

| System | Direction | Nature of Dependency |
|---|---|---|
| Enemy Spawner | Enemy Health & Damage depends on | Receives spawn context, enemy type, elite flag, and scaling context |
| EnemyDefinitionSO | Enemy Health & Damage depends on | Source of base HP, base contact damage, invincibility/contact interval knobs |
| Damage Calculator | Enemy Health & Damage depends on | Provides authoritative element-adjusted damage values |
| Difficulty Scaling | Enemy Health & Damage depends on | Provides HP and contact damage multipliers by elapsed time |
| Event Bus | Enemy Health & Damage depends on | Publishes `EnemyDeathEvent`, `PlayerDamagedEvent`, and boss completion signals |
| Object Pool | Enemy Health & Damage depends on | Reuses enemy objects and spawns pooled XP/drop instances |
| XP & Leveling | Depends on Enemy Health & Damage | Consumes death outcomes and XP gem lifecycle |
| Element Drop System (MVP-2 hook) | Depends on Enemy Health & Damage | Uses death hook for optional drop generation |
| Enemy Behaviors | Lateral integration | Owns Alive sub-state behavior; health system owns HP/death authority |
| VFX/UI Feedback | Depends on Enemy Health & Damage | Displays floating damage numbers and death feedback |
| Boss System / Boss UI | Depends on Enemy Health & Damage | Uses HP authority and death events for boss encounter flow |

## Tuning Knobs

| Parameter | Current Value | Safe Range | Effect of Increase | Effect of Decrease |
|---|---:|---:|---|---|
| Base HP per enemy type | EnemyDefinitionSO-specific | per enemy archetype | Increases TTK and survivability | Enemies die faster, lower pressure |
| Difficulty HP slope (via scaling system) | 0.12/min default | 0.06-0.20 | Late-run enemies become much tankier | Flatter late-run endurance curve |
| Base contact damage per enemy type | 5-15 baseline | 1-30 | Raises collision punishment | Softer contact threat |
| Difficulty damage slope (via scaling system) | 0.08/min default | 0.04-0.15 | Contact damage spikes faster over time | More forgiving late run |
| Contact interval | 0.5s | 0.1-2.0s | Fewer damage ticks under overlap | More frequent damage ticks |
| Hit invincibility duration | 0.1s | 0.0-0.3s | Suppresses burst multi-hit stacking | Allows higher burst throughput |
| Elite HP multiplier | 3.0x | 1.5-4.0x | Makes elites tougher and more distinct | Elites feel closer to normal enemies |
| Çor split offset distance | 0.75 world units | 0.25-2.0 | Wider separation, easier target split | Tight clustering after split |
| Çor split HP ratio | 0.5x | 0.3-0.7 | Harder post-split cleanup | Easier post-split cleanup |
| Göl Aynası clone HP ratio | 0.5x baseline profile | 0.2-0.8 | Clones survive longer, more clutter | Clones become disposable decoys |
| Göl Aynası clone XP reward | 0 | 0-1 | (If raised) adds farm value risk | Keeps clones as tactical noise only |
| Element drop chance hook | MVP-2 disabled by default | 0%-100% | More reward density on kills | Rarer strategic element drops |

## Acceptance Criteria

- [ ] Enemy HP initializes at spawn from `EnemyDefinitionSO` base HP multiplied by provided difficulty context.
- [ ] Elite enemies receive HP amplification at spawn and publish death events with `isElite=true`.
- [ ] `TakeDamage(amount, sourceElement)` applies accepted damage, enforces hit invincibility gate, and ignores hits in `Dying` state.
- [ ] Lethal hit clamps HP to `0` and triggers exactly one death pipeline execution per enemy life.
- [ ] `EnemyDeathEvent` payload includes `enemyId`, `enemyType`, `position`, `isElite`, and `runTime`.
- [ ] Enemy death spawns XP gem via Object Pool and returns enemy to pool (never destroys runtime combat entity).
- [ ] Contact damage is gated per enemy-player pair by configurable interval (default 0.5s).
- [ ] Contact damage uses `base_contact × difficulty_multiplier` and emits `PlayerDamagedEvent` on valid ticks.
- [ ] Çor death spawns exactly two valid halves with half-HP initialization and clamped spawn positions.
- [ ] Göl Aynası clone instances can exist with low HP, zero XP reward, and temporary cleanup through pool lifecycle.
- [ ] Boss death follows shared health/death flow and additionally emits `BossDefeatedEvent`.
- [ ] Floating damage number requests are emitted on accepted hits and rendered by VFX/UI layer.
- [ ] Overkill, simultaneous damage, dying-state damage, and stale projectile-target scenarios follow documented edge-case rules.
- [ ] No hardcoded gameplay values are introduced; all tuning parameters are sourced from ScriptableObjects/config.
