# Object Pool System

> **Status**: Approved
> **Author**: zbrave + engine-programmer
> **Last Updated**: 2026-04-04
> **Implements Pillar**: Death Teaches, Never Punishes; Build Diversity Over Build Power

## Overview

Object Pool is a Foundation-layer runtime memory system that reuses GameObject instances instead of creating and destroying them during combat. In Kök Tengri, this system must support 300+ on-screen enemies at 60 FPS on mobile by pooling high-churn entities (enemies, spell projectiles, XP gems, VFX particles, element drops), eliminating gameplay-time GC allocations, and keeping spawn/despawn costs deterministic. The system exists so moment-to-moment combat remains smooth even when wave density, spell effects, and drop volume spike at the same time.

## Player Fantasy

The player should feel uninterrupted combat flow: no hitching when large waves spawn, no stutter when projectile-heavy builds trigger, and no pause when many drops appear after elite or boss kills. The technical goal behind this fantasy is invisible consistency; the player experiences the game as responsive, stable, and fair regardless of build composition or enemy count.

## Detailed Rules

### Detailed Rules

#### 1) Generic Pool Contract

- A generic runtime pool class is required: `ObjectPool<T>` where `T : Component, IPooledObject`.
- `ObjectPool<T>` owns:
  - `Queue<T> available`
  - `HashSet<T> inUse` (or equivalent active tracking)
  - reference prefab
  - pool configuration for that type
- Required operations:
  - `Initialize(PoolRuntimeConfig config, Transform parent)`
  - `T TryTake()`
  - `void Return(T instance)`
  - `void Warm(int count)`
  - `void TrimTo(int targetAvailable)`
  - `void DisposePool()`

#### 2) IPooledObject Interface

Every pooled prefab component implements `IPooledObject`:

```csharp
public interface IPooledObject
{
    void OnPoolCreate();          // called once after instantiate
    void OnPoolTake();            // called when activated from pool
    void OnPoolReturn();          // called before deactivation
    void OnPoolDestroy();         // called before permanent destroy
    bool IsActive { get; }        // runtime safety state
}
```

Behavior requirements:
- `OnPoolTake()` resets runtime state (HP, velocity, timers, particle playback cursor, damage flags).
- `OnPoolReturn()` cancels coroutines/tweens, unsubscribes transient events, clears target references.
- No pooled object may allocate memory in `OnPoolTake()` or `OnPoolReturn()`.

#### 3) Pool Types (Required)

The system must define and register these concrete pool domains:

1. `EnemyPool`
2. `ProjectilePool`
3. `XPGemPool`
4. `VFXPool`
5. `ElementDropPool`

Each pool type maps to one or more prefabs and one `PoolType` enum entry used by config and telemetry.

#### 4) ScriptableObject Configuration

Configuration is data-driven through `PoolConfigSO`.

```csharp
public enum PoolType { Enemy, Projectile, XPGem, VFX, ElementDrop }

[Serializable]
public struct PoolTypeConfig
{
    public PoolType poolType;
    public int initialSize;
    public int maxSize;
    public bool canExpand;
    public OverflowPolicy overflowPolicy; // ExpandOrNull, ReturnNull
    public int warmOnSceneLoad;
    public int warmOnRunStart;
    public bool enableAutoShrink;
    public int minRetained;
    public float shrinkCheckIntervalSec;
    public float lowUsageWindowSec;
    public float lowUsageThreshold; // 0.0-1.0 utilization
}
```

`PoolConfigSO` contains `List<PoolTypeConfig>` and is the single source of truth for pool sizing/tuning. No hardcoded pool sizes in gameplay code.

#### 5) Initialization and Warming

- **Scene load warming**: During scene boot, `PoolManager` warms each pool by `warmOnSceneLoad` to prevent first-combat spikes.
- **Run start pre-warming**: When Run Manager enters `RunStarting`, system performs additional `warmOnRunStart` fills for combat-critical pools (`EnemyPool`, `ProjectilePool`, `VFXPool`, `XPGemPool`, `ElementDropPool`).
- Warm operations happen before player control starts. If warming exceeds frame budget, execute via staged batches across loading frames, never during active combat frames.

#### 6) Take/Return Performance Contract

- Target: `TryTake()` average < **0.01 ms** on target mobile hardware.
- Target: `Return()` average < **0.01 ms** on target mobile hardware.
- Runtime gameplay target: **zero GC allocations** from pooling operations after run start completes.
- `TryTake()` must be O(1) under normal conditions (queue pop + activate + reset).

#### 7) Overflow Behavior

When `TryTake()` is called and no available instances exist:

1. If `canExpand == true` and current total < `maxSize`, allocate one new instance, register, and return it.
2. If pool is at max or policy disallows expansion:
   - `overflowPolicy == ExpandOrNull` and expansion impossible -> return `null`
   - `overflowPolicy == ReturnNull` -> return `null` immediately

Caller responsibilities on `null`:
- Enemy spawner: skip spawn request and record overflow metric.
- Projectile system: skip projectile spawn for that frame (never crash).
- VFX system: degrade gracefully (optional fallback lightweight effect or no-op).
- XP/Element drops: cap spawned drops and merge value if needed (handled by drop system rules).

#### 8) Shrink on Low Usage

Auto-shrink is allowed only outside peak combat windows.

- If `enableAutoShrink == true`, each pool checks utilization every `shrinkCheckIntervalSec`.
- If average utilization over `lowUsageWindowSec` is below `lowUsageThreshold`, trim available instances.
- Never shrink below `minRetained`.
- Shrink destroys available (inactive) instances only; active instances are never force-destroyed.
- Preferred shrink timings: run summary screen, menu state, or low-action intervals.

#### 9) Cleanup Between Runs

- On run end, all active pooled objects are force-returned through safe lifecycle (`OnPoolReturn`) before next run begins.
- On scene unload or app quit, `DisposePool()` calls `OnPoolDestroy()` then destroys all instances.
- Cleanup must clear active tracking collections to prevent stale references across runs.

### States and Transitions

| State | Entry Condition | Exit Condition | Behavior |
|---|---|---|---|
| Uninitialized | Pool manager created, not configured | `Initialize` called with valid config | No take/return allowed |
| Warming | Scene load or run start warm invoked | Warm count reached or batch complete | Instantiates inactive instances and registers them |
| Ready | Warm complete | Take request, shrink cycle, or cleanup start | Normal runtime pooling |
| Expanding | `TryTake` finds empty available and expansion allowed | Instance created and returned, or max reached | Creates one (or batch) additional instances |
| Exhausted | `TryTake` requested while empty and cannot expand | Any instance returned to available | Returns null according to overflow policy |
| Shrinking | Low-utilization check passes and auto-shrink enabled | Target available size reached | Destroys inactive excess instances |
| Cleaning | Run end / scene unload / app shutdown | All objects returned and/or destroyed | Resets pool runtime state for next lifecycle |

Transition constraints:
- `Uninitialized -> Ready` is invalid; warming/initial fill is mandatory.
- `Shrinking` may only remove inactive instances.
- `Cleaning` has priority over `Shrinking`.

### Interactions with Other Systems

| System | Interface In | Interface Out | Responsibility Split |
|---|---|---|---|
| Enemy Spawner | `TryTake(PoolType.Enemy)` | `Return(enemy)` on death/despawn | Spawner requests entities; pool manages lifecycle/memory |
| Spell Effects / Projectile Logic | `TryTake(PoolType.Projectile)` | `Return(projectile)` on hit/timeout | Spell systems own behavior; pool owns instance reuse |
| XP & Leveling | `TryTake(PoolType.XPGem)` | `Return(gem)` on pickup/cleanup | XP system sets gem value; pool handles object reuse |
| VFX System | `TryTake(PoolType.VFX)` | `Return(vfx)` on completion callback | VFX owns playback timing; pool reclaims instances |
| Element Drop System | `TryTake(PoolType.ElementDrop)` | `Return(drop)` on pickup/timeout | Drop logic owns gameplay value; pool owns memory lifecycle |
| Run Manager | `WarmOnRunStart()`, `CleanupForNextRun()` | Pool status metrics events | Run Manager controls phase timing; pool executes prep/cleanup |
| Event Bus | Subscribes to run lifecycle and despawn events | Publishes overflow/warm/shrink telemetry events | Event Bus coordinates decoupled orchestration |

## Formulas

### 1) Utilization Ratio

```text
utilization = active_count / total_count
```

| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| `active_count` | int | 0..total_count | pool runtime | Number of checked-out instances |
| `total_count` | int | 1..maxSize | pool runtime | Active + available instances |

Expected output range: 0.0 to 1.0.

### 2) Warm Count Calculation

```text
warm_target = clamp(initialSize + warmOnSceneLoad + warmOnRunStart, minRetained, maxSize)
```

| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| `initialSize` | int | 0..maxSize | PoolConfigSO | Base startup allocation |
| `warmOnSceneLoad` | int | 0..maxSize | PoolConfigSO | Additional preload during scene boot |
| `warmOnRunStart` | int | 0..maxSize | PoolConfigSO | Additional preload right before gameplay |
| `minRetained` | int | 0..maxSize | PoolConfigSO | Minimum persistent pool size |
| `maxSize` | int | 1..N | PoolConfigSO | Hard cap per pool |

### 3) Auto-Shrink Target

```text
if avg_utilization < lowUsageThreshold
  target_total = max(minRetained, ceil(avg_peak_utilization_window * safety_factor))
else
  target_total = current_total
```

Default `safety_factor = 1.25` to avoid immediate re-expand thrashing.

| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| `avg_utilization` | float | 0.0..1.0 | sampled runtime | Mean utilization over low usage window |
| `lowUsageThreshold` | float | 0.05..0.60 | PoolConfigSO | Threshold below which shrink may run |
| `avg_peak_utilization_window` | float | 0.0..1.0 | sampled runtime | Peak utilization trend in same window |
| `minRetained` | int | 0..maxSize | PoolConfigSO | Lower bound for retained instances |

## Edge Cases

| Scenario | Expected Behavior | Rationale |
|---|---|---|
| 1) Pool exhausted during heavy wave | Apply overflow policy; return `null` if expansion unavailable; caller degrades gracefully | Prevent frame spikes/crashes from emergency allocations |
| 2) Double return of same instance | Ignore second return and log warning in development builds | Prevent queue corruption and duplicate references |
| 3) Returning object to wrong pool type | Reject return, log error, and keep object active for fallback cleanup pass | Preserves pool integrity |
| 4) Scene unload while objects still active | Cleanup phase force-returns active objects, then destroys all instances | Prevent leaks and cross-scene stale objects |
| 5) Pooled object destroyed externally | Pool validates null entries and heals tracking before next operation | Handles accidental external destroy safely |
| 6) Pre-warm interrupted (app pause/background) | Resume or restart warm batch before enabling gameplay | Ensure no first-combat instantiate spikes |
| 7) Shrink runs right before spike | Safety factor + minRetained + timed checks reduce thrashing | Stabilizes performance under bursty combat |
| 8) VFX with long lifetime never returns | TTL safeguard auto-returns after max lifetime timeout | Prevents pool starvation from orphaned effects |
| 9) Run restart spam from player | `CleanupForNextRun()` is idempotent; multiple calls safe | Avoid duplicate cleanup bugs |

## Dependencies

| System | Direction | Nature |
|---|---|---|
| Event Bus | Object Pool depends on | Receives run lifecycle events and emits telemetry/overflow events |
| Run Manager | Object Pool depends on | Triggers pre-warm on run start and cleanup on run end |
| Enemy Spawner | Depends on Object Pool | Requests/recycles enemy instances |
| Spell Effects / Projectile Systems | Depends on Object Pool | Requests/recycles projectile and effect instances |
| XP & Leveling | Depends on Object Pool | Requests/recycles XP gem instances |
| Element Drop System | Depends on Object Pool | Requests/recycles element drop instances |
| VFX System | Depends on Object Pool | Requests/recycles particle/VFX instances |
| PoolConfigSO Data | Object Pool depends on | Supplies per-pool sizing and behavior settings |

## Tuning Knobs

| Parameter | Current Value | Safe Range | Effect of Increase | Effect of Decrease |
|---|---:|---:|---|---|
| Enemy initialSize | 180 | 80-320 | Fewer early expands, higher memory | Lower memory, higher early expand risk |
| Enemy maxSize | 420 | 250-600 | Supports extreme waves, higher RAM | Caps spikes earlier, more null-overflow events |
| Projectile initialSize | 140 | 60-280 | Better burst stability for projectile builds | More first-minute expansion |
| Projectile maxSize | 360 | 180-500 | Handles high attack-speed builds | Hard cap can suppress projectile density |
| XPGem initialSize | 120 | 40-240 | Smooth mass enemy death drops | May over-allocate in low-density waves |
| XPGem maxSize | 400 | 120-600 | Supports boss/elite pileups | More drop suppression/merge events |
| VFX initialSize | 80 | 30-160 | Fewer missing effects during bursts | Higher risk of effect pop-in/missing VFX |
| VFX maxSize | 220 | 80-360 | More visual headroom | Stronger visual culling under load |
| ElementDrop initialSize | 30 | 10-80 | Stable elite drop rendering | More first-elite expansion |
| ElementDrop maxSize | 90 | 30-180 | Supports prolonged elite chains | Drop nulls appear sooner |
| lowUsageThreshold | 0.20 | 0.05-0.50 | More aggressive shrink, lower memory | Less shrink, steadier readiness |
| shrinkCheckIntervalSec | 10s | 3-30s | Faster adaptation | Slower reaction to usage change |
| lowUsageWindowSec | 45s | 15-120s | Smoother shrink decisions | More responsive but noisier |
| minRetained | per-pool 25% of initial | 10%-60% of initial | Fewer re-expands after lull | Lower memory with re-expand risk |

## Acceptance Criteria

- [ ] `ObjectPool<T>` implemented and used by all required pool domains.
- [ ] `IPooledObject` lifecycle callbacks are invoked in correct order for create/take/return/destroy.
- [ ] `PoolConfigSO` fully defines per-pool settings (initial size, max size, expand behavior, shrink settings, warm settings).
- [ ] `EnemyPool`, `ProjectilePool`, `XPGemPool`, `VFXPool`, and `ElementDropPool` are configured and operational.
- [ ] Scene load warming runs before gameplay and prevents first-spawn instantiate spikes.
- [ ] Run start pre-warming executes on every run before player control.
- [ ] Overflow handling is deterministic (`expand` when allowed, otherwise `null`) and callers handle null safely.
- [ ] Low-usage shrink works without touching active instances and never drops below `minRetained`.
- [ ] Run cleanup returns or destroys all pooled instances; no stale active references between runs.
- [ ] Performance: `TryTake()` and `Return()` average < **0.01 ms** each on target hardware.
- [ ] Performance: pooling operations create **zero GC allocations during active gameplay**.
- [ ] No pool sizes or behavior flags are hardcoded in runtime gameplay systems.
