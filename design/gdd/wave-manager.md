# Wave Manager System

> **Status**: Approved
> **Author**: zbrave + game-designer
> **Last Updated**: 2026-04-04
> **Implements Pillar**: Death Teaches, Never Punishes; Build Diversity Over Build Power

## Overview

Wave Manager is the run-time pacing authority that controls continuous enemy spawning, timed enemy roster unlocks, and fixed boss encounter timing for Kök Tengri runs. Unlike discrete arena-clear waves, this system follows a survivor-like flow where pressure rises continuously over elapsed run time. It subscribes to `RunStartEvent` to begin scheduling, computes current spawn parameters from data-driven configuration and difficulty multipliers, and supplies those parameters to Enemy Spawner every update tick. Wave Manager also publishes `WaveCompletedEvent` when an internal wave segment has fully resolved (segment timer elapsed and all enemies assigned to that segment are dead), enabling downstream systems to react without interrupting continuous spawn pacing.

## Player Fantasy

The player should feel that the underworld is relentlessly adapting and pushing back as their shaman grows stronger. Early minutes feel readable and survivable, mid-run feels tense and crowded, and late-run feels mythic and chaotic without becoming random noise. Boss arrivals every five minutes should feel like ritual milestones that punctuate the run, while normal enemy flow resumes as an ongoing siege after each encounter. The system supports the fantasy of enduring a living invasion rather than clearing isolated rooms.

## Detailed Design

### Detailed Rules

1. **Run lifecycle ownership**
   - Wave Manager remains `Inactive` until `RunStartEvent` is received from Run Manager.
   - On `RunStartEvent`, it initializes runtime timers, unlock schedule state, and spawn progression state from `WaveManagerConfigSO`.
   - On `RunEndEvent`, it transitions to `Inactive`, stops all spawn scheduling, and clears runtime counters.

2. **Continuous spawn model (not discrete clear waves)**
   - Enemies spawn continuously during `Spawning` state using a time-scaled spawn rate.
   - There is no global "kill all to progress" gate.
   - Internal `waveIndex` is a pacing segment identifier used for telemetry, pacing checkpoints, and `WaveCompletedEvent` emission only.

3. **Spawn rate scaling authority**
   - Base spawn rate starts from configuration (`BaseSpawnRateEnemiesPerSecond`).
   - Default growth is +10% per elapsed minute via configurable parameter (`SpawnRateIncreasePerMinute`).
   - Effective spawn rate is multiplied by run difficulty scalar from Difficulty Scaling system.

4. **Enemy roster unlock schedule**
   - Wave Manager unlocks enemy families by elapsed time and exposes available families to Enemy Spawner.
   - Default unlock schedule (data-driven, shown here as current tuning):

| Elapsed Time | Newly Available Enemies | Notes |
|---|---|---|
| 00:00 | Kara Kurtlar | Initial pressure baseline |
| 02:00 | Yek Uşağı | Adds tankier melee pressure |
| 05:00 | Albastılar | Adds ranged threat profile |
| 08:00 | Çor'lar | Adds split-on-death complexity |
| 10:00 | Elite variants enabled | 5% elite chance per eligible spawn |
| 12:00 | Demirci Cinleri | Adds armored check to builds |
| 18:00 | Göl Aynası | Adds cloning noise and target confusion |

5. **Boss schedule and separation from regular flow**
   - Boss timing is fixed at five-minute cadence and checked by Wave Manager timeline.
   - Boss encounters are special events and are not sampled from normal spawn mix.
   - Current boss timeline:

| Elapsed Time | Boss | Scope |
|---|---|---|
| 05:00 | Tepegöz | MVP |
| 10:00 | Yer Tanrısı | MVP |
| 15:00 | Erlik Han'ın Elçisi | MVP |
| 20:00 | Boz Ejderha | Post-MVP |
| 25:00 | Erlik Han | Post-MVP |

6. **Enemy count cap and spawn throttling**
   - Normal enemy spawning is paused when `ActiveEnemyCount >= MaxActiveEnemies`.
   - `MaxActiveEnemies` defaults to 300 from performance budget and is configurable.
   - When count drops below cap, spawning resumes using current time-scaled parameters.
   - Boss spawns are exempt from the normal cap gate.

7. **Elite injection rule**
   - Before minute 10: elite chance is 0%.
   - At minute 10 and later: each regular spawn roll has configurable elite probability (default 5%).
   - Elite eligibility is limited to enemy families flagged `CanHaveEliteVariant` in data.

8. **Wave completion event semantics in a continuous system**
   - Wave Manager partitions timeline into configured pacing segments (`WaveSegmentDurationSeconds`).
   - A segment is "completed" when:
     1) segment duration has elapsed, and
     2) all enemies tagged with that segment index are dead.
   - On completion, Wave Manager publishes `WaveCompletedEvent(waveIndex, remainingEnemies, runTime)` via Event Bus.
   - Multiple completions in close succession are queued and published sequentially.

9. **Pause and time freeze behavior**
   - When run pause is active, Wave Manager freezes elapsed run timer, spawn accumulators, and boss timers.
   - No normal spawn, unlock progression, or boss trigger check advances while paused.

10. **Hero Mode escalation (post-Erlik Han)**
    - When Hero Mode flag is activated after Erlik Han defeat, Wave Manager applies:
      - spawn rate multiplier +50% (x1.5),
      - enemy HP multiplier +50% (x1.5) passed to downstream systems.
    - Hero Mode remains active until run end.

### States and Transitions

| State | Entry Condition | Exit Condition | Behavior |
|-------|----------------|----------------|----------|
| `Inactive` | App boot or `RunEndEvent` cleanup complete | `RunStartEvent` received | No scheduling, timers at rest, outputs disabled |
| `Spawning` | Enter from `Inactive` on run start OR return from `BossEncounter` after boss resolved | Boss schedule condition met and boss trigger accepted; or run end | Computes spawn rate, unlock set, elite chance, cap gate; provides spawn plan to Enemy Spawner |
| `BossEncounter` | Boss spawn trigger fired by timeline and boss not already active | Boss defeated/despawned or run end | Suppresses regular boss checks, keeps regular spawn policy as configured for encounter (default: reduced background spawns), guarantees active boss authority |
| `Spawning` (Resumed) | Boss encounter ends normally | Next boss trigger or run end | Continues continuous progression at current elapsed-time parameters (no timeline reset) |
| `Inactive` (Run End) | Run termination from any active state | Next run start | Stops spawning, clears queues, despawns managed boss via cleanup hooks |

Valid transition path for MVP and post-MVP:
- `Inactive -> Spawning -> BossEncounter -> Spawning -> BossEncounter ... -> Inactive`

### Interactions with Other Systems

| System | Interface In | Interface Out | Responsibility Split |
|---|---|---|---|
| Run Manager | `RunStartEvent`, `RunEndEvent`, pause state | `WaveManagerStarted`, `WaveManagerStopped` (internal diagnostics optional) | Run Manager owns run lifecycle; Wave Manager owns pacing state during run |
| Event Bus | Subscribe to `RunStartEvent` and `RunEndEvent` | Publish `WaveCompletedEvent`; publish boss trigger requests/events via boss interface | Event Bus handles delivery/ordering; Wave Manager owns event semantics |
| Enemy Spawner | Receives spawn plan request each tick | Returns spawn success/failure and active count snapshots | Wave Manager computes what/when; Spawner decides where and instantiates from pools |
| Difficulty Scaling | Provides current difficulty multiplier | Receives optional pacing telemetry | Difficulty system owns scalar; Wave Manager applies it to spawn density/rate |
| Boss System (post-MVP integration point active in schedule now) | `TrySpawnBoss(bossId, timeStamp)` and boss state query | `BossSpawned`, `BossDefeated`, `BossDespawned` | Wave Manager owns timing; Boss System owns boss lifecycle and behavior |
| Enemy Registry / Data | Enemy family unlock definitions, elite eligibility flags | N/A | Data defines capabilities; Wave Manager only reads |
| Performance Monitor | Cap budget (`MaxActiveEnemies`) and emergency throttle policy | Real-time active enemy counts | Performance layer provides limits; Wave Manager enforces spawn gate |

## Formulas

### Spawn Rate Scaling

```text
spawn_rate = base_spawn_rate × (1 + spawn_rate_increase_per_minute × elapsed_minutes) × difficulty_multiplier × hero_mode_spawn_multiplier
```

| Variable | Type | Range | Source | Description |
|----------|------|-------|--------|-------------|
| `base_spawn_rate` | float | 0.2-20.0 enemies/sec | `WaveManagerConfigSO` | Baseline regular spawn throughput at run start |
| `spawn_rate_increase_per_minute` | float | 0.00-0.50 | `WaveManagerConfigSO` | Linear growth factor per elapsed minute (default 0.10) |
| `elapsed_minutes` | float | 0.0-60.0+ | Run Manager clock | Elapsed run time in minutes (frozen while paused) |
| `difficulty_multiplier` | float | 0.5-3.0 | Difficulty Scaling | Dynamic global run difficulty scalar |
| `hero_mode_spawn_multiplier` | float | 1.0 or 1.5 | Hero Mode flag/config | Extra spawn pressure after Erlik Han defeat |

**Expected output range**: 0.2 to 60.0 enemies/sec (bounded by cap gating and safety clamps).
**Edge case**: If `elapsed_minutes` is negative due to bad clock input, clamp to 0 before evaluation.

### Wave Density Target

```text
target_density = base_density × (1 + density_growth_per_minute × elapsed_minutes) × difficulty_multiplier × hero_mode_density_multiplier
```

| Variable | Type | Range | Source | Description |
|----------|------|-------|--------|-------------|
| `base_density` | float | 5-200 enemies/area-unit | `WaveManagerConfigSO` | Initial target crowd density |
| `density_growth_per_minute` | float | 0.00-0.30 | `WaveManagerConfigSO` | Target density ramp rate |
| `elapsed_minutes` | float | 0.0-60.0+ | Run Manager clock | Run progression driver |
| `difficulty_multiplier` | float | 0.5-3.0 | Difficulty Scaling | Shared challenge scalar |
| `hero_mode_density_multiplier` | float | 1.0 or 1.5 | Hero Mode config | Late-game crowd escalation |

**Expected output range**: 5 to 600 equivalent density units.
**Edge case**: If target density exceeds performance envelope, Wave Manager keeps target internally but still obeys max active cap gate.

### Enemy Cap Gate

```text
if active_enemies >= max_active_enemies:
    spawn_allowed = false
else:
    spawn_allowed = true
```

| Variable | Type | Range | Source | Description |
|----------|------|-------|--------|-------------|
| `active_enemies` | int | 0-500+ | Enemy Spawner runtime count | Current number of active non-despawned enemies |
| `max_active_enemies` | int | 100-500 | `PerformanceConfigSO` / `WaveManagerConfigSO` | Spawn cap budget (default 300) |
| `spawn_allowed` | bool | true/false | Calculated | Gate controlling regular spawn attempts |

**Expected output range**: Boolean gate only.
**Edge case**: Boss spawn path ignores `spawn_allowed` and proceeds if boss timing condition is met.

### Boss Spawn Condition

```text
boss_spawn_due = (elapsed_minutes mod boss_interval_minutes == 0) AND (boss_active == false) AND (boss_for_this_slot_not_spawned == true)
```

| Variable | Type | Range | Source | Description |
|----------|------|-------|--------|-------------|
| `elapsed_minutes` | int/float | 0-60+ | Run Manager clock | Current run time used for fixed boss schedule checks |
| `boss_interval_minutes` | int | 1-10 | `WaveManagerConfigSO` | Boss cadence (default 5) |
| `boss_active` | bool | true/false | Boss System state | Whether a boss encounter is currently active |
| `boss_for_this_slot_not_spawned` | bool | true/false | Wave Manager boss schedule tracker | Prevents duplicate spawn at same schedule slot |

**Expected output range**: True at scheduled time boundaries only.
**Edge case**: If run is paused at exact boundary, trigger check is deferred until unpause and next eligible tick.

### Elite Spawn Probability

```text
elite_spawn_chance = 0                       when elapsed_minutes < elite_start_minute
elite_spawn_chance = elite_probability       when elapsed_minutes >= elite_start_minute

spawn_variant = Elite if random_0_1 < elite_spawn_chance else Normal
```

| Variable | Type | Range | Source | Description |
|----------|------|-------|--------|-------------|
| `elapsed_minutes` | float | 0.0-60.0+ | Run Manager clock | Time-gated elite enable condition |
| `elite_start_minute` | float | 0-60 | `WaveManagerConfigSO` | Minute elites become eligible (default 10) |
| `elite_probability` | float | 0.00-1.00 | `WaveManagerConfigSO` | Per-spawn elite probability after unlock (default 0.05) |
| `random_0_1` | float | [0,1) | RNG service | Uniform random sample |

**Expected output range**: 0% pre-threshold, configurable post-threshold (default 5%).
**Edge case**: Elite roll only occurs for enemy families that define elite variants.

## Edge Cases

| Scenario | Expected Behavior | Rationale |
|----------|------------------|-----------|
| Run paused | Spawning pauses and all Wave Manager timers freeze until unpause | Prevents hidden progression while player is paused |
| Active enemy count reaches cap (300 default) | Regular spawns are queued/withheld; spawning resumes when count drops below cap | Protects mobile performance and frame stability |
| Boss due while cap is reached | Boss still spawns (boss path is cap-exempt) | Boss cadence is a core run milestone and must be reliable |
| Multiple wave segments complete close together | `WaveCompletedEvent` publications are processed sequentially in deterministic order | Prevents race conditions in listeners and analytics |
| Run ends during active boss | Boss is despawned via cleanup and Wave Manager transitions to `Inactive` | Ensures clean teardown and no ghost entities |
| Hero Mode activated post-Erlik Han | Apply +50% spawn rate and +50% enemy HP modifiers immediately | Delivers intended late-run escalation profile |
| All enemy families unlocked | Spawner samples full eligible mix using configured weights | Keeps late game varied without unlock dead-zones |
| Difficulty multiplier changes mid-run | New multiplier is applied on next spawn planning tick | Supports dynamic difficulty adjustments cleanly |
| Scheduled boss failed to instantiate on first attempt | Retry according to configured retry window and log warning | Preserves fixed milestone intent while handling transient failures |

## Dependencies

| System | Direction | Nature of Dependency |
|--------|-----------|---------------------|
| Run Manager | Wave Manager depends on Run Manager | Provides run lifecycle events and authoritative elapsed time |
| Event Bus | Wave Manager depends on Event Bus | Subscribes to run events, publishes `WaveCompletedEvent` |
| Enemy Spawner | Enemy Spawner depends on Wave Manager | Consumes spawn parameters (types, rates, elite chance, gates) |
| Difficulty Scaling | Wave Manager depends on Difficulty Scaling | Reads difficulty multiplier for rate/density formulas |
| Boss System | Boss System depends on Wave Manager timing | Receives boss spawn timing triggers from fixed schedule |
| Enemy Data/Registry | Wave Manager depends on enemy data | Reads unlock times, spawn weights, elite eligibility |
| Performance Budget Config | Wave Manager depends on perf limits | Reads `MaxActiveEnemies` cap and throttle policy |

## Tuning Knobs

| Parameter | Current Value | Safe Range | Effect of Increase | Effect of Decrease |
|-----------|--------------|------------|-------------------|-------------------|
| `BaseSpawnRateEnemiesPerSecond` | 1.00 | 0.20-5.00 | Faster early pressure and XP flow | Slower onboarding and lower danger |
| `SpawnRateIncreasePerMinute` | 0.10 | 0.05-0.20 | Stronger time-based escalation | Flatter difficulty curve |
| `BossIntervalMinutes` | 5 | 3-10 | More frequent milestone fights | Fewer boss checkpoints |
| `MaxActiveEnemies` | 300 | 200-400 | Denser battlefield, higher CPU/GPU risk | Better performance, less crowd fantasy |
| `EliteStartMinute` | 10 | 6-15 | Earlier high-risk targets | Later elite pressure onset |
| `EliteSpawnProbability` | 0.05 | 0.01-0.15 | More elite rewards and danger spikes | Smoother, less volatile pacing |
| `WaveSegmentDurationSeconds` | 60 | 30-180 | Fewer completion events, coarser pacing telemetry | More frequent completion signals |
| `HeroModeSpawnMultiplier` | 1.5 | 1.2-2.0 | Harder post-final-boss survival | Gentler hero mode continuation |
| `HeroModeEnemyHpMultiplier` | 1.5 | 1.2-2.0 | Longer kill times, stronger attrition | Faster cleanup in hero mode |
| `BossSpawnRetryWindowSeconds` | 3 | 1-10 | More resilience to transient spawn failure | Faster fail-fast behavior |

## Visual/Audio Requirements

| Event | Visual Feedback | Audio Feedback | Priority |
|-------|----------------|---------------|----------|
| Entering new spawn intensity band | Subtle HUD pulse on danger meter | Low-intensity percussion rise | Medium |
| Enemy family unlock (e.g., Albastılar at 5:00) | Brief icon toast with enemy silhouette | Short stinger cue | Medium |
| Boss spawn trigger | Screen-edge vignette + boss name card | High-priority boss stinger | High |
| Boss encounter resolved | Name card fade + field normalization effect | Resolution stinger | High |
| Spawn cap reached | Small warning indicator near performance icon | Soft warning pulse (rate-limited) | Low |
| Hero Mode activation | Color grade shift + "Hero Mode" banner | Escalation stinger | High |
| Wave segment completed | Minimal checkpoint flash near timer | Soft completion chime | Low |

## UI Requirements

| Information | Display Location | Update Frequency | Condition |
|-------------|-----------------|-----------------|-----------|
| Current run time | Top HUD timer | Every frame | During active run |
| Current danger/spawn intensity | HUD danger meter | 4-10 times/sec | During `Spawning` state |
| Upcoming boss timer | Top-center mini-timer | Every second | When next boss is scheduled and not active |
| Active enemy cap status | Debug/perf HUD (release optional minimal icon) | On change | When near or at cap |
| Newly unlocked enemy families | Temporary right-side toast | On unlock events | At configured unlock timestamps |
| Boss active state | Boss health bar + encounter frame | Every frame | During `BossEncounter` |
| Hero Mode state | Banner + persistent small icon | On state change, then persistent | After Hero Mode activation |

## Acceptance Criteria

- [ ] Wave Manager subscribes to `RunStartEvent` and initializes continuous spawn scheduling from data configuration.
- [ ] Wave Manager transitions through `Inactive -> Spawning -> BossEncounter -> Spawning -> Inactive` with valid conditions only.
- [ ] Continuous spawning works without global "clear wave to continue" gating.
- [ ] Spawn rate follows `spawn_rate = base_rate × (1 + 0.10 × elapsed_minutes)` semantics with configurable growth and difficulty multiplier application.
- [ ] Enemy unlock schedule introduces Kara Kurtlar (0), Yek Uşağı (2), Albastılar (5), Çor'lar (8), elite enable (10), Demirci Cinleri (12), and Göl Aynası (18).
- [ ] Bosses are triggered on fixed five-minute schedule (5, 10, 15; 20 and 25 post-MVP) and are handled as special encounters outside regular spawn mix.
- [ ] Regular spawning pauses when `active_enemies >= max_active_enemies` (default 300), then resumes when below cap.
- [ ] Boss spawn path remains functional even when regular enemy count is at cap.
- [ ] After minute 10, elite variant chance is applied per eligible spawn using configurable default 5% probability.
- [ ] Wave Manager publishes `WaveCompletedEvent` when configured segment completion conditions are met, and close-together completions are delivered sequentially.
- [ ] Run pause freezes timers and spawn progression; unpause resumes correctly without time drift.
- [ ] Run ending during boss encounter triggers cleanup/despawn and leaves no residual active boss state.
- [ ] Hero Mode applies +50% spawn-rate and +50% enemy-HP modifiers after Erlik Han defeat.
- [ ] Wave Manager provides Enemy Spawner with current allowed enemy families, spawn rates, elite chance, and boss gating context each planning tick.
- [ ] All balance and scheduling values are data-driven via ScriptableObjects/config assets (no hardcoded gameplay constants in implementation).
- [ ] Implementation does not rely on UnityEvents, coroutines for gameplay-critical timing, or `FindObjectOfType` in hot paths.
- [ ] Performance target remains compatible with 60 FPS mobile budget while enforcing max active enemy cap.

## Open Questions

| Question | Owner | Deadline | Resolution |
|----------|-------|----------|-----------|
| Should regular spawning be fully paused or only reduced during `BossEncounter` for MVP? | game-designer | Before Wave Manager implementation start | Pending |
| What exact `WaveSegmentDurationSeconds` gives best telemetry value without event spam? | analytics-engineer + game-designer | Before first balance pass | Pending |
| Should enemy unlock toasts be skippable/minimal in accessibility mode? | ux-designer | Before HUD polish milestone | Pending |
| For post-MVP bosses at 20 and 25 minutes, should earlier schedules dynamically rebalance if run length is shortened? | producer + game-designer | Before post-MVP roadmap lock | Pending |
| Should Hero Mode begin only after Erlik Han defeat animation completes or immediately on defeat event publish? | gameplay-programmer + game-designer | Before implementation test plan finalization | Pending |
