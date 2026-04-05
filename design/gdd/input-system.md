# Input System

> **Status**: Approved
> **Author**: gameplay-programmer
> **Last Updated**: 2026-04-04
> **Implements Pillar**: Death Teaches, Never Punishes; Build Diversity Over Build Power

## Overview

The Input System is a Foundation-layer system that converts player touch and editor keyboard actions into a single, stable movement signal for gameplay and a separate tap signal for UI. On mobile, movement is controlled by a fixed virtual joystick in the bottom-left area of the screen: the player drags inside a radius to set direction and speed magnitude. UI interactions are handled by tap input and must not interfere with movement capture. This system exists to make control feel predictable on mobile, support quick iteration in the Unity Editor with WASD fallback, and provide shared input data to Player Movement and UI with consistent normalization and AFK signaling.

## Player Fantasy

When the player moves, the character should feel immediately responsive and readable, even in crowded moments with many enemies. The player should feel that survival depends on their positioning skill, not on fighting the controls. During calm moments, if the player stops interacting, the game should transition safely into AFK auto-slow behavior (10 seconds no input) without feeling like punishment.

## Detailed Rules

### Detailed Rules

1. The system uses **Unity New Input System** as the only input backend for MVP.
2. Mobile movement input is read from a **fixed-position virtual joystick** anchored to the bottom-left HUD region.
3. Joystick behavior:
   - Touch begins inside joystick capture area -> joystick enters Dragging state.
   - Drag vector is measured from joystick center to touch position.
   - Vector length is clamped by `joystickRadiusPx`.
   - Movement magnitude is radius-based and normalized to `0..1`.
   - Output direction is normalized when magnitude is above deadzone.
4. Touch behavior for MVP:
   - **Tap** events are routed to UI systems (buttons, popups, menus).
   - **Drag** in joystick area controls movement.
   - **Multi-touch is not required** in MVP; first active touch is authoritative.
5. Deadzone:
   - `deadzoneNormalized` is configurable in `InputConfigSO`.
   - If `rawMagnitude01 < deadzoneNormalized`, movement output is zero.
6. AFK detection:
   - If there is no movement or tap input for `afkTimeoutSeconds` (default 10.0), set `isAfk = true`.
   - While AFK is true, system emits `afkSlowMultiplier` for movement (default 0.35) to support Section 10 auto-slow behavior.
   - Any valid input immediately clears AFK state.
7. Editor fallback:
   - In Unity Editor and standalone test builds, WASD (and Arrow keys optional mirror) drive movement when touch joystick is absent.
   - Keyboard vector is normalized using the same deadzone + magnitude pipeline as mobile output.
8. Input system must be deterministic per frame:
   - Sample input in one update pass.
   - Publish one immutable frame result (`JoystickInput`) per frame.

### Data Contract

```csharp
public struct JoystickInput
{
    public Vector2 Direction;          // normalized, zero when under deadzone
    public float Magnitude01;          // 0..1 after deadzone remap
    public bool IsActive;              // true when movement input is non-zero
    public bool IsTapThisFrame;        // UI tap pulse
    public bool IsAfk;                 // true after timeout without input
    public float AfkSlowMultiplier;    // movement multiplier when IsAfk
    public double Timestamp;           // input sample time
}
```

### Configuration Asset (ScriptableObject)

```csharp
[CreateAssetMenu(menuName = "KokTengri/Input/InputConfigSO")]
public class InputConfigSO : ScriptableObject
{
    public float joystickRadiusPx = 120f;
    public float deadzoneNormalized = 0.15f;
    public float afkTimeoutSeconds = 10f;
    public float afkSlowMultiplier = 0.35f;
}
```

### State and Transition Table

| State | Entry Condition | Exit Condition | Behavior |
|-------|-----------------|----------------|----------|
| Idle | No active drag, no key movement | Touch drag starts in joystick area OR key movement detected | Emits zero movement; AFK timer counts up |
| Dragging | Valid touch drag captured by joystick | Touch released/cancelled | Emits direction + magnitude from touch delta/radius |
| KeyboardFallback | Editor/desktop with WASD or Arrow key vector | Keys released OR touch drag takes priority | Emits direction + magnitude from key vector |
| AFKSlow | `timeSinceLastInput >= afkTimeoutSeconds` | Any tap/drag/key input | Sets `IsAfk=true`, emits `afkSlowMultiplier` for movement consumers |

## Formulas

### Raw Joystick Magnitude (radius-based)

```
rawMagnitude01 = clamp( length(dragDeltaPx) / joystickRadiusPx, 0, 1 )
```

| Variable | Type | Range | Source | Description |
|----------|------|-------|--------|-------------|
| `dragDeltaPx` | Vector2 | 0..~400 px | touch drag | Pixel delta from joystick center to current touch |
| `joystickRadiusPx` | float | 64..220 px | InputConfigSO | Maximum joystick drag radius in pixels |

**Expected output range**: `0.0..1.0`

### Deadzone-Adjusted Magnitude

```
if rawMagnitude01 < deadzoneNormalized:
    magnitude01 = 0
else:
    magnitude01 = (rawMagnitude01 - deadzoneNormalized) / (1 - deadzoneNormalized)
```

| Variable | Type | Range | Source | Description |
|----------|------|-------|--------|-------------|
| `rawMagnitude01` | float | 0..1 | computed | Radius-normalized raw magnitude |
| `deadzoneNormalized` | float | 0.05..0.30 | InputConfigSO | Minimum movement threshold |
| `magnitude01` | float | 0..1 | computed | Final movement intensity after deadzone remap |

**Expected output range**: `0.0..1.0`

### Final Movement Vector

```
direction = (rawMagnitude01 <= 0) ? Vector2.zero : normalize(dragDeltaPx)
movementVector = direction * magnitude01
```

| Variable | Type | Range | Source | Description |
|----------|------|-------|--------|-------------|
| `direction` | Vector2 | unit vector or zero | computed | Movement direction |
| `magnitude01` | float | 0..1 | computed | Intensity multiplier |
| `movementVector` | Vector2 | -1..1 per axis | computed | Final frame movement input |

**Expected output range**: each axis `-1.0..1.0`

### AFK Trigger

```
isAfk = (timeSinceLastInput >= afkTimeoutSeconds)
effectiveSpeedMultiplier = isAfk ? afkSlowMultiplier : 1.0
```

| Variable | Type | Range | Source | Description |
|----------|------|-------|--------|-------------|
| `timeSinceLastInput` | float | 0..infinite | runtime timer | Seconds since last tap/drag/key event |
| `afkTimeoutSeconds` | float | 5..20 s | InputConfigSO | AFK trigger timeout (default 10s) |
| `afkSlowMultiplier` | float | 0.1..0.7 | InputConfigSO | Movement multiplier when AFK |

**Expected output range**: `effectiveSpeedMultiplier` is `0.1..1.0`

## Edge Cases

| Scenario | Expected Behavior | Rationale |
|----------|-------------------|-----------|
| Touch starts outside joystick area | Do not capture movement drag; allow UI hit-testing first | Prevent accidental movement when tapping UI |
| Touch starts in joystick area then exits radius | Keep drag active, clamp magnitude to 1.0 | Stable control at boundary, no sudden spikes |
| Drag magnitude oscillates near deadzone | Use strict threshold + remap; output stays zero below threshold | Prevent jitter and micro-drift |
| Tap and drag begin same frame | Joystick capture has priority if start position is in joystick area; otherwise tap is UI | Deterministic routing of first touch |
| Input stops during combat | AFK timer starts; at 10s set `IsAfk=true` and output slow multiplier | Matches Section 10 AFK auto-slow spec |
| Any input after AFK | Clear AFK state immediately and restore multiplier to 1.0 | Player regains full control instantly |
| Editor has no touch device | WASD fallback supplies movement vector with same normalization rules | Fast iteration and QA in editor |
| Multiple fingers on screen (MVP) | Ignore additional touches; track first active touch only | Simplifies MVP and avoids gesture ambiguity |

## Dependencies

| System | Direction | Nature of Dependency |
|--------|-----------|---------------------|
| Unity New Input System package | This depends on | Provides action maps and device abstraction for touch/keyboard |
| HUD / Virtual Joystick UI | This depends on | Provides fixed joystick anchor/capture area in bottom-left |
| Player Movement | Player Movement depends on this | Consumes `JoystickInput` (direction, magnitude, AFK multiplier) |
| UI Interaction Layer | UI depends on this | Consumes `IsTapThisFrame` when touch is not captured by joystick |
| Run Manager | Run Manager depends on this | May read AFK state for analytics or session behavior |

## Tuning Knobs

| Parameter | Current Value | Safe Range | Effect of Increase | Effect of Decrease |
|-----------|---------------|------------|--------------------|--------------------|
| `joystickRadiusPx` | 120 px | 64-220 px | Smoother control granularity, longer thumb travel | Faster full-speed reach, can feel twitchy |
| `deadzoneNormalized` | 0.15 | 0.05-0.30 | Less accidental drift, but less responsive low-speed control | More responsiveness, but increased jitter risk |
| `afkTimeoutSeconds` | 10.0 s | 5-20 s | AFK triggers less often | AFK triggers more aggressively |
| `afkSlowMultiplier` | 0.35 | 0.10-0.70 | Less severe AFK slowdown | Stronger AFK slowdown |
| `editorKeyboardEnabled` | true | true/false | Enables rapid test iteration without touch | Forces touch-only testing workflow |

## Acceptance Criteria

- [ ] Mobile: Fixed virtual joystick is visible/active at bottom-left and does not drift position.
- [ ] Mobile: Dragging from joystick center produces normalized `Magnitude01` in the `0..1` range based on radius.
- [ ] Mobile: Deadzone from `InputConfigSO.deadzoneNormalized` suppresses movement below threshold.
- [ ] Mobile: Tap interactions trigger UI events when touch is outside joystick capture.
- [ ] MVP scope: Multi-touch is intentionally unsupported; first active touch drives input.
- [ ] AFK: After exactly `afkTimeoutSeconds` with no input (default 10s), `IsAfk` becomes true and auto-slow multiplier applies.
- [ ] AFK exit: Any new drag/tap/key input clears AFK immediately.
- [ ] Editor: WASD fallback produces movement equivalent to joystick normalization path.
- [ ] Data-driven: No hardcoded joystick radius/deadzone/AFK timeout in runtime logic; all values read from `InputConfigSO`.
- [ ] Performance: Input read + processing cost is **< 0.01 ms/frame** on target mid-range mobile profile (60 FPS budget).
