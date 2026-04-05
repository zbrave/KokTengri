# Event Bus System

> **Status**: Approved
> **Author**: zbrave + game-designer
> **Last Updated**: 2026-04-04
> **Implements Pillar**: Every Element Matters, Build Diversity Over Build Power, Death Teaches, Never Punishes (indirect infrastructure support)

## Overview

The Event Bus is the Foundation-layer communication backbone for Kök Tengri. It provides a centralized, type-safe publish/subscribe channel so gameplay, UI, VFX, audio, progression, and analytics systems can exchange runtime signals without direct dependencies. The bus uses C# event payload structs/classes (not UnityEvents) and a Unity ScriptableObject channel pattern so designers and programmers can wire systems in inspector while keeping compile-time type safety in code. This system exists to keep architecture modular as features scale (31 systems), reduce coupling risk, and guarantee deterministic in-frame event flow for combat-critical gameplay responses.

## Player Fantasy

Players never interact with the Event Bus directly, but they feel its quality through responsiveness and coherence: crafted spells immediately trigger matching VFX/audio/UI updates, boss transitions are synchronized, run-end rewards are reliably processed, and no system appears "late" or out-of-sync. In fantasy terms, the world behaves like a single living ritual system where the shaman's actions propagate instantly and consistently across all feedback layers.

## Detailed Design

### Detailed Rules

1. **Centralized bus ownership**
   - A single runtime bus instance is created at run start by Run Manager.
   - Systems do not create private buses for gameplay events.
   - Bus lifetime = run lifetime (StartRun -> EndRun cleanup).

2. **Type-safe generic contract (`EventBus<T>`)**
   - Publish/subscribe API is generic by event type:
     - `void Subscribe<T>(Action<T> listener)`
     - `void Unsubscribe<T>(Action<T> listener)`
     - `void Publish<T>(T eventData)`
   - `T` must be a C# struct or class payload type.
   - No string-key routing and no UnityEvent-based dispatch.

3. **Unity ScriptableObject channel pattern**
   - Each event type has a ScriptableObject channel asset (conceptually `EventChannelSO<T>`), referenced by interested systems.
   - Channel assets are project-level wiring/config objects; dispatch still uses generic typed payloads.
   - Goal: inspector discoverability + code-level safety.

4. **Required event catalog (MVP scope)**

| Event Type | Primary Publishers | Primary Subscribers | Required Payload (minimum) |
|---|---|---|---|
| `SpellCraftedEvent` | Spell Crafting | Spell Slot Manager, HUD, VFX, Audio, Analytics | `spellId`, `newLevel`, `runTime` |
| `SpellUpgradedEvent` | Spell Crafting | Spell Slot Manager, HUD, VFX, Audio, Analytics | `spellId`, `oldLevel`, `newLevel`, `runTime` |
| `ElementConsumedEvent` | Spell Crafting | Element Inventory UI, Audio, Analytics | `elementType`, `reason`, `runTime` |
| `EnemyDeathEvent` | Enemy Health | XP System, Element Drop System, Economy, Audio, VFX | `enemyId`, `enemyType`, `position`, `isElite`, `runTime` |
| `PlayerDamagedEvent` | Player Health | HUD, Audio, Camera Feedback, Analytics | `damageAmount`, `currentHp`, `sourceId`, `runTime` |
| `LevelUpEvent` | XP & Leveling | Level-Up UI, Audio, VFX, Analytics | `newLevel`, `overflowXp`, `runTime` |
| `RunStartEvent` | Run Manager | Wave Manager, HUD, Analytics, Save | `runId`, `heroId`, `classId`, `seed` |
| `RunEndEvent` | Run Manager | Summary UI, Economy, Save, Analytics | `runId`, `result`, `survivedSeconds`, `kills`, `bossesDefeated` |
| `BossSpawnedEvent` | Boss System | HUD, Audio, VFX, Analytics | `bossId`, `bossType`, `spawnPosition`, `runTime` |
| `BossDefeatedEvent` | Boss System | Economy, Destan Collection, Audio, VFX, Analytics | `bossId`, `bossType`, `firstDefeat`, `runTime` |
| `WaveCompletedEvent` | Wave Manager | Enemy Spawner, HUD, Audio, Analytics | `waveIndex`, `remainingEnemies`, `runTime` |
| `XPCollectedEvent` | XP Pickup | XP System, HUD, Audio, Analytics | `amount`, `collectorId`, `position`, `runTime` |
| `ElementDroppedEvent` | Element Drop System | Pickup Spawner, HUD Marker, Audio, Analytics | `elementType`, `position`, `dropSourceId`, `runTime` |

5. **Spell Crafting compatibility requirement**
   - Spell Crafting must publish all three domain events currently defined by its GDD:
     - `SpellCraftedEvent` (OnSpellCrafted semantic)
     - `SpellUpgradedEvent` (OnSpellUpgraded semantic)
     - `ElementConsumedEvent` (OnElementConsumed semantic)
   - Event order inside a single crafting resolution is fixed:
     1) consume event(s), 2) craft/upgrade event.

6. **Deterministic ordering guarantee**
   - Dispatch is **sequential and deterministic within the same frame**.
   - For a given published event, listeners execute in subscription order snapshot.
   - Nested publish is queued and drained FIFO after current dispatch completes (no re-entrant mutation of listener list).

7. **Run-end cleanup guarantee**
   - On `RunEndEvent`, bus enters `Draining` then `Disposing`.
   - Cleanup steps:
     1) drain queued events,
     2) unsubscribe all runtime listeners,
     3) clear per-type listener lists and queues,
     4) reset debug counters/profiler samples.
   - Prevents ghost listeners and duplicate reactions in next run.

8. **Debug logging support**
   - Bus supports configurable logging levels: `Off`, `ErrorsOnly`, `Important`, `Verbose`.
   - Minimum logs at `Important`:
     - Run start/end lifecycle events,
     - subscribe/unsubscribe imbalance warnings,
     - dispatch over-budget warnings,
     - publish with zero listeners for marked-critical events.

9. **Performance rule**
   - Target dispatch cost: **< 0.05ms per published event with <=10 listeners** on target mobile device class.
   - Event payloads should avoid heap allocations in hot loops where possible (prefer structs for high-frequency events).

### States and Transitions

| State | Entry Condition | Exit Condition | Behavior |
|---|---|---|---|
| `Uninitialized` | App boot before run wiring | `RunStartEvent` initialization completes | No publish/subscribe allowed except bootstrap wiring |
| `Active` | Run started and bus initialized | `RunEndEvent` received | Full publish/subscribe/dispatch enabled |
| `Dispatching` | Any `Publish<T>` begins in Active | Current listener snapshot execution ends | Listener list is immutable snapshot; nested publishes queued |
| `Draining` | Run end initiated or explicit flush requested | Queue empty | FIFO queue drained without accepting new gameplay subscriptions |
| `Disposing` | Draining complete | Cleanup complete | Unsubscribe all, clear maps/queues, reset metrics |
| `Disposed` | Disposal finished | Next run creates new bus instance | Bus rejects publish/subscribe with error log |

Valid transitions:
- `Uninitialized -> Active`
- `Active -> Dispatching -> Active`
- `Active -> Draining -> Disposing -> Disposed`
- `Disposed -> Active` (new run instance only)

### System Interactions

| System | Interface In | Interface Out | Responsibility Split |
|---|---|---|---|
| Run Manager | `InitializeBus(runContext)`, `ShutdownBus()` | `RunStartEvent`, `RunEndEvent` | Run Manager owns lifecycle; Event Bus owns dispatch internals |
| Spell Crafting | `Publish(SpellCraftedEvent / SpellUpgradedEvent / ElementConsumedEvent)` | Typed domain events for subscribers | Crafting decides event semantics; bus routes only |
| Enemy Health & Damage | `Publish(EnemyDeathEvent)`, `Publish(PlayerDamagedEvent)` | Combat events | Combat computes payload; bus preserves ordering |
| XP & Leveling | `Subscribe(EnemyDeathEvent, XPCollectedEvent)`, `Publish(LevelUpEvent)` | Level progression events | XP system handles progression logic; bus decouples source and consumer |
| Boss System | `Publish(BossSpawnedEvent, BossDefeatedEvent)` | Boss lifecycle events | Boss system is authority for boss state, bus handles fanout |
| Wave Manager | `Publish(WaveCompletedEvent)` | Wave completion signal | Wave manager owns wave progression; bus not stateful about waves |
| Element Drop System | `Subscribe(EnemyDeathEvent)`, `Publish(ElementDroppedEvent)` | Drop spawn events | Drop logic remains isolated from enemy implementation |
| HUD / VFX / Audio | `Subscribe<T>` to relevant gameplay events | Presentation updates only | Presentation reacts only; must not mutate gameplay authority state |
| Save / Economy / Analytics | `Subscribe(RunEndEvent, BossDefeatedEvent, etc.)` | Persistent/meta side effects | Meta systems consume final signals without tight coupling to gameplay |

## Formulas

### Dispatch Time Budget

```text
dispatch_time_ms = t_after_dispatch_ms - t_before_dispatch_ms
```

Constraint:

```text
dispatch_time_ms <= 0.05    when listener_count <= 10
```

| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| `t_before_dispatch_ms` | double | >= 0 | profiler timestamp | Time sampled immediately before listener loop |
| `t_after_dispatch_ms` | double | >= 0 | profiler timestamp | Time sampled immediately after listener loop |
| `listener_count` | int | 0-10 (target) | runtime bus stats | Active listeners for specific event type |

### Event Throughput Cost Model

```text
frame_event_cost_ms = Σ(dispatch_time_ms_i) for all events i in frame
```

Budget guidance:

```text
frame_event_cost_ms <= 1.0 ms (soft budget in 16.6ms frame)
```

This keeps event plumbing overhead low enough for 60 FPS with heavy combat scenes.

### Listener Leak Detection

```text
net_listener_delta = total_subscribes - total_unsubscribes
```

Constraint at run teardown:

```text
net_listener_delta == 0   OR   all remaining listeners removed by forced cleanup
```

If non-zero before forced cleanup, emit warning with event type and owner context.

## Edge Cases

| Scenario | Expected Behavior | Rationale |
|---|---|---|
| Listener unsubscribes itself during callback | Current dispatch uses snapshot; unsubscribe affects next publish | Prevents iterator invalidation and non-deterministic skips |
| Listener subscribes new callback during callback | New listener not invoked in current dispatch; active from next event | Guarantees stable, deterministic pass for current event |
| Nested publish from inside listener | Nested event enqueued FIFO and processed after current dispatch completes | Preserves sequential in-frame ordering without recursion hazards |
| Publish with zero listeners | No-op in runtime, optional Important-level debug log for critical event types | Avoids hard failures while keeping diagnostics visibility |
| Exception thrown by one listener | Catch, log error with event type/listener owner, continue remaining listeners | One faulty system must not block all downstream responses |
| Run ends while queue still has events | Enter Draining state, flush queue, then dispose | Prevents lost end-of-run effects and ensures clean transition |
| Domain reload / scene reload in editor | Bus reinitializes to Uninitialized; stale references invalidated | Prevents hidden editor-only ghost subscriptions |
| Duplicate subscription of same listener | Allowed only once by default; second subscribe ignored + warning | Prevents accidental double reactions and inflated timings |
| High-frequency event spam (`XPCollectedEvent`) | Struct payload + pooled internal buffers + optional sampled verbose logs | Keeps GC and log cost predictable in combat peaks |

## Dependencies

| System | Direction | Nature |
|---|---|---|
| Run Manager | Event Bus depends on | Owns lifecycle start/end and teardown timing |
| Unity ScriptableObject asset layer | Event Bus depends on | Inspector wiring of event channels and config |
| Spell Crafting | Depends on Event Bus | Publishes craft/upgrade/consume events consumed by multiple systems |
| Enemy Health & Damage | Depends on Event Bus | Publishes combat state transitions (death/damage) |
| XP & Leveling | Depends on Event Bus | Subscribes to death/XP signals and emits level-up |
| Boss System | Depends on Event Bus | Emits spawn/defeat lifecycle for gameplay + meta listeners |
| Wave Manager | Depends on Event Bus | Emits wave completion for pacing systems |
| Element Drop System | Depends on Event Bus | Reacts to deaths, emits drop notifications |
| HUD / VFX / Audio | Depends on Event Bus | Presentation subscribers only; no hard references to gameplay authorities |
| Economy / Save / Analytics | Depends on Event Bus | Consumes run/boss/progression events for persistent and telemetry workflows |

## Tuning Knobs

| Parameter | Current Value | Safe Range | Effect of Increase | Effect of Decrease |
|---|---|---|---|---|
| `maxListenersPerEventType` | 32 | 10-128 | More flexibility, higher worst-case dispatch cost | Lower memory and tighter performance control |
| `maxQueuedEventsPerFrame` | 256 | 64-1024 | Better burst tolerance, higher memory usage | Lower memory, greater risk of overflow warnings |
| `dispatchWarningThresholdMs` | 0.05 | 0.03-0.20 | Fewer warnings, easier to pass budget | Stricter profiling signal, more warning noise |
| `loggingLevel` | Important | Off-Verbose | Better diagnostics, higher CPU/log I/O overhead | Cleaner logs, reduced observability |
| `criticalNoListenerWarnings` | Enabled | Enabled/Disabled | Earlier detection of integration gaps | Fewer logs but missed wiring mistakes |
| `duplicateSubscriptionPolicy` | WarnAndIgnore | Allow/WarnAndIgnore/Error | More strict integration safety | More permissive but bug-prone behavior |
| `catchListenerExceptions` | Enabled | Enabled/Disabled | Higher resilience, slight try/catch overhead | Lower overhead, higher crash coupling risk |
| `flushOnRunEnd` | Enabled | Enabled/Disabled | Guarantees completion of queued end events | Faster teardown but potential dropped signals |

## Acceptance Criteria

- [ ] `EventBus<T>` generic API is implemented and used for all MVP event dispatch; no UnityEvent usage in gameplay event flow.
- [ ] All required event types are defined and routable: `SpellCraftedEvent`, `SpellUpgradedEvent`, `ElementConsumedEvent`, `EnemyDeathEvent`, `PlayerDamagedEvent`, `LevelUpEvent`, `RunStartEvent`, `RunEndEvent`, `BossSpawnedEvent`, `BossDefeatedEvent`, `WaveCompletedEvent`, `XPCollectedEvent`, `ElementDroppedEvent`.
- [ ] Spell Crafting emits `SpellCraftedEvent`, `SpellUpgradedEvent`, and `ElementConsumedEvent` with deterministic order (consume before craft/upgrade resolution output).
- [ ] Dispatch ordering is sequential and deterministic within frame; nested publishes are FIFO-queued and processed after current dispatch.
- [ ] Run end triggers full cleanup: queue drained, listeners removed, maps cleared, and next run starts without duplicate listener effects.
- [ ] Debug logging levels are configurable and include over-budget dispatch warnings and subscription integrity warnings.
- [ ] Performance profiling demonstrates `< 0.05ms` average dispatch time per event with `<=10` listeners on target device profile.
- [ ] No hardcoded gameplay balance values are introduced by this system; all configurable bus thresholds are data/config-driven via ScriptableObjects.
