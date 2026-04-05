# Difficulty Scaling System

> **Status**: Approved
> **Author**: zbrave + systems-designer
> **Last Updated**: 2026-04-04
> **Implements Pillar**: Death Teaches, Never Punishes; Build Diversity Over Build Power

## Overview

Difficulty Scaling is a stateless utility system that converts elapsed run time into deterministic combat pressure multipliers. It does not own gameplay state, does not run as a MonoBehaviour, and does not spawn, move, or damage entities directly. Instead, it exposes pure calculation functions consumed by Wave Manager, Enemy Spawner, and Enemy Health so run intensity rises smoothly and predictably from minute 0 onward. The system exists to keep run pacing readable on mobile while ensuring every build must adapt to increasing threat over time.

## Player Fantasy

The player should feel that the world is waking up against them in escalating mythic waves, not that random spikes are unfairly punishing them. Early minutes should feel survivable and exploratory, mid-run should feel tense and demanding, and late-run should feel like an earned endurance trial. Scaling reinforces the fantasy that Erlik Han's influence intensifies as time passes, while still giving skilled players room to outplay and optimize. The intended emotional curve is confidence -> pressure -> controlled chaos.

## Detailed Design

### Detailed Rules

1. **System role and boundaries**
   - Difficulty Scaling is a pure math/service layer.
   - It must expose deterministic functions that accept time and flags as input and return multipliers as output.
   - It must not read frame state directly and must not mutate other systems.

2. **Primary authority input**
   - Elapsed time comes from Run Manager as authoritative run seconds.
   - Difficulty Scaling converts seconds to minutes for formula evaluation.
   - Negative input time is clamped to `0` before any calculation.

3. **Core multiplier outputs**
   - Enemy HP multiplier (used by Enemy Health during enemy stat initialization).
   - Enemy damage multiplier (used by contact damage or enemy attack calculators).
   - Spawn rate multiplier (used by Enemy Spawner/Wave Manager pacing logic).

4. **Baseline linear time curves (default values from `DifficultyConfigSO`)**
   - HP scale slope default: `+0.12` per elapsed minute.
   - Damage scale slope default: `+0.08` per elapsed minute.
   - Spawn scale slope default: `+0.10` per elapsed minute.
   - Time scaling has no hard cap and continues linearly beyond 30 minutes.

5. **Enemy unlock schedule (time-gated availability)**

| Unlock Time | Enemy Type | Base HP | Notes |
|---|---|---:|---|
| 0 min | Kara Kurt | 8 | Starter enemy pool |
| 2 min | Yek Uşağı | 25 | Heavy early pressure unit |
| 5 min | Albastı | 15 | Ranged harassment begins |
| 8 min | Çor | 20 (halves: 10) | Split behavior handled by enemy system |
| 12 min | Demirci Cin | 40 | Durable mid-run gate |
| 18 min | Göl Aynası | 12 (clones: 6) | Clone rules owned by enemy behavior |

6. **Boss timing reference**
   - Bosses are scheduled every 5 minutes by the Boss system.
   - Difficulty Scaling does not spawn bosses; it only exposes the same elapsed-time reference used by boss scheduling systems.

7. **Elite modifier rule**
   - After minute 10, eligible enemies may spawn as Elite.
   - Elite applies `3.0x` HP and `3.0x` XP reward modifier.
   - Elite multipliers stack multiplicatively with time scaling.

8. **Hero Mode modifier rule**
   - Hero Mode activates after Erlik Han defeat.
   - Hero Mode adds `+50%` to enemy HP and spawn rate via multiplier `1.5x`.
   - Hero Mode does not modify enemy damage unless separately configured in future scope.
   - Hero Mode stacks multiplicatively with normal time scaling.

9. **Configuration source of truth**
   - All coefficients, thresholds, schedules, and toggles are data-driven in `DifficultyConfigSO`.
   - Missing config values must resolve to safe defaults documented in this GDD.

### States and Transitions

This system is stateless by design, but integration can be modeled as deterministic evaluation phases:

| State | Entry Condition | Exit Condition | Behavior |
|---|---|---|---|
| Ready | Run initialized, config loaded | Time query requested | Holds immutable config reference |
| EvaluateBaseScaling | Consumer requests multipliers | Base multipliers computed | Computes HP/damage/spawn from elapsed minutes |
| ApplyContextModifiers | Base scaling exists | Context modifiers resolved | Applies Hero Mode and/or Elite multiplicatively |
| ReturnResult | Final multipliers assembled | Consumer receives output | Returns pure calculation result, no side effects |

Valid flow: `Ready -> EvaluateBaseScaling -> ApplyContextModifiers -> ReturnResult -> Ready`

### Interactions with Other Systems

| System | Interface In | Interface Out | Responsibility Split |
|---|---|---|---|
| Run Manager | `elapsedSeconds`, `isHeroMode` | Time/context values | Run Manager owns authoritative run context |
| Wave Manager | Requests spawn multiplier | `spawnRateMultiplier` | Wave pacing logic applies multiplier to wave templates |
| Enemy Spawner | Requests unlock list + spawn multiplier | Eligible enemy set + scaled spawn pacing | Spawner chooses what to spawn; scaling only provides constraints |
| Enemy Health | Requests HP multiplier | `enemyHpMultiplier` | Enemy Health owns final HP assignment |
| Enemy Damage | Requests damage multiplier | `enemyDamageMultiplier` | Damage systems own contact/attack resolution |
| XP/Reward System | Reads elite flag from spawn result | `eliteXpMultiplier` reference | Scaling defines elite rule, reward system applies XP outcome |
| Event Bus | Optional publish of debug/telemetry events | Time-based scaling snapshots | Event Bus transports data; scaling remains source of truth |

## Formulas

All values below are represented as config-backed defaults in `DifficultyConfigSO`.

### Enemy HP Scaling

```text
enemy_hp = base_hp × hp_multiplier
hp_multiplier = 1 + (hp_slope_per_minute × elapsed_minutes)
default hp_slope_per_minute = 0.12
```

| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| `base_hp` | float | > 0 | EnemyDefinitionSO | Enemy base HP before scaling |
| `elapsed_minutes` | float | >= 0 | Run Manager time | Elapsed run time in minutes (clamped) |
| `hp_slope_per_minute` | float | 0.00-0.50 | DifficultyConfigSO | HP growth rate per minute |
| `hp_multiplier` | float | >= 1.0 | Calculated | Time-based HP multiplier |

### Enemy Damage Scaling

```text
enemy_damage = base_contact_damage × damage_multiplier
damage_multiplier = 1 + (damage_slope_per_minute × elapsed_minutes)
default damage_slope_per_minute = 0.08
```

| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| `base_contact_damage` | float | > 0 | EnemyDefinitionSO | Enemy base contact damage |
| `elapsed_minutes` | float | >= 0 | Run Manager time | Elapsed run time in minutes (clamped) |
| `damage_slope_per_minute` | float | 0.00-0.40 | DifficultyConfigSO | Damage growth rate per minute |
| `damage_multiplier` | float | >= 1.0 | Calculated | Time-based damage multiplier |

### Spawn Rate Scaling

```text
spawn_rate = base_rate × spawn_multiplier
spawn_multiplier = 1 + (spawn_slope_per_minute × elapsed_minutes)
default spawn_slope_per_minute = 0.10
```

| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| `base_rate` | float | > 0 | WaveConfigSO | Baseline spawn rate for current wave |
| `elapsed_minutes` | float | >= 0 | Run Manager time | Elapsed run time in minutes (clamped) |
| `spawn_slope_per_minute` | float | 0.00-0.40 | DifficultyConfigSO | Spawn growth rate per minute |
| `spawn_multiplier` | float | >= 1.0 | Calculated | Time-based spawn multiplier |

### Hero Mode and Elite Stacking

```text
final_hp_multiplier = hp_multiplier × hero_mode_hp_multiplier × elite_hp_multiplier
final_spawn_multiplier = spawn_multiplier × hero_mode_spawn_multiplier
final_xp_multiplier = base_xp_multiplier × elite_xp_multiplier

default hero_mode_hp_multiplier = 1.5
default hero_mode_spawn_multiplier = 1.5
default elite_hp_multiplier = 3.0
default elite_xp_multiplier = 3.0
```

Stacking policy:
- Time scaling × Hero Mode = multiplicative.
- Time scaling × Elite = multiplicative.
- Hero Mode × Elite = multiplicative where both apply.

### Example Multipliers at Key Time Points (Default Config)

| Time (min) | HP Multiplier `1+0.12t` | Damage Multiplier `1+0.08t` | Spawn Multiplier `1+0.10t` |
|---:|---:|---:|---:|
| 5 | 1.60 | 1.40 | 1.50 |
| 10 | 2.20 | 1.80 | 2.00 |
| 15 | 2.80 | 2.20 | 2.50 |
| 20 | 3.40 | 2.60 | 3.00 |
| 25 | 4.00 | 3.00 | 3.50 |
| 30 | 4.60 | 3.40 | 4.00 |

Hero Mode example at 20 min:
- Base HP multiplier = `3.40`
- Hero Mode HP multiplier = `1.5`
- Final HP multiplier = `3.40 × 1.5 = 5.10`

Hero Mode + Elite example at 20 min:
- Final elite HP multiplier = `3.40 × 1.5 × 3.0 = 15.30`
- Final elite XP multiplier = `1.0 × 3.0 = 3.0`

## Edge Cases

| Scenario | Expected Behavior | Rationale |
|---|---|---|
| Time = 0 | All base multipliers return `1.0` | Prevents unintended early spike and preserves onboarding clarity |
| Time < 0 | Clamp elapsed time to `0` before formulas | Defensive correctness for timing jitter or bad input |
| Hero Mode + normal scaling | Multiply (`base × 1.5`), do not add | Keeps scaling mathematically consistent and predictable |
| Elite + time scaling | Multiply (`base × 3.0` for HP) | Preserves elite identity at all run stages |
| Run time > 30 min | Continue linear scaling with no hard cap | Supports endless/survival extensions and post-MVP modes |
| Missing `DifficultyConfigSO` | Use safe defaults from this GDD and emit warning | Prevents runtime failures and protects playability |
| Unknown enemy type in unlock query | Exclude from unlock output until valid config exists | Avoids spawning unconfigured entities |
| Hero Mode active before final boss defeat (invalid state) | Ignore Hero Mode flag unless unlock condition is confirmed by Run Manager | Prevents accidental progression bypass |

## Dependencies

| System | Direction | Nature of Dependency |
|---|---|---|
| Run Manager | Difficulty Scaling depends on | Provides authoritative elapsed run time and Hero Mode status |
| DifficultyConfigSO | Difficulty Scaling depends on | Provides all coefficients, schedules, thresholds, and fallback values |
| Wave Manager | Depends on Difficulty Scaling | Consumes spawn multiplier to tune wave pressure |
| Enemy Spawner | Depends on Difficulty Scaling | Consumes unlock schedule and spawn multiplier |
| Enemy Health | Depends on Difficulty Scaling | Consumes HP multiplier for runtime stat scaling |
| Enemy Damage | Depends on Difficulty Scaling | Consumes damage multiplier for outgoing enemy damage |
| XP/Reward System | Depends on Difficulty Scaling (elite rules) | Applies elite XP multiplier where elite flag is present |
| Event Bus (optional telemetry) | Lateral integration | Broadcasts scaling snapshots or debug diagnostics |

## Tuning Knobs

| Parameter | Current Value | Safe Range | Effect of Increase | Effect of Decrease |
|---|---:|---|---|---|
| HP slope per minute | 0.12 | 0.06-0.20 | Tankier enemies, stronger endurance pressure | Longer TTK, easier sustain |
| Damage slope per minute | 0.08 | 0.04-0.15 | Higher punishment for mistakes | More forgiving survivability |
| Spawn slope per minute | 0.10 | 0.05-0.18 | Denser battlefield, higher APM demand | Lower crowd pressure |
| Elite start minute | 10 | 8-14 | Earlier risk/reward spikes | Later elite complexity |
| Elite HP multiplier | 3.0 | 1.5-4.0 | Elite target priority becomes critical | Elites feel less distinct |
| Elite XP multiplier | 3.0 | 1.5-4.0 | Stronger incentive to hunt elites | Less reward tension |
| Hero Mode HP multiplier | 1.5 | 1.2-2.0 | Harder post-boss endurance | Softer post-boss continuation |
| Hero Mode spawn multiplier | 1.5 | 1.2-2.0 | Higher post-boss chaos and score potential | Slower post-boss pacing |
| Unlock schedule offsets | Defined table | +/-2 min per enemy | Earlier complexity and enemy variety | Slower escalation ramp |

## Acceptance Criteria

- [ ] Difficulty Scaling is implemented as stateless pure functions with no MonoBehaviour lifecycle.
- [ ] System accepts elapsed time input from Run Manager and clamps negative values to zero.
- [ ] HP scaling follows config-driven linear formula equivalent to default `1 + 0.12 × elapsed_minutes`.
- [ ] Damage scaling follows config-driven linear formula equivalent to default `1 + 0.08 × elapsed_minutes`.
- [ ] Spawn scaling follows config-driven linear formula equivalent to default `1 + 0.10 × elapsed_minutes`.
- [ ] Enemy unlock schedule matches configured gates: 0/2/5/8/12/18 minutes for Kara Kurt, Yek Uşağı, Albastı, Çor, Demirci Cin, Göl Aynası.
- [ ] Elite eligibility begins at configured minute (default 10) and applies `3x` HP and `3x` XP multipliers multiplicatively.
- [ ] Hero Mode applies `+50%` HP and spawn rate multiplicatively after Erlik Han defeat condition is met.
- [ ] Time scaling continues beyond 30 minutes without hard cap unless future config introduces one.
- [ ] Missing or partial `DifficultyConfigSO` data resolves to safe defaults without gameplay crash.
- [ ] Wave Manager, Enemy Spawner, and Enemy Health consume returned multipliers without owning duplicate scaling math.
- [ ] No gameplay balance constants are hardcoded in consumer systems; all values are sourced from ScriptableObjects.
