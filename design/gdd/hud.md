# HUD System

> **Status**: Draft
> **Author**: ui-programmer
> **Last Updated**: 2026-04-04
> **Implements Pillar**: Clarity Under Pressure

## Overview

The HUD (Heads-Up Display) is the always-visible, in-run information layer for Kök Tengri. It presents player HP, XP progress, level number, element inventory slots, spell slots, timer, optional kill counter, boss HP, and pause access while gameplay continues in real time. The HUD is strictly display-only: it does not own, mutate, or validate gameplay state. All updates arrive through Event Bus subscriptions and read-only snapshots from authoritative systems. This system exists to keep survival decisions readable in high-density combat without blocking the playfield, especially on mobile screens.

## Player Fantasy

The player should feel informed and in control even when the arena becomes chaotic. Health loss must be immediately legible, spell readiness should be readable at a glance, and progression should feel visible through XP and level feedback. The HUD should support the fantasy of a disciplined mythic fighter: "I always know my condition, my tools, and my next power spike." It should feel grounded in pixel-art style, responsive to danger, and never visually noisy enough to hide threats.

## Detailed Rules

### Detailed Rules

1. **Display-only authority boundary**
   - HUD never modifies gameplay state.
   - HUD never computes gameplay outcomes (damage, XP gains, cooldown logic, crafting logic).
   - HUD only renders values from Event Bus payloads and read-only snapshots.

2. **Event-driven updates only**
   - HUD subscribes to Event Bus events for all runtime changes.
   - No polling of gameplay systems every frame except permitted visual interpolation on already-received values.
   - No UnityEvents in HUD runtime update flow.

3. **Mobile-first layout policy**
   - Primary target is landscape mobile play.
   - Portrait orientation is supported as fallback through responsive anchors and scale rules.
   - Critical controls and survival info must stay inside safe areas.

4. **Required HUD elements (MVP-1)**
   - Player HP Bar (top-left or top-center depending on profile)
   - XP Bar (top or bottom edge based on profile)
   - Level Number (adjacent to XP bar)
   - Element Inventory (3 slots, bottom-left)
   - Spell Slots (6 slots, bottom-center)
   - Timer (top-right or top-center)
   - Pause Button (top-right, always accessible)

5. **Optional HUD element**
   - Kill Counter may be enabled by config profile; default is enabled for playtest builds and optional for production visibility tuning.

6. **Boss encounter behavior**
   - On boss spawn, boss HP bar appears at top-center.
   - Player HP bar remains visible; boss bar does not replace player survival info.
   - On boss defeat, boss bar hides and victory flash feedback plays.

7. **Health feedback behavior**
   - HP bar updates on damage events.
   - Damage intake triggers red flash layer and smooth value transition.
   - Rapid consecutive hits must queue/merge visual transitions without strobing.

8. **Spell slot behavior**
   - Each slot shows spell icon, level number, and cooldown overlay state.
   - Cooldown overlay is visual-only; underlying timer authority stays in Spell Slot Manager.
   - If all active spells are max level, each slot shows a clear max-level marker.

9. **Element inventory behavior**
   - Exactly three visual slots mirror Element Inventory index order (0, 1, 2).
   - Empty slots render placeholder frame.
   - Occupied slots render icon + color + shape cue (accessibility requirement).

10. **XP/Level behavior**
    - XP bar fill updates from XPCollectedEvent and level reset behavior from LevelUpEvent.
    - Level number updates immediately on LevelUpEvent.
    - Level-up animation may play while preserving gameplay readability.

11. **Timer behavior**
    - Timer displays elapsed run time in `MM:SS` format.
    - Timer pauses during run-pause states controlled by Run Manager.

12. **Pause button behavior**
    - Pause button remains accessible during normal run state and boss state.
    - Pause input is forwarded to pause flow system; HUD does not own pause state machine.

13. **Damage numbers coordination boundary**
    - Floating damage numbers are owned by VFX System pooling.
    - HUD may provide screen-space anchoring rules and layering priority.
    - HUD never instantiates per-hit objects directly in gameplay path.

14. **Performance rules**
    - HUD updates must avoid runtime GC allocations in hot paths.
    - Reuse widget instances; do not create/destroy bars or slot widgets during run.
    - Floating damage numbers use pooled objects only.

### States and Transitions

| State | Entry Condition | Exit Condition | Behavior |
|---|---|---|---|
| Hidden | Before RunStartEvent is received | RunStartEvent | HUD root disabled, no gameplay subscriptions active |
| Active | RunStartEvent received and HUD initialized | Pause overlay or RunEndEvent | All core HUD widgets visible and processing event-driven updates |
| DimmedForLevelUp | Level-up screen opens | Level-up screen closes | HUD remains visible at reduced opacity, no ownership change of gameplay values |
| BossOverlayActive | BossSpawnedEvent received | BossDefeatedEvent or RunEndEvent | Boss HP bar visible with regular HUD still active |
| Paused | Pause flow entered | Pause flow exited | HUD remains visible, timer and animated interpolation rules freeze where required by pause policy |
| PostRun | RunEndEvent received | Next RunStartEvent | Runtime widgets frozen, summary handoff allowed, gameplay subscriptions cleaned |

Valid transition examples:
- `Hidden -> Active -> PostRun`
- `Active -> DimmedForLevelUp -> Active`
- `Active -> BossOverlayActive -> Active`
- `Active -> Paused -> Active`
- `BossOverlayActive -> Paused -> BossOverlayActive`

### Interactions with Other Systems

| System | Interface In | Interface Out | Responsibility Split |
|---|---|---|---|
| Event Bus | `Subscribe<T>` for run/gameplay events | None (HUD is consumer-only in MVP) | Event Bus transports updates; HUD maps payloads to visuals |
| Player Health (via Event Bus) | `PlayerDamagedEvent(currentHp, maxHp, damageAmount, runTime)` | None | Health system is authority; HUD renders HP state and damage feedback |
| XP & Leveling | `XPCollectedEvent`, `LevelUpEvent` | None | XP system owns progression; HUD visualizes XP fill and level number |
| Spell Slot Manager | `SpellCraftedEvent`, `SpellUpgradedEvent`, slot snapshot for cooldown/level/icon | None | Slot Manager owns spell runtime state; HUD renders slot visuals |
| Element Inventory | `ElementConsumedEvent`, `ElementAdded` and inventory snapshot | None | Inventory owns slot data; HUD mirrors 3-slot state |
| Boss System | `BossSpawnedEvent`, `BossDefeatedEvent`, boss HP snapshot/event stream | None | Boss system owns HP values and lifecycle; HUD controls visibility/placement |
| Run Manager | `RunStartEvent`, `RunEndEvent`, pause lifecycle signals, elapsed time source | Pause button command signal (input passthrough) | Run Manager owns run lifecycle; HUD owns presentation only |
| VFX System | Damage number spawn context and screen-space lane guidance | Optional UI-layer anchor data | VFX owns damage numbers and pooling; HUD owns layering contract |
| Accessibility Settings | Colorblind mode and scale profile | None | Accessibility system provides mode; HUD changes glyph + palette accordingly |

## Formulas

### Player HP Bar Fill

```text
fill_amount = current_hp / max_hp
```

| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| `current_hp` | float | 0..max_hp | PlayerDamagedEvent / health snapshot | Current player health after damage/heal resolution |
| `max_hp` | float | >0 | Player stat snapshot | Maximum health at current run state |

**Expected output range**: `0.0..1.0` (clamped).

### XP Bar Fill

```text
fill_amount = current_xp / xp_needed_for_next_level
```

| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| `current_xp` | float | 0..xp_needed_for_next_level | XP system event/snapshot | XP currently accumulated in active level |
| `xp_needed_for_next_level` | float | >0 | XP progression config/runtime | Requirement to reach next level |

**Expected output range**: `0.0..1.0` (clamped).

### Spell Cooldown Overlay Fill (Inverted)

```text
cooldown_fill = remaining_cooldown / total_cooldown
```

| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| `remaining_cooldown` | float | 0..total_cooldown | Spell slot runtime snapshot | Time remaining until spell ready |
| `total_cooldown` | float | >0 | Spell definition + level scaling | Total cooldown duration for current activation cycle |

**Expected output range**: `0.0..1.0` (clamped).
**Rendering rule**: visual overlay is inverted so `1.0` means fully blocked and `0.0` means ready.

### HP Transition Smoothing (Visual Only)

```text
display_hp_next = lerp(display_hp_current, target_hp, hp_lerp_speed * delta_time)
```

| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| `display_hp_current` | float | 0..max_hp | HUD local visual state | Currently rendered HP value |
| `target_hp` | float | 0..max_hp | Latest event-derived HP | Destination HP for interpolation |
| `hp_lerp_speed` | float | config range | HUD config | Visual smoothing speed coefficient |
| `delta_time` | float | 0..0.05 | runtime | Frame delta for interpolation |

**Note**: this formula is visual interpolation only and does not alter gameplay HP.

## Edge Cases

| Scenario | Expected Behavior | Rationale |
|---|---|---|
| Device resolution changes or orientation rotates | HUD re-anchors using responsive layout profile and safe-area bounds | Prevents off-screen UI and maintains readability on mobile |
| Level-up screen overlaps run UI | HUD stays visible but dimmed; critical bars remain readable | Preserves context while emphasizing level-up decision layer |
| All active spells reach max level | Slot badges show max-level indicator on all relevant slots | Avoids confusion about upgrade availability |
| Boss HP bar active while player under pressure | Boss bar appears top-center while player HP remains at corner | Keeps boss objective and survival info visible simultaneously |
| Rapid consecutive damage events | HP bar transitions smoothly toward latest value without jittering jumps | Maintains readability in high-hit moments |
| XP gain and LevelUpEvent in same frame | XP bar resolves to post-level state and level number increments once | Prevents double-animation or stale fill presentation |
| Spell crafted and upgraded chain in short interval | Slot icon updates deterministically, then level badge, then cooldown state | Ensures predictable visual order under event bursts |
| Element inventory full and consume/add race | HUD follows final authoritative inventory snapshot after event queue drain | Prevents transient incorrect slot visuals |
| Pause triggered during boss intro | Pause button interaction locks as directed by pause flow, HUD remains visible | Avoids input ambiguity during transition windows |
| Kill counter disabled in config | Counter region collapses without leaving dead spacing | Supports layout cleanliness when optional element is off |

## Dependencies

| System | Direction | Nature of Dependency |
|---|---|---|
| Event Bus | HUD depends on Event Bus | Primary update transport for all gameplay-to-UI changes |
| Run Manager | HUD depends on Run Manager | Run lifecycle, timer source, and pause state inputs |
| Player Health | HUD depends on Player Health events | HP values and damage feedback triggers |
| XP & Leveling | HUD depends on XP & Leveling | XP fill source, level number updates, level-up triggers |
| Element Inventory | HUD depends on Element Inventory | Three-slot element snapshot and add/consume updates |
| Spell Slot Manager | HUD depends on Spell Slot Manager | Six-slot spell icon/level/cooldown/ready data |
| Boss System | HUD depends on Boss System | Boss HP visibility and value stream during encounters |
| VFX System | VFX and HUD are coordinated peers | Damage number layering and pooled marker anchoring agreement |
| Accessibility System | HUD depends on accessibility profile | Colorblind-safe indicator mode and scale profile selection |

## Tuning Knobs

| Parameter | Current Value | Safe Range | Effect of Increase | Effect of Decrease |
|---|---|---|---|---|
| `hudScaleLandscape` | 1.0 | 0.8-1.4 | Better readability, more screen occupancy | More playfield visibility, lower readability |
| `hudScalePortrait` | 1.0 | 0.8-1.5 | Better touch readability | More compact but denser information |
| `hpLerpSpeed` | 12.0 | 4.0-24.0 | Faster HP reaction, sharper changes | Smoother but potentially laggy feedback |
| `damageFlashDurationMs` | 180 | 80-350 | Stronger hit clarity, more visual intensity | Subtler hit feedback |
| `damageFlashAlpha` | 0.45 | 0.15-0.70 | More urgent damage signal | Less intrusive damage signal |
| `bossBarHeightPxRef` | 18 | 12-32 | More readable boss HP at small screens | More gameplay view space |
| `spellSlotSpacingPxRef` | 6 | 2-14 | Clearer icon separation | More compact slot cluster |
| `cooldownOverlayOpacity` | 0.65 | 0.30-0.85 | Stronger cooldown readability | Cleaner icons with weaker cooldown legibility |
| `timerBlinkThresholdSec` | 60 | 0-180 | Earlier urgency signaling near objectives | Reduced urgency signal |
| `killCounterEnabled` | true | true/false | Adds run feedback and progression visibility | Reduces HUD noise |
| `colorblindModeDefault` | Off | Off/Protan/Deutan/Tritan | Accessibility-first defaults | Baseline art-accurate default palette |

## Acceptance Criteria

- [ ] HUD initializes on `RunStartEvent` and tears down subscriptions on `RunEndEvent` without leaked listeners.
- [ ] HUD remains display-only and never mutates gameplay state.
- [ ] HP bar updates from `PlayerDamagedEvent` and shows red flash feedback on valid damage intake.
- [ ] XP bar updates from `XPCollectedEvent` and level label updates from `LevelUpEvent`.
- [ ] Level-up feedback animation triggers from `LevelUpEvent` while preserving HUD readability.
- [ ] Element inventory displays exactly 3 slots and reflects `ElementAdded` + `ElementConsumedEvent` outcomes.
- [ ] Spell bar displays exactly 6 slots with icon, level number, and cooldown overlay per occupied slot.
- [ ] Spell level updates are reflected on `SpellCraftedEvent` and `SpellUpgradedEvent`.
- [ ] Boss HP bar appears on `BossSpawnedEvent` and hides on `BossDefeatedEvent` with victory flash.
- [ ] Player HP and boss HP remain simultaneously visible during boss encounters.
- [ ] Timer displays elapsed run time in `MM:SS` and respects pause/run lifecycle control.
- [ ] Pause button remains accessible in active run state and routes command to pause flow owner.
- [ ] No HUD updates in active run generate avoidable GC allocations in hot paths.
- [ ] Floating damage numbers are pooled and reused; HUD does not instantiate per-hit objects.
- [ ] HUD layout remains readable and non-obstructive across supported mobile resolutions and safe areas.
- [ ] Colorblind accessibility mode provides shape + color differentiation for element indicators.
- [ ] No UnityEvents, no gameplay logic ownership, and no hardcoded balance values in runtime HUD implementation.
