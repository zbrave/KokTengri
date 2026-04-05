# Plan: Sprint 1 Foundation Systems (EventBus, ObjectPool, Input)

## Objective
Implement the foundational systems for KokTengri (EventBus, ObjectPool, Input System) following a Verification-Driven Development (TDD) approach, strictly adhering to ADR-0001, ADR-0002, and ADR-0003.

## Scope
- **IN**: Unit tests for all systems, EventBus core & SO channels, ObjectPool core & SO config, New Input System wrapper.
- **OUT**: Gameplay mechanics using these systems (e.g., player movement, enemy spawning) - this is strictly foundational architecture.

## Guardrails & Constraints
- **0-Allocation**: Hot paths (Event publish, Pool TryTake/Return) must generate zero GC allocations.
- **Test-Driven**: Unit tests must be written and fail BEFORE implementing the corresponding feature.
- **Data-Driven**: All configurations must be serializable via ScriptableObjects.

## Tasks

### Wave 1: Test Scaffolding & CI Setup
- [ ] T1: Configure Unity Test Runner and create foundational test folders (`tests/KokTengri.Tests/Foundation`).
- [ ] T2: Create Test skeleton classes for EventBus, ObjectPool, and Input System.

### Wave 2: EventBus Implementation
- [ ] T3: Write failing unit tests for EventBus (Publish, Subscribe, Unsubscribe, dispatch order, and 0-GC allocation checks).
- [ ] T4: Implement `EventBus` generic static class (as per ADR-0001).
- [ ] T5: Implement `EventChannelSO<T>` base and some concrete type samples (e.g., IntEventChannelSO).
- [ ] T6: Verify T3 tests pass.

### Wave 3: ObjectPool Implementation
- [ ] T7: Write failing unit tests for ObjectPool (Prewarm, TryTake, Return, overflow policies).
- [ ] T8: Implement `IPooledObject` interface and generic `ObjectPool<T>` (as per ADR-0002).
- [ ] T9: Implement `PoolConfigSO` data structure (as per ADR-0003).
- [ ] T10: Verify T7 tests pass.

### Wave 4: Input System Implementation
- [ ] T11: Write failing unit tests for Input wrapper using dependency injection (mock inputs).
- [ ] T12: Implement `IInputProvider` interface and wrapper class around Unity New Input System.
- [ ] T13: Verify T11 tests pass.

## Final Verification Wave
- [ ] V1: All unit tests pass in the Unity Test Runner.
- [ ] V2: No GC allocations detected in hot paths during tests.
- [ ] V3: Explicit user approval required to mark work complete.
