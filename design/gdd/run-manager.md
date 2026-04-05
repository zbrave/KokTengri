# Run Manager System

> **Status**: Draft
> **Author**: Sisyphus-Junior
> **Last Updated**: 2026-04-04
> **Implements Pillar**: Death Teaches, Never Punishes; Clarity Under Pressure; Session-Friendly Mobile Runs

## Overview

Run Manager is the authority for the full lifecycle of one playable run in Kök Tengri. It initializes deterministic run context (runId, hero/class selection, RNG seed), creates and owns the runtime Event Bus instance, controls state transitions from run start to finalization, tracks elapsed time, coordinates timing-dependent systems (waves and difficulty scaling), and closes the run by emitting RunEndEvent, calculating rewards, and triggering persistence. The system exists so every run is reproducible, bounded, and cleanly terminated on mobile, while still supporting post-30-minute Hero Mode after Erlik Han is defeated.

## Player Fantasy

The player should feel that each run is a self-contained legend with a clear beginning, escalating pressure, meaningful turning points, and a definitive aftermath. Timing is readable, pauses are safe, and outcomes are fair: death is final but understandable, victory at the timer cap is earned, and Hero Mode feels like an intentional "beyond destiny" challenge for players who defeat Erlik Han. The system should feel invisible but trustworthy, so players focus on combat and build choices instead of worrying about session stability.

## Detailed Design

### Detailed Rules

1. **Single-run authority**
   - Exactly one Run Manager instance controls one active run.
   - Run Manager is the source of truth for run state and elapsed run time.
   - No other gameplay system may advance run lifecycle states.

2. **Lifecycle ownership**
   - Public lifecycle flow: `StartRun -> InProgress -> Paused -> Ending -> Ended`.
   - Internal state machine: `Uninitialized -> Starting -> Active -> Paused -> Ending -> Ended`.
   - `InProgress` maps to internal `Active` (with Hero Mode / AFK flags).

3. **Run bootstrap (`StartRun`)**
   - Inputs: `heroId`, `classId`, optional `seedOverride`.
   - Run Manager generates:
     - `runId` (unique per run)
     - `seed` (override or generated)
     - initial run config snapshot (duration, Hero Mode multipliers, AFK threshold)
   - Run Manager creates Event Bus instance; bus lifetime equals run lifetime.
   - On successful initialization, Run Manager publishes `RunStartEvent` with payload:
     - `runId`, `heroId`, `classId`, `seed`.

4. **Deterministic RNG contract**
   - Run Manager owns the canonical run seed and exposes deterministic RNG streams to dependent systems.
   - Wave scheduling and other run-level random generation must derive from this seed.
   - Seed + configuration snapshot should allow deterministic replay in debug environments.

5. **Time tracking contract**
   - Run elapsed time accumulates in Update using `Time.unscaledDeltaTime`.
   - Accumulation occurs only while state is `Active`.
   - Time is represented in seconds for systems and formatted as `MM:SS` for HUD.
   - Configurable run duration target supports 15-30 minutes (default 30).

6. **Timer cap and Hero Mode gate**
   - If elapsed time reaches configured cap and Hero Mode is not active, run enters Ending with `Victory` result.
   - If Erlik Han is defeated before or at timer cap, Hero Mode may activate and run can continue past 30:00.
   - Hero Mode does not auto-end on time cap; run ends only by death or player voluntary end.

7. **Hero Mode activation rules**
   - Trigger condition: Erlik Han defeat signal confirmed by boss system.
   - On activation:
     - set `heroModeActive = true`
     - notify HUD/announcement pipeline
     - apply Hero Mode modifiers via config:
       - enemy HP multiplier = `1.5x`
       - enemy spawn-rate multiplier = `1.5x`
       - gold multiplier at run-end = `1.5x`

8. **Pause and resume (mobile-safe)**
   - App backgrounding forces pause when state is `Active`.
   - Resume only occurs via explicit foreground resume path.
   - Pause/resume transitions use debounce window to ignore rapid duplicate platform callbacks.
   - While paused:
     - gameplay simulation halted by owning systems
     - run elapsed timer does not advance
     - no lifecycle transitions except resume or terminate-safe flow

9. **AFK detection behavior**
   - Run Manager samples movement/input activity timestamps while `Active`.
   - If inactivity duration >= configured AFK threshold (default 10 seconds), Run Manager flags `afkDetected = true`.
   - Run Manager publishes/forwards AFK state to Input System; Input applies auto-slow movement behavior.
   - AFK does not pause the run and remains valid during boss fights.

10. **Run-ending conditions**
    - Run enters `Ending` when one of the following occurs:
      - player death
      - timer cap reached without Hero Mode (`Victory`)
      - player voluntarily ends run after Hero Mode is available
    - Death during boss fight ends run immediately with `Death` result.

11. **Result classification**
    - `Death`: player died before clean voluntary finish.
    - `Victory`: player survived to configured timer cap without entering Hero Mode continuation.
    - `HeroMode`: run entered Hero Mode and later ended by player choice (or other non-death terminal flow).

12. **End-of-run processing order**
    1) lock state to `Ending`
    2) finalize metrics (`survivedSeconds`, `kills`, `bossesDefeated`, heroMode flag)
    3) compute rewards (gold and related outputs)
    4) publish `RunEndEvent` with required payload:
       - `runId`, `result`, `survivedSeconds`, `kills`, `bossesDefeated`
    5) trigger save/persistence pipeline
    6) cleanup Event Bus lifecycle (`Draining -> Disposing`)
    7) mark state `Ended`

13. **Event Bus cleanup guarantee**
    - On run end, bus cleanup is mandatory:
      - drain queued events
      - unsubscribe runtime listeners
      - clear internal maps/queues
      - dispose bus resources
    - Next run must create a fresh bus instance; no listener carryover is allowed.

14. **Data-driven balance and configuration**
    - All tunable values come from ScriptableObject config assets (no hardcoded gameplay values).
    - Timing, multipliers, debounce windows, and AFK threshold are read from RunManagerConfigSO (and linked configs).

15. **Implementation constraints for engineering handoff**
    - No UnityEvents for run lifecycle signaling; use `EventBus<T>` pattern.
    - No coroutines for gameplay-critical run timing; use Update + timers.
    - No `FindObjectOfType` lookup for dependencies; use injected references.

### States and Transitions

| State | Entry Condition | Exit Condition | Behavior |
|-------|-----------------|----------------|----------|
| Uninitialized | App loaded, run not created | `StartRun` called with valid context | No active Event Bus, no time accumulation |
| Starting | `StartRun` begins initialization | Event Bus created, run context valid, `RunStartEvent` published | Initializes seed, runId, config snapshot, subsystem handshakes |
| Active | Start sequence completed OR resumed from Paused | Pause request, run-ending condition, or app background | Advances elapsed time, provides time to Wave Manager and Difficulty Scaling, AFK checks enabled |
| Paused | Pause invoked from Active (manual/system/app background) | Resume request accepted (debounce passed) or forced end | Time accumulation halted, gameplay progression halted |
| Ending | Any terminal condition fired | End-of-run pipeline complete | Freezes lifecycle transitions, computes rewards, emits `RunEndEvent`, triggers save, starts bus cleanup |
| Ended | End pipeline + bus disposal complete | New run bootstrap only (new instance/context) | Immutable terminal state for this run |

Valid transitions:
- `Uninitialized -> Starting -> Active`
- `Active -> Paused -> Active`
- `Active -> Ending -> Ended`
- `Paused -> Ending -> Ended`

Active-state flags (not standalone states):
- `heroModeActive` (false/true)
- `afkDetected` (false/true)

Sub-flow notes:
- `StartRun` API intent maps to internal `Starting` then `Active`.
- `InProgress` product-facing label corresponds to internal `Active` state.
- Multiple rapid pause/resume calls are ignored inside debounce guard.

### Interactions with Other Systems

| System | Interface In | Interface Out | Responsibility Split |
|--------|--------------|---------------|----------------------|
| Event Bus | `CreateBus()`, `Publish<T>()`, `DisposeBus()` | `RunStartEvent`, `RunEndEvent` | Run Manager owns bus lifetime; bus handles routing and dispatch internals |
| Wave Manager | `SetRunClockProvider(elapsedSeconds)` | Wave progression events/status | Run Manager supplies authoritative elapsed time; Wave Manager owns spawn/wave logic |
| Difficulty Scaling | `SetElapsedTime(elapsedSeconds)` | Scaling outputs for enemies/spawn systems | Run Manager provides time input; Difficulty system computes scaling curves |
| Economy | `CalculateRunRewards(runSummary)` | Gold (and economy payload) | Run Manager triggers reward calculation at run end; Economy owns conversion rules |
| Save System | `PersistRunResult(runSummary, rewards)` | Save confirmation/error | Run Manager decides when to save; Save System owns storage implementation (post-MVP persistence expansion) |
| HUD | `GetRunTime()`, `GetRunState()`, hero mode signal | Time/state display and announcements | Run Manager is authority; HUD is presentation-only |
| Input System | `OnAfkStateChanged(isAfk)` | Movement modulation applied | Run Manager detects AFK; Input System applies auto-slow movement behavior |
| Boss System | Boss defeat signal (Erlik Han) | Hero Mode eligibility trigger | Boss System is authority on boss defeat; Run Manager is authority on mode switch |

Interface payload contracts:
- `RunStartEvent`: `{ runId, heroId, classId, seed }`
- `RunEndEvent`: `{ runId, result, survivedSeconds, kills, bossesDefeated }`

## Formulas

### Base Run Gold Reward

```text
gold_base = (survived_minutes × 10) + (kill_count × 0.5) + (bosses_defeated × 100)
```

| Variable | Type | Range | Source | Description |
|----------|------|-------|--------|-------------|
| `survived_minutes` | float | 0-60+ | Run Manager elapsed time / 60 | Minutes survived in this run |
| `kill_count` | int | 0-5000+ | Combat statistics aggregator | Total enemies killed in run |
| `bosses_defeated` | int | 0-10 | Boss tracker | Total bosses defeated in run |

**Expected output range**: 0 to ~5000+ gold (long Hero Mode runs can exceed this).
**Edge case**: Clamp negative or invalid stat inputs to zero before evaluation.

### Hero Mode Gold Modifier

```text
gold_final = gold_base × hero_mode_multiplier
```

Where:

```text
hero_mode_multiplier = 1.5 if heroModeActiveAtAnyPoint else 1.0
```

| Variable | Type | Range | Source | Description |
|----------|------|-------|--------|-------------|
| `gold_base` | float | >= 0 | Base reward formula | Pre-multiplier run reward |
| `hero_mode_multiplier` | float | 1.0 or 1.5 | RunManagerConfigSO | Gold multiplier when Hero Mode is active |

**Expected output range**: 0 to ~7500+ depending on run length and kill density.
**Edge case**: If Hero Mode is entered briefly and run ends soon after, multiplier still applies.

### Run Time Accumulation (Pause-Safe)

```text
elapsed_seconds_next = elapsed_seconds_current + Time.unscaledDeltaTime
```

Applied only when state is `Active`.

| Variable | Type | Range | Source | Description |
|----------|------|-------|--------|-------------|
| `elapsed_seconds_current` | float | >= 0 | Run Manager runtime state | Current run duration seconds |
| `Time.unscaledDeltaTime` | float | 0-0.1+ | Unity runtime clock | Frame delta independent of time scale |

**Expected output range**: 0 to unbounded (practically limited by termination conditions).
**Edge case**: While paused, accumulation is suspended regardless of frame activity.

### AFK Detection

```text
afkDetected = (current_unscaled_time - last_input_unscaled_time) >= afk_threshold_seconds
```

| Variable | Type | Range | Source | Description |
|----------|------|-------|--------|-------------|
| `current_unscaled_time` | float | >= 0 | Unity unscaled clock | Current frame timestamp |
| `last_input_unscaled_time` | float | >= 0 | Input activity feed | Last movement/input timestamp |
| `afk_threshold_seconds` | float | 5-30 (default 10) | RunManagerConfigSO | Idle duration threshold |

**Expected output range**: Boolean result.
**Edge case**: AFK can trigger during boss fights and should not pause run state.

## Edge Cases

| Scenario | Expected Behavior | Rationale |
|----------|-------------------|-----------|
| App is backgrounded mid-run | Force transition `Active -> Paused`; block timer and progression until resume path | Mobile safety and fairness; avoid silent time loss |
| App crashes or is killed during run | MVP behavior: no mid-run save restore; run is considered lost | Keeps MVP simple and avoids partial-state corruption risk |
| Player dies during boss fight | Immediate transition to `Ending`, result `Death`; boss fight treated as failed | Maintains consistent death authority and clear stakes |
| All currently available bosses are defeated but timer not at cap | Continue normal mode unless Erlik Han defeat condition activates Hero Mode; otherwise run continues to configured cap | Prevents premature termination and preserves pacing structure |
| Erlik Han defeated | Show Hero Mode announcement, set Hero Mode flag, apply +50% enemy HP and spawn-rate modifiers | Creates explicit escalation milestone and clear mode identity |
| Run exceeds 30:00 after Hero Mode activation | Run continues; termination only by death or player voluntary end | Supports advanced challenge loop beyond baseline session cap |
| Rapid pause/resume callback burst | Debounce duplicate transitions; preserve single authoritative state transition | Prevents unstable lifecycle oscillation on mobile OS callbacks |
| AFK during boss fight | AFK flag triggers auto-slow movement through Input System; run remains Active | Aligns with AFK rule without violating combat flow |
| Timer cap reached exactly as death occurs | Death result takes priority if death is confirmed in same frame before end pipeline lock | Preserves intuitive fail-state priority in simultaneous events |
| RunEnd requested twice from multiple sources | First request wins and locks `Ending`; subsequent requests ignored with warning log | Ensures idempotent termination pipeline |

## Dependencies

| System | Direction | Nature of Dependency |
|--------|-----------|----------------------|
| Event Bus | Run Manager depends on Event Bus | Run Manager creates, owns, publishes lifecycle events, and disposes at run end |
| Wave Manager | Wave Manager depends on Run Manager | Uses authoritative elapsed run time for wave schedule progression |
| Difficulty Scaling | Difficulty Scaling depends on Run Manager | Uses elapsed time input for scaling curves |
| Economy | Run Manager depends on Economy | Economy applies run-end reward formulas and multipliers |
| Save System | Run Manager depends on Save System | Persists run result/reward payload after run termination |
| HUD | HUD depends on Run Manager | Displays run timer, state, and Hero Mode notifications |
| Input System | Input System depends on Run Manager AFK signals | Applies AFK auto-slow behavior on state change |
| Boss System | Run Manager depends on Boss System defeat signals | Erlik Han defeat is trigger condition for Hero Mode activation |
| App Lifecycle Adapter | Run Manager depends on platform lifecycle callbacks | Converts background/foreground signals into pause/resume transitions |

## Tuning Knobs

| Parameter | Current Value | Safe Range | Effect of Increase | Effect of Decrease |
|-----------|---------------|------------|--------------------|--------------------|
| `RunDurationSeconds` | 1800 (30 min) | 900-2700 | Longer sessions, higher potential rewards, increased fatigue risk | Shorter sessions, faster loops, lower per-run progression |
| `MinConfiguredRunDurationSeconds` | 900 (15 min) | 600-1200 | Guarantees longer minimum runs | Supports quicker sessions but reduces pacing runway |
| `HeroModeEnemyHpMultiplier` | 1.5 | 1.2-2.0 | Stronger endgame challenge, steeper attrition | Softer Hero Mode difficulty spike |
| `HeroModeSpawnRateMultiplier` | 1.5 | 1.1-2.0 | Denser combat and resource pressure | Slower Hero Mode intensity ramp |
| `HeroModeGoldMultiplier` | 1.5 | 1.1-2.0 | Higher risk/reward incentive for extending runs | Weaker incentive to stay in Hero Mode |
| `AfkThresholdSeconds` | 10 | 5-30 | AFK state triggers later; fewer false positives | AFK state triggers sooner; more aggressive idle response |
| `PauseResumeDebounceMs` | 300 | 100-1000 | More robust against OS callback spam, slower responsiveness | Snappier resumes but higher duplicate-transition risk |
| `TimeBroadcastIntervalSeconds` | 0.1 | 0.05-0.5 | Lower update frequency, less overhead, coarser timing updates | Higher update fidelity, more frequent cross-system updates |
| `VictoryAtTimerCapEnabled` | true | boolean | Enforces strict capped-mode completion when not Hero Mode | If false, requires alternate non-time victory trigger |
| `RunEndSaveRetryCount` | 1 | 0-3 | More resilience to transient save failures | Faster fail-through with simpler flow |

All parameters are owned by ScriptableObject config assets (`RunManagerConfigSO`, `EconomyConfigSO`, `WaveConfigSO`) and are not hardcoded in runtime logic.

## Acceptance Criteria

- [ ] Run Manager supports full lifecycle transitions: `Uninitialized -> Starting -> Active -> Paused -> Active -> Ending -> Ended`.
- [ ] `StartRun` initializes deterministic context and publishes `RunStartEvent` with `{ runId, heroId, classId, seed }`.
- [ ] Run timer accumulates via `Time.unscaledDeltaTime` only while `Active` and displays correctly on HUD.
- [ ] Configurable run duration supports 15-30 minute sessions; default 30 minutes.
- [ ] If timer cap is reached without Hero Mode, run ends with `Victory` and executes end pipeline.
- [ ] Erlik Han defeat triggers Hero Mode announcement and applies +50% enemy HP and spawn-rate modifiers.
- [ ] Hero Mode applies 1.5x gold multiplier at run-end calculation.
- [ ] App background event forces pause; resume path restores `Active` without duplicate transitions (debounced).
- [ ] AFK detection triggers after configured idle threshold (default 10s) and informs Input System for auto-slow movement.
- [ ] AFK during boss fights does not auto-pause and does not block combat progression.
- [ ] Run-ending conditions are handled correctly: player death OR voluntary end after Hero Mode availability.
- [ ] `RunEndEvent` publishes `{ runId, result, survivedSeconds, kills, bossesDefeated }` exactly once per run.
- [ ] End pipeline executes in deterministic order: metrics finalize -> rewards -> event publish -> save trigger -> Event Bus drain/dispose -> `Ended`.
- [ ] Event Bus instance is created at run start and fully cleaned at run end; no listener carryover to next run.
- [ ] Gold formula matches economy spec: `(survived_minutes × 10) + (kill_count × 0.5) + (bosses_defeated × 100)`.
- [ ] System uses `EventBus<T>` pattern (no UnityEvents), Update + timers (no coroutines), and injected references (no `FindObjectOfType`).
- [ ] All tunable values are ScriptableObject-driven; no hardcoded gameplay balance values.
- [ ] Performance: Run Manager update work remains within mobile budget target (<= 0.1 ms average on target device profile).
