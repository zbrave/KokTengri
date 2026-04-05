# Element Inventory System

> **Status**: Approved
> **Author**: zbrave + game-designer
> **Last Updated**: 2026-04-04
> **Implements Pillar**: Every Element Matters

## Overview

Element Inventory is a fixed 3-slot runtime container that stores raw element picks during a run (`Od`, `Sub`, `Yer`, `Yel`, `Temür`) until they are consumed by Spell Crafting or discarded by the player. It exists to create tactical pressure between immediate recipe payoff and future setup, while keeping the system readable on mobile HUD. The inventory is intentionally small so every slot decision matters and level-up choices remain meaningful.

## Player Fantasy

The player should feel like a deliberate shaman, not a random collector: each element held in the pouch is a planned ingredient for future magic. A full inventory should create productive tension ("Do I hold this for a combo, or discard now?") rather than confusion. Good inventory management should feel like foresight and mastery, directly supporting discovery and build planning.

## Detailed Rules

### Core Data Model

```csharp
public enum ElementType
{
    Od,
    Sub,
    Yer,
    Yel,
    Temur // code identifier; display name in UI/localization is "Temür"
}
```

- Inventory capacity is fixed at `3` slots (index `0..2`).
- Each occupied slot stores exactly one `ElementType`.
- Empty slots are represented internally as `null`/empty marker (implementation detail), not a sixth element.
- Inventory does not store spells, levels, or recipe metadata; it stores only raw element values.

### Required Operations (Runtime Contract)

The inventory must expose these Spell Crafting-compatible APIs:

- `bool TryAdd(ElementType element)`
  - Adds element to the first free slot (lowest index).
  - Returns `true` on success, `false` if no free slot.
  - Fires `OnElementAdded` on success.
  - Fires `OnInventoryFull` when operation results in `3/3` occupancy.

- `bool TryConsumeAt(int slotIndex)`
  - Removes element at `slotIndex` if occupied.
  - Returns `true` on success.
  - Returns `false` if index invalid or slot empty.
  - Fires `OnElementRemoved` on success.
  - This is the inventory-level **Remove** operation used by discard flow and Spell Crafting consumption.

- `IReadOnlyList<ElementType> GetSnapshot()`
  - Returns an ordered snapshot of currently occupied slots for evaluation.
  - Snapshot order must be deterministic by slot index (0 → 2).
  - Callers cannot mutate internal inventory state through this reference.

Additional helper queries required by level-up and UI:

- `bool HasFreeSlot()`
- `bool IsFull()`

### Gameplay Rules and Flow

1. **Add source**: In MVP-1, element additions come from level-up selection resolution.
2. **Consume source**: Spell Crafting consumes inventory elements when recipe resolution succeeds.
3. **Dual-element consume rule**: when crafting/upgrade uses selected element + one inventory element, both are removed from flow:
   - selected element is consumed during the crafting resolution path,
   - matched inventory element is consumed via `TryConsumeAt(matchedIndex)`.
4. **Discard rule**: if inventory is full (`3/3`) and current selections cannot progress crafting, next level-up includes **Discard 1 element** option.
5. **Starting elements**:
   - Standard hero: run starts with one pre-loaded element (`1/3`).
   - Umay exception: run starts with two pre-loaded elements (`Sub + Yer`, `2/3`).
6. **No auto-drop**: elements are never silently removed by overflow; all removals are explicit consume/discard actions.

### Inventory States

| State | Condition | Behavior |
|---|---|---|
| Empty | 0 occupied slots | No immediate recipe contribution; can accept 3 adds |
| Partial(1) | 1 occupied slot | Can accept 2 adds; one potential pair candidate |
| Partial(2) | 2 occupied slots | Can accept 1 add; two potential pair candidates |
| Full | 3 occupied slots | Add blocked; enables discard option in appropriate level-up contexts |

State is derived from occupied slot count and must never desynchronize from slot data.

### Slot Visualization (HUD/Level-Up)

- Show exactly three persistent element slots in HUD.
- Slot order matches internal indices (`0,1,2`) to preserve deterministic UX.
- Empty slot visual: muted/outlined placeholder.
- Occupied slot visual: localized element icon + color mapping:
  - Od = red
  - Sub = blue
  - Yer = brown
  - Yel = white
  - Temür = gray
- Full inventory warning state should be visually obvious (brief flash/highlight) when `OnInventoryFull` fires.

## Formulas

### Occupancy and Capacity

```text
occupied_count = count(slots where slot is occupied)
free_count = 3 - occupied_count
is_full = (occupied_count == 3)
has_free_slot = (free_count > 0)
```

| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| `occupied_count` | int | 0-3 | Inventory runtime state | Number of filled slots |
| `free_count` | int | 0-3 | Derived | Remaining capacity |
| `is_full` | bool | true/false | Derived | Whether add is blocked |
| `has_free_slot` | bool | true/false | Derived | Whether add is allowed |

### First-Free-Slot Add Rule

```text
add_index = min(i in [0..2] where slot[i] is empty)
if add_index exists -> place element, success
else -> fail (inventory already full)
```

### Inventory State Derivation

```text
if occupied_count == 0 -> Empty
if occupied_count == 1 -> Partial(1)
if occupied_count == 2 -> Partial(2)
if occupied_count == 3 -> Full
```

## Edge Cases

| Scenario | Expected Behavior | Rationale |
|---|---|---|
| TryAdd called when full | Return `false`; inventory unchanged; no silent overwrite | Prevents hidden loss of player choices |
| TryConsumeAt with invalid index (<0 or >2) | Return `false`; no event fired | Deterministic, safe API contract |
| TryConsumeAt on empty slot | Return `false`; no event fired | Prevents fake removals and desync |
| Full inventory and no productive level-up match | Next level-up includes Discard option | Avoids deadlock and preserves agency |
| Consume during Spell Crafting | Matched inventory element removed by index; selected element consumed by crafting flow | Matches approved crafting sequence |
| Hero run initialization | Standard heroes preload 1 element; Umay preloads 2 (`Sub + Yer`) | Supports hero identity and faster opening plans |
| Snapshot queried during rapid UI updates | `GetSnapshot()` returns stable slot-order data for current frame/state | Prevents tooltip/runtime mismatch |
| Repeated OnInventoryFull triggers | Event should fire only on transition into full (`2->3`), not every frame while full | Prevents event spam in UI/audio |

## Dependencies

| System | Direction | Nature |
|---|---|---|
| Spell Crafting (`design/gdd/spell-crafting.md`) | Spell Crafting depends on Inventory | Uses `TryAdd(ElementType)`, `TryConsumeAt(int)`, `GetSnapshot()` for add/consume/evaluation |
| Level-Up Selection UI | UI depends on Inventory state | Reads occupancy/full state to render slot visuals and discard option availability |
| Hero System | Inventory depends on Hero setup data | Preloads starting element(s) at run start (1 for normal heroes, 2 for Umay) |
| Event Bus (provisional) | Inventory publishes events | Broadcasts inventory state transitions for HUD/audio/VFX |
| HUD | HUD depends on Inventory snapshot/events | Renders 3 slot icons and reacts to add/remove/full transitions |

Dependency guarantee: Inventory is authoritative for slot occupancy; consumers must not mutate slot state directly.

## Tuning Knobs

| Parameter | Current Value | Safe Range | Effect of Increase | Effect of Decrease |
|---|---:|---|---|---|
| `maxSlots` | 3 | 2-5 (non-MVP experimentation only) | Lower planning pressure, easier recipe setup | Higher tension, more discard pressure |
| `startingElementsDefault` | 1 | 0-2 | Faster first craft for most heroes | Slower early crafting ramp |
| `startingElementsUmay` | 2 (`Sub+Yer`) | 1-2 | Stronger opening identity for Umay | Less unique hero opener |
| `discardAvailabilityRule` | Full + no productive path | strict/full-time | Always-available discard increases control, lowers tension | Stricter discard increases commitment weight |
| `fullWarningFeedbackDurationMs` | 400ms | 150-800ms | More noticeable warning | Less intrusive UI feedback |

## Acceptance Criteria

- [ ] Inventory stores only `ElementType` values (`Od`, `Sub`, `Yer`, `Yel`, `Temür`) across exactly 3 fixed slots.
- [ ] `TryAdd(ElementType)` fills the first free slot and returns `false` when inventory is full.
- [ ] `TryConsumeAt(int)` removes the element at a valid occupied slot and fails safely otherwise.
- [ ] `GetSnapshot()` returns deterministic slot-order data for recipe evaluation.
- [ ] `HasFreeSlot()` and `IsFull()` reflect true occupancy in all states.
- [ ] Events fire correctly: `OnElementAdded`, `OnElementRemoved`, `OnInventoryFull` (transition-based for full).
- [ ] Starting inventory is preloaded correctly for all heroes (1 element), with Umay exception (2 elements: `Sub + Yer`).
- [ ] When Spell Crafting consumes a matched pair, both the selected element and matched inventory element are removed from crafting flow.
- [ ] When inventory is full and no match path is available, next level-up offers discard option.
- [ ] HUD shows exactly 3 slots with clear empty/occupied/full feedback.
