# Enemy Spawner System

> **Status**: Approved
> **Author**: zbrave + gameplay-programmer
> **Last Updated**: 2026-04-04
> **Implements Pillar**: Death Teaches, Never Punishes; Build Diversity Over Build Power

## Overview

Enemy Spawner is the runtime execution system that turns Wave Manager pacing plans into active enemies in the arena. It does not decide macro pacing, enemy unlock timing, or combat behavior logic; instead, it receives spawn instructions (`enemy type`, `count`, `spawn rate`, `elite eligibility`) and performs safe, performant spawns using Object Pool. The system enforces mobile performance constraints with a strict normal-enemy active cap of 300, queues overflow spawn requests when the cap is reached, and resumes queued spawning as enemies die. It also applies Difficulty Scaling multipliers to enemy combat stats at spawn time, resolves valid spawn positions around the player (screen edge or just outside camera view), and guarantees boss spawn execution even during cap pressure.

## Player Fantasy

The player should feel continuously hunted by an organized mythic invasion rather than seeing enemies pop in unfairly. Enemies should emerge from believable perimeter positions around the battlefield, pressure should ramp smoothly with time, and elite/boss arrivals should feel intentional and readable. Even at high intensity, the game must remain smooth and responsive on mobile so deaths feel earned by positioning/build decisions rather than performance stutter or spawn spikes.

## Detailed Design

### Detailed Rules

1. **System scope and authority split**
   - Enemy Spawner executes spawn requests.
   - Wave Manager owns `what to spawn` and `when to spawn`.
   - Difficulty Scaling owns multiplier calculation.
   - Enemy Behaviors owns post-spawn AI behavior.

2. **Input contract from Wave Manager**
   - Spawner consumes a spawn plan containing:
     - `enemyDefinitionId` (maps to `EnemyDefinitionSO`)
     - `requestedCount`
     - `spawnRatePerSecond`
     - `isBossSpawn`
     - `eliteRollEnabled`
     - `segment/wave metadata`
   - Spawner must not modify pacing intent; it only executes or queues when blocked by cap.

   **Enemy spawn catalog reference (from Difficulty Scaling + master spec):**

   | Enemy Type | Base HP | Spawn/Profile Tag (data tag only) | Special Stat Notes |
   |---|---:|---|---|
   | Kara Kurt | 8 | Fast, pack pressure | None |
   | Yek Uşağı | 25 | Slow, group pressure | None |
   | Albastı | 15 | Ranged pressure | None |
   | Çor | 20 | Split-capable unit | Split halves use 10 HP |
   | Demirci Cin | 40 | Armored pressure unit | Immune to physical damage |
   | Göl Aynası | 12 | Clone-capable unit | Clone copies use 6 HP |

   Note: These tags are spawn metadata only; detailed behavior execution belongs to Enemy Behaviors.

3. **Data source of truth**
   - Enemy prefab and base stats are read from `EnemyDefinitionSO`.
   - Spawner runtime settings (distances, variance, cap, queue limits, batching) are read from `EnemySpawnerConfigSO`.
   - No gameplay-time `Instantiate` or `Destroy` is allowed for enemy lifecycle.

4. **Object Pool usage**
   - Spawner requests enemy instances through Object Pool (`TryTake` on enemy pool domain).
   - On enemy death/despawn, enemy instance is returned to pool (`Return`) instead of destroyed.
   - If pool returns `null`, spawner follows overflow policy from pool configuration and logs telemetry.

5. **Normal spawn cap enforcement**
   - `MaxActiveEnemies` default is 300 (configurable).
   - Cap applies to normal and elite enemies.
   - Cap does not apply to boss entities.

6. **Queue behavior at cap**
   - If cap is reached, incoming normal spawn instructions are converted into queued spawn tickets.
   - Queue entries preserve enemy type, elite intent, and source wave metadata.
   - Queue is processed as soon as active count drops below cap.
   - Queue processing is deterministic FIFO by default (configurable only if explicitly approved).

7. **Spawn position policy**
   - Spawn points are generated around player position on a ring band (`minSpawnDistance`..`maxSpawnDistance`) with variance.
   - Preferred positions are at camera edge or slightly outside camera bounds.
   - Spawner validates positions against arena bounds and clamps/adjusts invalid results.
   - Spawner must never place an enemy on top of the player.

8. **Distance constraints**
   - Minimum spawn distance from player is configurable to prevent unfair pop-in collisions.
   - Maximum spawn distance is configurable to prevent wasted off-screen far spawns.
   - Distances are read from `EnemySpawnerConfigSO`, not hardcoded.

9. **Difficulty application on spawn**
   - At spawn time, spawner fetches current multipliers from Difficulty Scaling.
   - Applied HP = base HP × current HP multiplier.
   - Applied damage = base damage × current damage multiplier.
   - Multipliers are applied before enemy activation completes.

10. **Elite spawn rule (minute 10+)**
    - Before 10:00 elapsed run time, elite chance is 0%.
    - At or after 10:00, each eligible normal spawn has 5% elite chance (default, data-driven).
    - Elite modifiers:
      - HP multiplier x3 over non-elite final HP.
      - XP multiplier x3.
      - Gold aura visual marker.
      - Guaranteed element drop flag.
    - If enemy type has no elite variant support, elite flag is skipped for that spawn.

11. **Boss spawn handling**
    - Boss spawn requests are always executed (cap-exempt path).
    - Boss position uses special policy: arena center or configured boss edge spawn point.
    - Boss spawns bypass normal queue and normal cap gate.

12. **Pause handling**
    - During pause, spawner does not consume spawn budget, process queue, or spawn new enemies.
    - Pending queue remains intact.
    - On resume, spawner continues from frozen timers and queue state.

13. **Batch spawn distribution**
    - If multiple spawn tickets are due in the same frame, spawner batches them.
    - Batch positions are distributed angularly around the player to reduce overlap clumps.

14. **Telemetry and debug requirements**
    - Emit key metrics/events through Event Bus-compatible diagnostics:
      - cap reached/cleared
      - queue depth changes
      - pool null-take count
      - elite spawn successes/skips

### States and Transitions

| State | Entry Condition | Exit Condition | Behavior |
|-------|----------------|----------------|----------|
| `Inactive` | Run not started, or run ended | `RunStartEvent` received | No spawn execution, queue reset/idle |
| `Spawning` | Enter from `Inactive` on run start, or from `CapReached` after active count below cap | Active normal enemy count reaches cap, pause requested, or run end | Executes spawn instructions at Wave Manager rate; applies scaling; acquires pooled enemies |
| `CapReached` | `ActiveEnemyCount >= MaxActiveEnemies` during normal spawn path | Active count drops below cap OR run end | Holds incoming normal spawn tickets in queue; boss path still allowed |
| `Spawning` (Resumed) | Leave `CapReached` when count below cap | Cap reached again, pause, or run end | Dequeues and spawns pending tickets first, then live incoming tickets |
| `Inactive` (Run End) | `RunEndEvent` from any active state | Next run start | Stops all spawning, clears queue/runtime counters |

Valid transition path:
- `Inactive -> Spawning -> CapReached -> Spawning -> Inactive`

### Interactions with Other Systems

| System | Interface In | Interface Out | Responsibility Split |
|---|---|---|---|
| Wave Manager | Spawn plan (`type`, `count`, `rate`, boss flag, elite eligibility) | Spawn success/failure and active count snapshots | Wave Manager decides pacing; Spawner executes concrete spawn operations |
| Difficulty Scaling | Request current multipliers using elapsed run time | `hpMultiplier`, `damageMultiplier`, optional `spawnMultiplier` context | Scaling owns formula outputs; Spawner applies outputs to spawned units |
| Object Pool | `TryTake(EnemyPool, enemyPrefabKey)` | `Return(enemyInstance)` on death/despawn | Pool owns memory lifecycle; Spawner owns runtime acquisition and placement |
| Event Bus | Subscribe run lifecycle and pause signals | Publish spawn diagnostics + optional spawn events | Bus owns routing; Spawner owns event semantics |
| Enemy Health | Receive initialized HP value at spawn | Death callback/event to release spawn slot | Health owns combat HP state; Spawner owns spawn slot accounting |
| Drop System | Receives elite/normal enemy death context | Guaranteed element drop on elite kill | Drop logic owns reward spawn; Spawner only tags elite state |
| Arena Boundary/Navigation Service | Validate candidate spawn position | Corrected valid spawn coordinate | Arena system owns playable bounds; Spawner respects constraints |

## Formulas

### Spawn Position on Player-Centered Ring

```text
angle = random(0, 2π)
distance = clamp(spawn_distance + random(-distance_variance, +distance_variance), min_spawn_distance, max_spawn_distance)
candidate_position = player_position + (cos(angle), sin(angle)) × distance
spawn_position = AdjustToValidArenaAndCameraBand(candidate_position)
```

| Variable | Type | Range | Source | Description |
|----------|------|-------|--------|-------------|
| `player_position` | Vector2 | arena bounds | Player runtime transform | Spawn ring center reference |
| `spawn_distance` | float | config-driven | `EnemySpawnerConfigSO` | Baseline spawn radius from player |
| `distance_variance` | float | 0..spawn_distance | `EnemySpawnerConfigSO` | Randomization amount for spawn radius |
| `min_spawn_distance` | float | > 0 | `EnemySpawnerConfigSO` | Prevents spawning too close to player |
| `max_spawn_distance` | float | > min | `EnemySpawnerConfigSO` | Prevents far-off inactive spawns |
| `angle` | float | [0, 2π) | RNG service | Circular distribution angle |

**Expected output range**: Valid in-bounds point near screen edge/outside camera band.
**Edge case**: If adjusted position remains invalid after max retries, fallback to nearest valid perimeter point.

### Applied Enemy HP at Spawn

```text
applied_hp = base_hp × difficulty_hp_multiplier
```

| Variable | Type | Range | Source | Description |
|----------|------|-------|--------|-------------|
| `base_hp` | float | > 0 | `EnemyDefinitionSO` | Enemy base HP prior to scaling |
| `difficulty_hp_multiplier` | float | >= 1.0 | Difficulty Scaling | Time-based HP scaling output |

**Expected output range**: Enemy-specific; increases with elapsed run time.

### Applied Enemy Damage at Spawn

```text
applied_damage = base_damage × difficulty_damage_multiplier
```

| Variable | Type | Range | Source | Description |
|----------|------|-------|--------|-------------|
| `base_damage` | float | > 0 | `EnemyDefinitionSO` | Base contact/attack damage |
| `difficulty_damage_multiplier` | float | >= 1.0 | Difficulty Scaling | Time-based damage scaling output |

**Expected output range**: Enemy-specific; increases with elapsed run time.

### Elite Stat and Reward Modifier

```text
elite_hp = normal_hp × 3
elite_xp = normal_xp × 3
```

| Variable | Type | Range | Source | Description |
|----------|------|-------|--------|-------------|
| `normal_hp` | float | > 0 | Post-scaling enemy setup | HP after normal difficulty scaling |
| `normal_xp` | float | >= 0 | Enemy reward definition | Base XP reward |

**Expected output range**: Exactly 3x over normal values for elite-enabled types.
**Edge case**: If type is not elite-capable, skip elite modifier and spawn as normal.

### Queue Processing Gate

```text
if active_enemy_count < max_active_enemies:
    while queue_not_empty and active_enemy_count < max_active_enemies:
        spawn(dequeue())
```

| Variable | Type | Range | Source | Description |
|----------|------|-------|--------|-------------|
| `active_enemy_count` | int | 0..N | Spawner runtime tracker | Current active normal+elite enemies |
| `max_active_enemies` | int | 100..500 | Perf config / Spawner config | Active cap (default 300) |
| `queue_not_empty` | bool | true/false | Spawn queue state | Indicates pending deferred spawns |

**Expected output range**: Queue drains progressively whenever headroom exists.

## Edge Cases

| Scenario | Expected Behavior | Rationale |
|----------|------------------|-----------|
| Enemy pool exhausted during spawn attempt | Follow pool policy: expand if allowed and under max; otherwise skip that ticket and log warning/telemetry | Prevents hard frame spikes and keeps runtime stable |
| Boss spawn requested while cap is reached | Boss still spawns immediately via cap-exempt path | Boss timing is a major milestone and must be reliable |
| Player is near arena boundary | Candidate positions are adjusted to nearest valid playable perimeter and camera band | Prevents invalid coordinates and unreachable enemies |
| Multiple enemy types due in same frame | Process as a batch and distribute angularly around ring | Reduces overlap bursts and unfair front-loaded hits |
| Spawn requested during pause | Defer processing until resume; do not consume timers or queue tickets | Ensures deterministic pause behavior and fairness |
| Elite roll succeeds for type without elite variant | Skip elite flag and spawn normal variant, record diagnostic | Maintains compatibility with mixed content readiness |

## Dependencies

| System | Direction | Nature of Dependency |
|--------|-----------|---------------------|
| Wave Manager | Enemy Spawner depends on Wave Manager | Receives spawn instructions, rates, and pacing context |
| Difficulty Scaling | Enemy Spawner depends on Difficulty Scaling | Receives HP/damage multipliers at spawn time |
| Object Pool | Enemy Spawner depends on Object Pool | Acquires/releases enemy instances without instantiate/destroy |
| Event Bus | Enemy Spawner depends on Event Bus | Receives lifecycle signals and publishes diagnostics/events |
| Enemy Definition Data (`EnemyDefinitionSO`) | Enemy Spawner depends on data assets | Reads prefab references and base combat stats |
| Enemy Health / Death pipeline | Enemy Spawner depends on Enemy Death signals | Frees active slots and resumes queued spawns |
| Wave Manager | Wave Manager depends on Enemy Spawner (feedback path) | Receives active count and spawn execution outcomes |
| Drop/Reward System | Drop system depends on Enemy Spawner tags | Uses elite tags for guaranteed element drop logic |

## Tuning Knobs

| Parameter | Current Value | Safe Range | Effect of Increase | Effect of Decrease |
|-----------|--------------|------------|-------------------|-------------------|
| `MaxActiveEnemies` | 300 | 200-400 | Denser battlefield, higher CPU pressure | More stable performance, lower crowd intensity |
| `MinSpawnDistanceFromPlayer` | config default | 4-12 units | Safer spawn fairness, less immediate pressure | Risk of close unfair spawns |
| `MaxSpawnDistanceFromPlayer` | config default | 10-30 units | Wider spawn envelope, potential delayed engagement | Tighter action loop, less perimeter variety |
| `SpawnDistanceVariance` | config default | 0-8 units | Less predictable perimeter pressure | More uniform and readable spawn ring |
| `EdgeBiasWeight` | config default | 0.0-1.0 | More edge/outside-camera spawns | More evenly circular spawns |
| `QueueDrainPerFrameLimit` | config default | 1-30 | Faster catch-up after cap drops | Smoother but slower queue recovery |
| `EliteStartMinute` | 10 | 8-14 | Earlier elite risk/reward moments | Later elite pressure onset |
| `EliteChance` | 0.05 | 0.01-0.15 | More elite rewards and danger spikes | Flatter pacing variance |
| `SpawnPositionRetryCount` | config default | 1-16 | Better valid-position guarantee, more CPU | Less CPU, more fallback usage |

## Visual/Audio Requirements

| Event | Visual Feedback | Audio Feedback | Priority |
|-------|----------------|---------------|----------|
| Normal enemy spawn | Subtle spawn-in fade from perimeter (no pop-in flash) | Low-volume spawn rustle by enemy family | Medium |
| Elite enemy spawn | Gold aura + stronger silhouette outline | Distinct elite cue | High |
| Boss spawn | Boss entry card + camera-safe reveal location marker | Boss stinger cue | High |
| Cap reached | Optional small perf warning icon (non-intrusive) | None or soft UI pulse (rate-limited) | Low |
| Queue drain burst | No extra noisy effects; keep readability | None | Low |

## UI Requirements

| Information | Display Location | Update Frequency | Condition |
|-------------|-----------------|-----------------|-----------|
| Active enemy count (debug/perf) | Debug overlay | On change / 4-10 Hz | Dev and profiling modes |
| Cap reached state | Debug overlay + optional small HUD icon | On state change | When active count reaches cap |
| Queue depth | Debug overlay | On change | Dev and profiling modes |
| Elite spawn confirmation (indirect) | Enemy visuals in world | Immediate on spawn | Elite-enabled spawns only |
| Boss spawn confirmation | Boss banner / boss HUD | On boss spawn event | Boss encounter starts |

## Acceptance Criteria

- [ ] Enemy Spawner executes Wave Manager spawn instructions without owning pacing logic.
- [ ] All enemy runtime instances are acquired via Object Pool (`TryTake`) and returned on death/despawn (`Return`).
- [ ] No gameplay-time enemy `Instantiate` or `Destroy` calls are required in normal flow.
- [ ] Normal enemy cap defaults to 300 active enemies and is configurable through data.
- [ ] When cap is reached, normal spawns are queued and resume automatically when active count drops.
- [ ] Boss spawns are cap-exempt and execute even when normal cap is saturated.
- [ ] Spawn positions are generated around player at edge/outside-camera band and validated against arena bounds.
- [ ] Spawner enforces configurable min/max spawn distance from player.
- [ ] Applied enemy HP uses `base_hp × difficulty_hp_multiplier` at spawn.
- [ ] Applied enemy damage uses `base_damage × difficulty_damage_multiplier` at spawn.
- [ ] Elite eligibility starts at minute 10 with default 5% chance per eligible spawn and supports x3 HP/x3 XP.
- [ ] Enemy type configuration is sourced from `EnemyDefinitionSO` (prefab, base stats, behavior config references).
- [ ] System integration avoids UnityEvents, gameplay-critical coroutines, and `FindObjectOfType` in hot paths.
- [ ] Performance behavior remains compatible with 60 FPS mobile target under 300+ active enemy scenarios.
## Open Questions
| Question | Owner | Deadline | Resolution |
|----------|-------|----------|-----------|
| Should cap warning indicator be shown to players in release builds, or remain debug-only to avoid UI noise? | ux-designer + producer | Before HUD polish lock | Pending |
