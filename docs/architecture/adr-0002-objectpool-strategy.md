# ADR-0002: ObjectPool Strategy

## Status
Proposed

## Date
2026-04-04

## Context

### Problem Statement
Kök Tengri requires 300+ on-screen enemies, hundreds of projectiles, and numerous XP gems/VFX particles at 60 FPS on mobile. Frequent `Instantiate` and `Destroy` calls cause significant CPU spikes and trigger Garbage Collection (GC), leading to frame stutters ("hitching"). We need a system to reuse GameObjects to maintain a smooth 60 FPS experience.

### Constraints
- **Platform**: Mid-range mobile (Android/iOS).
- **Performance**: Zero GC allocations in hot paths (active gameplay).
- **Memory**: < 512 MB total memory ceiling.
- **Engine**: Unity 2022.3 LTS.

### Requirements
- **Generic Support**: Must work with any `Component` type.
- **Lifecycle Management**: Pooled objects must have clear "Take" and "Return" hooks to reset state.
- **Data-Driven**: Pool sizes and pre-allocation (warming) must be configurable via ScriptableObjects.
- **Deterministic Cost**: `TryTake` and `Return` operations must be O(1) and extremely fast.

## Decision

We will implement a **Generic Object Pool System** with per-type pools, pre-allocation (warming), and automatic expansion.

1.  **Generic `ObjectPool<T>`**: A core class managing a `Queue<T>` of inactive instances and a `HashSet<T>` of active ones.
2.  **`IPooledObject` Interface**: All pooled prefabs must implement this interface to handle lifecycle events (`OnPoolTake`, `OnPoolReturn`).
3.  **`PoolConfigSO`**: A ScriptableObject defining initial sizes, max sizes, and warming behavior for each pool type (Enemy, Projectile, XP Gem, VFX, Element Drop).
4.  **Warming Strategy**: Pools are "warmed" (pre-instantiated) during scene load and run start to prevent spikes during the first combat encounter.
5.  **Overflow Policy**: Configurable behavior when a pool is exhausted (Expand, Return Null, or Recycle Oldest).

### Architecture Diagram

```text
[ PoolManager ] <--- (Request) --- [ Spawner System ]
      |
      | (Check Available)
      v
[ ObjectPool<T> ] <--- (Reuse) --- [ Inactive Queue ]
      |
      | (Instantiate if empty & allowed)
      v
[ New Instance ] ---> [ IPooledObject.OnPoolTake() ] ---> [ Active Gameplay ]
```

### Key Interfaces

```csharp
public interface IPooledObject
{
    void OnPoolCreate();
    void OnPoolTake();
    void OnPoolReturn();
    void OnPoolDestroy();
    bool IsActive { get; }
}

public interface IObjectPool<T> where T : Component, IPooledObject
{
    T TryTake();
    void Return(T instance);
    void Warm(int count);
}
```

## Alternatives Considered

### Alternative 1: Unity's Built-in `UnityEngine.Pool.ObjectPool<T>`
- **Description**: Use the pooling utility introduced in Unity 2021.
- **Pros**: Native, well-tested, supports collection pooling.
- **Cons**: Less integrated with our specific `IPooledObject` lifecycle and `PoolConfigSO` data-driven warming strategy.
- **Rejection Reason**: While viable, a custom wrapper or implementation allows tighter integration with our `EventBus` for telemetry and our specific ScriptableObject configuration workflow.

### Alternative 2: Per-Type Manual Pools
- **Description**: Each system (EnemySpawner, ProjectileManager) implements its own private pool.
- **Pros**: Simple for small projects.
- **Cons**: Massive code duplication, inconsistent behavior, difficult to monitor global memory usage.
- **Rejection Reason**: Violates DRY principles and makes global performance optimization impossible.

### Alternative 3: Dependency Injection (DI) Pools
- **Description**: Use a DI framework like Zenject/VContainer to manage pooled instances.
- **Pros**: Clean architectural separation.
- **Cons**: High complexity, significant learning curve for the team, potential overhead.
- **Rejection Reason**: Overkill for the current project scope; we prefer a lightweight Foundation-layer system.

## Consequences

### Positive
- **Smooth Performance**: Eliminates `Instantiate`/`Destroy` spikes during combat.
- **Zero GC**: Reusing objects prevents heap allocations in the core loop.
- **Configurability**: Designers can tune pool sizes for different device tiers via `PoolConfigSO`.

### Negative
- **Memory Overhead**: Pre-allocating objects consumes RAM upfront.
- **Complexity**: Requires careful state resetting in `OnPoolReturn` to avoid "ghost" data bugs.

### Risks
- **Pool Starvation**: If `maxSize` is too low, spawns will fail.
- **Mitigation**: Implement `ExpandOrNull` policy and publish `PoolOverflowEvent` to the `EventBus` for monitoring.

## Performance Implications
- **CPU**: `TryTake` and `Return` < 0.01ms.
- **Memory**: Pre-allocation consumes memory (e.g., 180 enemies ~ 10-20MB).
- **Load Time**: Increased slightly due to warming (mitigated by batching across frames).
- **Network**: N/A.

## Migration Plan
All high-churn entities (Kara Kurt, Yek Uşağı, projectiles, XP gems) will use the `ObjectPool` from their first implementation in Phase 1.

## Validation Criteria
- **Profiling**: Zero GC allocations from `EnemySpawner` and `SpellEffects` during a 5-minute combat test.
- **Stress Test**: 300+ active enemies maintained at 60 FPS on target hardware.
- **Unit Tests**: Verify `OnPoolTake`/`OnPoolReturn` are called correctly and pool limits are respected.

## Related Decisions
- [ADR-0001: EventBus Pattern](adr-0001-eventbus-pattern.md) (Used for overflow telemetry)
- [ADR-0003: ScriptableObject Data Strategy](adr-0003-scriptableobject-data-strategy.md) (Pool settings stored in SOs)
- [design/gdd/object-pool.md](../design/gdd/object-pool.md)
