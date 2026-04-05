# Player Movement System

> **Status**: Draft
> **Author**: gameplay-programmer
> **Last Updated**: 2026-04-04
> **Implements Pillar**: Death Teaches, Never Punishes

## Overview

Player Movement defines how the hero navigates a top-down 2D bounded arena in Kök Tengri using the virtual joystick signal produced by the Input System. Movement is continuous while joystick input is held, supports analog magnitude, and is constrained by arena boundaries so the player can never leave playable space. This system is responsible for position updates, movement state transitions, boundary clamping, knockback response after contact damage, temporary invincibility windows, and movement freeze gates during pause-like gameplay moments (level-up screen and boss transitions). The system exists to make positioning skill readable and reliable, because positioning is the player's primary survival tool in a survivor-like combat loop.

## Player Fantasy

The player should feel like a disciplined shaman-warrior who survives through control and awareness rather than random luck. Movement should feel immediate and trustworthy, with smooth directional control under pressure and clear consequences when the player is surrounded. When hit, the brief knockback and recovery window should communicate danger without creating helplessness. The overall feeling should be: "I always know why I moved, why I was hit, and how to recover my position."

## Detailed Design

### Detailed Rules

1. **Movement authority and cadence**
   - Player movement is evaluated every gameplay frame while the run is in active simulation state.
   - Position updates are frame-rate independent using delta time.
   - Movement processing target cost is `< 0.1 ms/frame` on target mid-range mobile profile.

2. **Input consumption contract**
   - Movement consumes `JoystickInput` from Input System as the single source of directional input.
   - `Direction` is normalized when above deadzone, and `Magnitude01` is in `0..1`.
   - Full analog movement is supported by scaling speed with joystick magnitude.
   - If design mode requires 8-direction snapping, it is a configurable option, disabled by default in MVP.

3. **Base movement behavior**
   - Base move speed is configured at `3.0 units/second` from player data config.
   - While valid joystick input is active, player continuously moves in input direction.
   - If joystick input is below deadzone, movement intent is zero and state resolves to Idle unless overridden by AFK Auto-Move.

4. **AFK auto-move behavior**
   - If Input System reports `IsAfk=true` after 10 seconds idle, movement enters AFK Auto-Move state.
   - AFK Auto-Move speed is `30%` of current normal movement speed.
   - AFK direction uses the last valid non-zero input direction; if none exists this run, fallback direction comes from config default (`Vector2.right` by default).
   - Any new valid input exits AFK Auto-Move immediately.

5. **Arena bounds and clamping**
   - Arena playable area is an axis-aligned bounds region provided by Arena/Level system.
   - Final candidate position is clamped to bounds each movement step.
   - Player cannot cross, tunnel through, or remain outside boundaries from normal movement or knockback.

6. **Damage contact and knockback**
   - Enemy contact damage is owned by Enemy Damage/Player Health systems; Movement reacts to damage events.
   - On valid contact damage, Movement applies a brief knockback away from damage source.
   - During knockback, directional input does not override displacement authority.
   - After knockback phase, player enters i-frame recovery where damage is ignored for configured duration.

7. **I-frame and recovery behavior**
   - Default i-frame duration is `0.5s`, configurable via player combat config.
   - Default knockback duration is `0.15s`, configurable.
   - During knockback recovery window, movement speed is reduced to `50%`.
   - Multiple damage events during active i-frames do not re-apply knockback, but can extend i-frame timer by configurable extension rule.

8. **Movement freeze gates**
   - Movement is fully frozen when level-up selection screen is open (run paused gate).
   - Movement is fully frozen during boss transition lock windows.
   - Frozen state has higher priority than Moving, AFK Auto-Move, and Knockback recovery.

9. **Physics implementation recommendation (MVP)**
   - Use `Rigidbody2D`-driven movement (kinematic body with `MovePosition`) rather than direct transform writes.
   - Rationale:
     - preserves stable collision/contact behavior with enemies and arena colliders,
     - keeps knockback integration and collision response predictable,
     - avoids transform-physics desync issues common in per-frame transform movement.
   - Movement intent is computed in Update context and applied through physics step in FixedUpdate with accumulated delta.

10. **Camera reference output**
    - Player world position is the camera follow anchor.
    - Movement publishes `PlayerPositionEvent` through Event Bus after authoritative position update.
    - Camera and AI subscribers consume this signal read-only; they do not mutate player position.

11. **Out-of-scope clarification (MVP)**
    - No dash, dodge roll, blink, or movement-cancel abilities in MVP.
    - Any burst mobility is post-MVP and must be specified in a separate GDD.

### States and Transitions

| State | Entry Condition | Exit Condition | Behavior |
|---|---|---|---|
| Idle | No valid movement input, not AFK auto-move, not frozen, not in knockback | Valid movement input OR AFK timeout OR freeze gate OR damage knockback trigger | Position unchanged except external displacement; listens for transition triggers |
| Moving | Valid joystick direction above deadzone and not blocked by higher-priority states | Input below deadzone -> Idle; damage event -> Knockback; freeze gate -> Frozen; AFK timeout -> AFK Auto-Move | Continuous analog movement with speed modifiers and boundary clamping |
| Knockback | Contact damage accepted and knockback authority granted | Knockback timer expires -> Invincible | Forced movement along knockback vector; player input ignored for displacement |
| Invincible | Knockback phase ends and i-frame timer starts | I-frame timer expires -> Moving or Idle (based on input) OR freeze gate -> Frozen | Ignores new damage for i-frame window; applies 50% movement speed recovery rule |
| Frozen | Level-up screen open, boss transition lock, or explicit movement lock status | Freeze source cleared -> Idle or Moving based on input | Movement disabled, velocity zeroed, no position integration |
| AFK Auto-Move | Input System reports AFK timeout reached and movement not frozen | Any new valid input -> Moving; freeze gate -> Frozen; explicit AFK disable -> Idle | Slow auto-movement at 30% speed using retained/fallback direction |

Valid transition sequence examples:
- `Idle -> Moving -> Idle`
- `Moving -> Knockback -> Invincible -> Moving`
- `Moving -> Knockback -> Invincible -> Idle`
- `Any active locomotion state -> Frozen -> Idle/Moving`
- `Idle -> AFK Auto-Move -> Moving`

### Interactions with Other Systems

| System | Interface In | Interface Out | Responsibility Split |
|---|---|---|---|
| Input System | `JoystickInput(Direction, Magnitude01, IsAfk, AfkSlowMultiplier)` | None | Input System owns capture/normalization/AFK timing; Movement owns locomotion response |
| Event Bus | Subscribes to damage + freeze-related events | Publishes `PlayerPositionEvent`, `PlayerMovementStateChangedEvent` | Event Bus routes events; Movement owns state transitions |
| Enemy Damage / Player Health | Receives contact damage events with source position | Movement emits knockback completion signal (optional) | Damage systems decide if hit is valid; Movement applies displacement response |
| Spell Effects (Rüzgar Koşusu) | Receives additive speed modifier terms | None | Spell system computes buff values; Movement consumes modifiers in speed multiplier |
| Class System (Mergen passive) | Receives class passive value (`+10% move speed` when active class is Mergen) | None | Class system owns class identity; Movement applies passive multiplicatively |
| Meta Progression | Receives persistent speed upgrade level | None | Meta system owns progression level; Movement converts level to multiplier (`+2%/level`, max 10) |
| Camera Follow | Subscribes to player position updates | Optional camera constraint feedback | Movement is authoritative position source; Camera is read-only follower |
| HUD | Receives current effective speed modifier summary | None | HUD displays active speed boost indicators only; no movement authority |
| Level-Up Flow | Receives freeze/unfreeze lifecycle events | None | Level-Up flow controls pause gate; Movement enforces freeze |
| Boss Flow | Receives transition lock start/end events | None | Boss flow controls temporary movement lock; Movement enforces freeze |

## Formulas

### Position Delta

```text
position_delta = direction_normalized × move_speed × speed_multiplier × input_magnitude × delta_time
```

| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| `direction_normalized` | Vector2 | unit vector or zero | Input System | Direction from joystick after deadzone |
| `move_speed` | float | 1.0-8.0 | PlayerConfigSO | Base movement speed (`3.0` default) |
| `speed_multiplier` | float | 0.1-3.0 | computed | Combined multiplier from spell/class/meta/recovery/AFK |
| `input_magnitude` | float | 0.0-1.0 | Input System | Analog intensity from joystick |
| `delta_time` | float | 0.0-0.05 | runtime frame timing | Time step for frame-rate independence |

**Expected output range**: `0.0..~0.4 units/frame` at default tuning and 60 FPS.

### Speed Multiplier Composition

```text
speed_multiplier = base_multiplier
                 × (1 + spell_boost)
                 × (1 + class_passive)
                 × (1 + meta_progress)
                 × recovery_multiplier
                 × afk_multiplier
```

Where:

```text
base_multiplier = 1.0
class_passive = 0.10 when class == Mergen else 0.0
meta_progress = min(meta_speed_level, 10) × 0.02
recovery_multiplier = 0.5 during knockback recovery else 1.0
afk_multiplier = 0.3 during AFK Auto-Move else 1.0
```

| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| `spell_boost` | float | 0.0-1.0 | Spell effect runtime | Speed bonus from Rüzgar Koşusu and future buffs |
| `class_passive` | float | 0.0 or 0.10 | Class System | Mergen passive movement bonus |
| `meta_speed_level` | int | 0-10 | Meta Progression save data | Persistent speed upgrade tier |
| `recovery_multiplier` | float | 0.5 or 1.0 | Movement state | Reduced speed in post-knockback recovery |
| `afk_multiplier` | float | 0.3 or 1.0 | AFK state | Slow auto-move scaling |

**Stacking rule**: modifiers are multiplicative, not additive.

### Knockback Vector

```text
knockback_vector = normalize(player_pos - damage_source_pos) × knockback_force
```

| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| `player_pos` | Vector2 | arena bounds | Movement runtime | Current player position |
| `damage_source_pos` | Vector2 | arena bounds | Damage event payload | Position of hit source |
| `knockback_force` | float | 0.5-6.0 | PlayerCombatConfigSO | Force scalar for displacement |

**Expected behavior**: displacement is applied over `knockback_duration` and clamped to arena.

### Timers

```text
i_frame_active = elapsed_since_hit < i_frame_duration
knockback_active = elapsed_since_hit < knockback_duration
```

| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| `i_frame_duration` | float | 0.1-1.0 s | PlayerCombatConfigSO | Damage immunity window (`0.5s` default) |
| `knockback_duration` | float | 0.05-0.4 s | PlayerCombatConfigSO | Forced displacement window (`0.15s` default) |

## Edge Cases

| Scenario | Expected Behavior | Rationale |
|---|---|---|
| Joystick at maximum deflection | Clamp magnitude to `1.0`; speed cannot exceed computed max multiplier output | Prevents unintended speed spikes from oversized drag values |
| Joystick barely moved | Input below deadzone is ignored and state remains Idle (unless AFK Auto-Move active) | Eliminates jitter and drift noise |
| Player pushed into boundary during knockback | Clamp resulting position to arena bounds; no escape outside play area | Maintains spatial rules and camera assumptions |
| Multiple damage sources in same frame | Apply only one knockback impulse; if i-frames already active, do not stack displacement; optionally extend i-frames by extension rule | Prevents pinball behavior and control loss |
| Speed modifiers from spell/class/meta | Apply multiplicatively in fixed evaluation order | Produces predictable scaling and avoids runaway additive stacking |
| Level-up screen opens mid-move | Transition to Frozen immediately; movement integration stops until screen closes | Aligns with pause-driven level-up UX |
| Boss transition starts during knockback | Freeze gate takes priority; resume from Idle/Moving when transition lock ends | Ensures cinematic/control consistency |
| Player in i-frame recovery with movement input | Movement allowed at 50% speed until recovery multiplier expires | Communicates hit consequence without full stun |
| AFK activated with no last valid direction | Use configured fallback direction and slow speed until any real input arrives | Guarantees deterministic AFK movement behavior |
| Player receives damage while Frozen | Health system may process valid hit per global rules; Movement does not integrate displacement until Frozen exits | Keeps state authority clear and avoids hidden movement during lock |

## Dependencies

| System | Direction | Nature of Dependency |
|---|---|---|
| Input System | Player Movement depends on Input System | Reads joystick direction, magnitude, AFK state, and slow multiplier |
| Event Bus System | Player Movement depends on Event Bus | Subscribes to damage/freeze events and publishes position/state events |
| Player Health / Enemy Damage | Player Movement depends on combat signals | Triggers knockback and i-frame state transitions from contact damage |
| Spell Effects | Player Movement depends on spell buffs | Consumes Rüzgar Koşusu movement speed bonus |
| Class System | Player Movement depends on class metadata | Consumes Mergen passive (`+10% move speed`) |
| Meta Progression | Player Movement depends on persistent upgrades | Applies movement speed progression (`+2%` per level, max 10) |
| Camera Follow | Camera depends on Player Movement | Uses player position as follow anchor |
| HUD | HUD depends on Player Movement | Displays active movement speed modifier context |
| Level-Up Flow | Player Movement depends on Level-Up flow | Freezes movement while level-up selection is open |
| Boss Flow | Player Movement depends on boss transition flow | Freezes movement during transition locks |

## Tuning Knobs

| Parameter | Current Value | Safe Range | Effect of Increase | Effect of Decrease |
|---|---|---|---|---|
| `baseMoveSpeed` | 3.0 units/s | 2.0-5.0 | Faster baseline kiting and repositioning | Slower traversal, higher danger density |
| `deadzoneNormalized` | 0.15 | 0.05-0.30 | Less drift, lower micro-control | More sensitivity, more jitter risk |
| `afkTimeoutSeconds` | 10.0 s | 5.0-20.0 | AFK triggers less often | AFK triggers sooner |
| `afkAutoMoveMultiplier` | 0.30 | 0.10-0.60 | AFK movement less punishing | AFK movement slower and safer |
| `knockbackDurationSeconds` | 0.15 s | 0.05-0.40 | Longer displacement lock and danger | Quicker control recovery |
| `knockbackForce` | 1.8 | 0.5-6.0 | Larger displacement from hits | Softer hit reaction |
| `iFrameDurationSeconds` | 0.50 s | 0.10-1.00 | More forgiveness after hit | Tighter punishment window |
| `recoverySpeedMultiplier` | 0.50 | 0.30-0.90 | Faster post-hit movement | Heavier post-hit slowdown |
| `metaSpeedPerLevel` | +0.02 | +0.01 to +0.05 | Stronger long-term speed scaling | Flatter progression impact |
| `metaSpeedMaxLevel` | 10 | 5-20 | Higher progression ceiling | Earlier progression cap |
| `positionEventRateHz` | 60 | 10-60 | Smoother camera/AI updates, higher event load | Lower event overhead, less smooth follow |

## Acceptance Criteria

- [ ] Player moves continuously in top-down arena while joystick input is active, with analog magnitude influence.
- [ ] Base movement speed is sourced from data and defaults to `3.0 units/second`.
- [ ] Movement integration is delta-time based and frame-rate independent.
- [ ] Arena boundary clamping prevents leaving playable area in all states, including knockback.
- [ ] State flow `Idle -> Moving -> Idle` behaves deterministically from input transitions.
- [ ] Damage flow `Moving + Taking Damage -> Knockback -> Invincible -> Moving/Idle` behaves as specified.
- [ ] Frozen gate blocks movement during level-up selection and boss transition windows.
- [ ] AFK auto-move activates after 10s idle and moves at `30%` speed until valid input resumes.
- [ ] Knockback uses source-relative vector and respects configured duration/force values.
- [ ] I-frames default to `0.5s`; additional simultaneous hits do not cause stacked knockback impulses.
- [ ] During knockback recovery, movement speed multiplier is reduced to `50%`.
- [ ] Speed modifiers from Rüzgar Koşusu, Mergen passive, and meta upgrades stack multiplicatively.
- [ ] Camera follow receives authoritative player position events from movement updates.
- [ ] HUD can display active movement speed modifier context when a movement buff is active.
- [ ] No dash or dodge roll behavior exists in MVP implementation.
- [ ] Performance: movement update path executes under `< 0.1 ms/frame` on target profile.
- [ ] No hardcoded gameplay balance constants in runtime logic; values come from ScriptableObject/config data.
- [ ] No UnityEvents, no coroutines for movement-critical timing, and no `FindObjectOfType` in movement runtime path.
