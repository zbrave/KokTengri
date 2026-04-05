## 2026-04-05 — S1-02 EventBus learnings

- Implemented `KokTengri.Core.EventBus` as a static typed bus with `Dictionary<Type, List<Delegate>>` listener storage plus per-type queued event stores to preserve FIFO nested publish behavior.
- Used reusable per-type dispatch buffers so listener mutation during callbacks does not affect the current publish, while avoiding steady-state allocations after warmup.
- Added `EventChannelSO<T>` and three concrete channel assets (`PlayerDamagedEventChannelSO`, `XPCollectedEventChannelSO`, `RunEndEventChannelSO`) to bridge inspector wiring to the typed bus.
- Wrote the NUnit tests first in `Assets/Tests/Foundation/EventBusTests.cs`, then verified behavior with a temporary .NET harness because this workspace does not currently expose Unity-generated project files or a working `csharp-ls` installation.
