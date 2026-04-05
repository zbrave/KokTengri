# ADR-0001: EventBus Pattern

## Status
Proposed

## Date
2026-04-04

## Context

### Problem Statement
Kök Tengri is a complex survivor-like game with 31+ interacting systems (combat, UI, VFX, audio, progression, analytics). Direct references between these systems create tight coupling, making the codebase fragile, difficult to test, and hard to scale. We need a centralized communication backbone that allows systems to exchange signals without knowing about each other.

### Constraints
- **Platform**: Mobile (mid-range Android/iOS).
- **Performance**: Must support 300+ enemies at 60 FPS (16.6ms frame budget).
- **Engine**: Unity 2022.3 LTS (URP 2D).
- **Language**: C#.

### Requirements
- **Type Safety**: Compile-time verification of event payloads.
- **Decoupling**: Systems must not have direct references to each other.
- **Inspector Integration**: Designers must be able to wire events in the Unity Inspector.
- **Deterministic Flow**: Events must be processed in a predictable, sequential order within the same frame.
- **Zero/Low Allocation**: High-frequency events (e.g., XP collection) must not cause GC spikes.

## Decision

We will implement a **Centralized Typed Event Bus** using C# generics and a ScriptableObject-based channel pattern.

1.  **Generic Payloads**: All events will be defined as C# `struct` or `class` types (preferring `struct` for high-frequency events to avoid heap allocations).
2.  **Static/Singleton Bus**: A single `EventBus` instance will manage subscriptions and dispatch.
3.  **ScriptableObject Channels**: Each event type will have a corresponding `EventChannelSO<T>` asset. Systems can reference these assets in the Inspector to subscribe or publish, providing a bridge between code and the Unity Editor.
4.  **Sequential Dispatch**: Events are processed immediately and sequentially. Nested publishes are queued and processed FIFO after the current event dispatch completes to prevent re-entrancy issues.

### Architecture Diagram

```text
[ Publisher ] ----> [ EventChannelSO<T> ] ----> [ EventBus ]
      |                                            |
      | (Publish Payload)                          | (Dispatch to Listeners)
      v                                            v
[ System A ]                                  [ System B ]
[ System C ]                                  [ System D ]
```

### Key Interfaces

```csharp
public interface IEventBus
{
    void Subscribe<T>(Action<T> listener);
    void Unsubscribe<T>(Action<T> listener);
    void Publish<T>(T eventData);
}

// Concrete implementation using generics
public static class EventBus
{
    public static void Subscribe<T>(Action<T> listener) { ... }
    public static void Unsubscribe<T>(Action<T> listener) { ... }
    public static void Publish<T>(T eventData) { ... }
}
```

## Alternatives Considered

### Alternative 1: Unity Events (`UnityEvent<T>`)
- **Description**: Use Unity's built-in event system.
- **Pros**: Native Inspector support, easy for non-programmers.
- **Cons**: Significant performance overhead (reflection-based), no generic support without boilerplate, difficult to manage globally.
- **Rejection Reason**: Performance cost is too high for a survivor-like with hundreds of entities; lacks the strict type safety and global decoupling we require.

### Alternative 2: C# Action Delegates
- **Description**: Use standard C# `event Action<T>` in singleton managers.
- **Pros**: High performance, native C#.
- **Cons**: Hard to manage globally, prone to memory leaks if not unsubscribed, no Inspector visibility.
- **Rejection Reason**: Becomes "spaghetti code" as the number of systems grows; lacks the centralized control and Inspector integration of the SO-channel pattern.

### Alternative 3: MessagePipe Library
- **Description**: Use a high-performance DI-friendly messaging library.
- **Pros**: Extremely fast, feature-rich (async, filtering).
- **Cons**: External dependency, adds complexity to the project setup.
- **Rejection Reason**: We prefer a lightweight, custom solution that integrates directly with Unity's ScriptableObject workflow without external dependencies for the core foundation.

## Consequences

### Positive
- **Modular Architecture**: Systems can be added or removed without affecting others.
- **Testability**: Systems can be tested in isolation by mocking event inputs.
- **Performance**: Generic dispatch is extremely fast (< 0.05ms per event).
- **Designer Friendly**: ScriptableObject channels allow wiring in the Inspector.

### Negative
- **Traceability**: It can be harder to follow the flow of logic across decoupled systems without specialized debugging tools.
- **Boilerplate**: Requires defining a struct/class for every event type.

### Risks
- **Memory Leaks**: Listeners must unsubscribe on destruction (e.g., `OnDestroy`).
- **Mitigation**: Implement a `Reset()` method for run-end cleanup and include leak detection in debug builds.

## Performance Implications
- **CPU**: Minimal overhead (< 0.05ms per event with <= 10 listeners).
- **Memory**: Minimal; uses internal dictionaries and lists. High-frequency events use `struct` to avoid GC.
- **Load Time**: Negligible.
- **Network**: N/A (Single-player MVP).

## Migration Plan
This is a Foundation-layer system. All new gameplay systems (Spell Crafting, Enemy Health, XP) will be built using this pattern from day one. Existing prototypes will be refactored to use the `EventBus` during Phase 1.

## Validation Criteria
- **Unit Tests**: Verify subscribe, unsubscribe, and publish logic (including nested events).
- **Profiling**: Ensure dispatch time stays within the 0.05ms budget on target mobile hardware.
- **Leak Check**: Verify `net_listener_delta == 0` at the end of a run.

## Related Decisions
- [ADR-0002: ObjectPool Strategy](adr-0002-objectpool-strategy.md) (Uses EventBus for telemetry)
- [ADR-0003: ScriptableObject Data Strategy](adr-0003-scriptableobject-data-strategy.md) (Events often pass SO references)
- [design/gdd/event-bus.md](../design/gdd/event-bus.md)
