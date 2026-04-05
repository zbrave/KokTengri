# Spell Effects System

> **Status**: Approved
> **Author**: zbrave + game-designer
> **Last Updated**: 2026-04-04
> **Implements Pillar**: Build Diversity Over Build Power, Every Element Matters, Clarity Through Consistency

## Overview

Spell Effects is the combat execution system that turns an activated spell slot into world behavior: orbiting hazards, directional projectiles, and contact-driven damage events. It sits between Spell Slot Manager (timing authority) and combat resolution systems (Object Pool, Damage Calculator, Event Bus), and it never owns cooldown logic or local damage formulas. For MVP-1, this document defines only three initial effects: **Alev Halkası** (Od + Od, orbit), **Kılıç Fırtınası** (Od + Temür, projectile), and **Kaya Kalkanı** (Yer + Yer, orbit). The system exists to ensure every spell activation is deterministic, data-driven through `SpellDefinitionSO`, performant under heavy combat density, and consistent with the same formula rules used everywhere else in Kök Tengri.

## Player Fantasy

The player should feel like a ritual combatant surrounded by intelligent, self-sustaining elemental power. Orbit spells should create a moving zone of control that rewards positioning and pathing through enemy packs, while projectile spells should feel like decisive directional bursts that punish approach lines. Upgrading a spell should visibly reshape battlefield geometry (more rings, more rocks, more swords, wider spread), not just increase hidden numbers. The emotional result is readable escalation: each level-up expands tactical options and screen presence without breaking clarity.

## Detailed Rules

### Detailed Rules

1. **System ownership boundary**
   - Spell Slot Manager decides *when* an effect should activate.
   - Spell Effects decides *what* happens during that activation.
   - Damage Calculator decides *how much* damage each valid hit deals.
   - Object Pool decides *how* runtime entities are spawned/reused.

2. **Mandatory effect base contract**
   - All concrete effects implement `SpellEffectBase`.
   - Every activation request must include:
     - `spellId`
     - `spellLevel`
     - `playerPosition`
     - `playerFacingDirection`
     - optional targeting context (nearest enemy snapshot, if available)
   - `SpellEffectBase` validates input and returns `ActivationSucceeded` or `ActivationFailed`.

3. **Configuration source of truth**
   - All effect tunables are read from `SpellDefinitionSO` (and linked config assets).
   - No hardcoded gameplay balance values in runtime effect logic.
   - Required data fields per spell include at least:
     - `baseDamage`
     - `cooldown`
     - `effectRadius`
     - `projectileSpeed`
     - `orbitSpeed`
     - `tickInterval`
     - `maxDistance` (projectile spells)

4. **Activation type policy (MVP-1 scope)**
   - **Orbit type**: continuous while spell slot is active (no cooldown cycle in this system).
   - **Projectile type**: periodic activation requests received from Slot Manager cooldown loop.
   - MVP-1 implements only Orbit + Projectile behavior families.
   - AoE, Aura, Passive categories are documented but intentionally not implemented here.

5. **Damage policy**
   - Spell Effects never computes local damage formulas.
   - On hit/contact, Spell Effects requests `Spell Damage (MVP)` from Damage Calculator using:
     - `baseDamage` from `SpellDefinitionSO`
     - `spellLevel`
     - spell recipe element context
     - target enemy type context
     - class and meta modifiers provided by caller context

6. **Pooling policy**
   - All spawned spell entities (swords, orbit rocks, orbit fire visuals/hit proxies, area indicators) must be requested from Object Pool.
   - If pool returns `null`, activation fails gracefully for that spawn and logs warning telemetry.
   - No instantiate/destroy loop in active combat.

7. **Timing policy**
   - No coroutines for gameplay-critical effect timing.
   - Use deterministic Update + delta time timers.
   - Contact damage for orbit effects is gated per enemy by tick interval.

8. **Architectural restrictions**
   - No UnityEvents in runtime spell execution flow.
   - No `FindObjectOfType` calls in spell runtime updates.
   - No direct UI state mutation from Spell Effects.
   - Cross-system notifications use Event Bus.

9. **Failure containment rule**
   - A single failed activation (pool exhausted, invalid context, missing target) does not block future activations.
   - Effect layer returns failure to Slot Manager and continues normal processing next cycle.

10. **Upgrade application rule**
    - New level parameters are applied on the next activation snapshot.
    - Projectile spells apply upgraded values on next projectile volley.
    - Orbit spells apply upgraded orbit layout on next orbit reconfiguration pass; already-applied tick events in current interval are not retroactively recalculated.

### Spell Families (Reference Taxonomy)

| Category | Runtime Pattern | MVP-1 Implementation | Notes |
|---|---|---|---|
| Orbit | Continuous entity orbiting around player | Yes (`Alev Halkası`, `Kaya Kalkanı`) | Contact tick damage with per-enemy gating |
| Projectile | Periodic directional shots | Yes (`Kılıç Fırtınası`) | Straight-line travel, first-hit collision |
| AoE | Periodic area pulses | No | Reserved for future spells |
| Aura | Continuous area around player | No | Reserved for future spells |
| Passive | Periodic buff/heal without projectile | No | Reserved for future spells |

### Per-Spell Behavior Specification (MVP-1)

#### A) Alev Halkası (Od + Od)

- **Type**: Orbit, continuous
- **Intent**: Area denial and close-range attrition around player
- **Activation source**: Slot occupied + continuous active state
- **Behavior**:
  1. Build orbit layout from current level profile.
  2. Spawn orbit ring entities from pool (visual + collision/hit proxy).
  3. Attach orbital center to player position stream.
  4. Rotate rings using configured orbit angular speed.
  5. On enemy contact, apply tick-gated damage using Damage Calculator.

**Alev Halkası level profile**

| Level | Ring Count | Radius Profile | Damage Profile | Coverage Intent |
|---|---:|---|---|---|
| 1 | 1 | small radius | base | single near-defense orbit |
| 2 | 1 | larger radius | increased | safer spacing + higher contact uptime |
| 3 | 2 | inner + outer radii | increased | dual-lane control |
| 4 | 2 | larger inner + larger outer | increased | stronger ring sweep area |
| 5 | 3 | inner + mid + outer | highest | max rotational zone control |

#### B) Kılıç Fırtınası (Od + Temür)

- **Type**: Projectile, periodic
- **Intent**: Directional burst for lane clearing and forward pressure
- **Activation source**: Slot Manager cooldown completion
- **Base damage reference**: 12 (from master spec allowed range 5-25)
- **Behavior**:
  1. Resolve launch direction:
     - Use last non-zero movement direction when available.
     - If player is stationary and nearest enemy exists, aim toward nearest enemy.
     - If no enemy available, keep last movement direction fallback.
  2. Build volley pattern by level (count + spread angle).
  3. Request sword projectile instances from Object Pool.
  4. Initialize each projectile with speed, direction, max distance/lifetime.
  5. Move projectiles linearly.
  6. On first valid enemy hit: apply damage, publish hit event, return projectile to pool.
  7. If projectile lifetime expires or exits arena bounds: return to pool.

**Kılıç Fırtınası level profile**

| Level | Sword Count | Spread Profile | Speed Profile | Damage Profile |
|---|---:|---|---|---|
| 1 | 1 | narrow | base | base |
| 2 | 1 | narrow | faster | increased |
| 3 | 2 | slight spread | faster | increased |
| 4 | 3 | wider spread | faster | increased |
| 5 | 4 | wide spread | fastest in safe range | highest |

#### C) Kaya Kalkanı (Yer + Yer)

- **Type**: Orbit, continuous
- **Intent**: Defensive orbit pressure with heavier, fewer contacts than fire rings
- **Activation source**: Slot occupied + continuous active state
- **MVP-1 scope note**: Projectile blocking/absorb behavior is explicitly deferred to MVP-2.
- **Behavior**:
  1. Build orbit rock count and radius from level profile.
  2. Spawn rock orbit entities from pool.
  3. Keep rock anchors synchronized to player position.
  4. Apply contact damage on tick-gated enemy overlap.
  5. Do not absorb incoming damage in MVP-1.

**Kaya Kalkanı level profile**

| Level | Rock Count | Size/Radius Profile | Damage Profile | Coverage Intent |
|---|---:|---|---|---|
| 1 | 2 | small | base | limited close shield line |
| 2 | 2 | larger | increased | thicker contact windows |
| 3 | 3 | medium | increased | better angular coverage |
| 4 | 4 | larger | increased | high coverage around player |
| 5 | 5 | largest within readability budget | highest | maximum shield coverage |

### Runtime States and Transitions

#### A) Base Effect Lifecycle

| State | Entry Condition | Exit Condition | Behavior |
|---|---|---|---|
| Inactive | Slot missing or run ended | Slot activated by manager | No runtime entities active |
| PendingActivation | Activation request received | Validation success/failure | Resolve config, context, pool availability |
| ActiveContinuous | Orbit effect initialized | Slot removed or run end | Maintains orbit entities and contact checks |
| ActiveProjectile | Projectile volley fired | All spawned projectiles returned | Tracks projectile travel/collision/lifetime |
| ActivationFailed | Any validation/spawn failure | Next manager activation request | Logs warning and exits safely |
| Cleaning | Run end or slot cleanup | All pooled entities returned | Deterministic teardown |

#### B) Projectile Entity Sub-State (Kılıç Fırtınası)

| State | Entry Condition | Exit Condition | Behavior |
|---|---|---|---|
| Spawned | Pool take success | First update tick | Initialize transform, velocity, timers |
| Traveling | First update tick | Hit / lifetime end / out-of-bounds | Move in straight line, test collisions |
| HitResolved | First enemy collision | Return complete | Damage applied once, event published |
| Expired | Lifetime or distance cap reached | Return complete | No damage event |
| Returned | Return called | Next take | Inactive and reset |

#### C) Orbit Contact Tick Sub-State (Alev Halkası, Kaya Kalkanı)

| State | Entry Condition | Exit Condition | Behavior |
|---|---|---|---|
| Eligible | Enemy enters contact and no active lockout | Damage applied | Allowed to deal contact tick |
| CooldownPerEnemy | Tick applied to specific enemy | Tick interval elapsed for that enemy | Prevent repeated same-interval ticks |
| ReEligible | Lockout elapsed | Next contact-tick | Enemy can be damaged again |

### Interactions with Other Systems

| System | Interface In | Interface Out | Responsibility Split |
|---|---|---|---|
| Spell Slot Manager | `RequestSpellActivation(spellId, level, context)` | `ActivationSucceeded/ActivationFailed` | Slot Manager owns cadence; Spell Effects owns execution |
| Damage Calculator | `CalculateSpellDamage(input)` | Integer damage output per hit | Spell Effects provides context; calculator owns formula |
| Object Pool | `TryTake(PoolType.Projectile/VFX)` and `Return(instance)` | Pool success/null + reuse lifecycle | Effects request entities; pool owns memory/perf |
| Event Bus | Subscribe to run lifecycle, publish spell impact events | `SpellEffectActivated`, `SpellEffectHit`, `SpellEffectFailed` | Effects emit runtime signals; bus routes to consumers |
| Enemy Health & Damage | Receives hit payloads | Hit accepted/rejected by target validity | Effects detect contact; enemy system applies HP mutation |
| Player Movement Context | Player position + last movement/facing vectors | None | Movement system provides aim/orbit anchor context |
| HUD/VFX/Audio | Subscribe to effect events only | Presentation responses | Presentation reacts; no gameplay authority in UI layer |

## Formulas

### 1) Spell Damage Delegation Formula (Authoritative)

```text
level_multiplier = 1 + 0.25 × (spell_level - 1)
raw_spell_damage = base_damage × level_multiplier × element_multiplier × class_bonus × meta_power_multiplier
final_spell_damage = max(1, floor(raw_spell_damage))
```

| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| `base_damage` | float | 5-25 | `SpellDefinitionSO` | Base damage for specific spell |
| `spell_level` | int | 1-5 | Spell Slot runtime state | Current spell level |
| `element_multiplier` | float | 0.6 / 1.0 / 1.5 | Damage Calculator affinity table | Target weakness/resistance context |
| `class_bonus` | float | 1.0 / 1.15 / 1.25 | Class rules | Active class multiplier |
| `meta_power_multiplier` | float | 1.0-1.60 | Meta progression data | Account progression multiplier |

**Expected output range**: approximately 3 to 150 for MVP baseline constraints.

**System rule**: Spell Effects calls this via Damage Calculator; no local reimplementation allowed.

### 2) Orbit Radius Scaling by Level

```text
orbit_radius = base_radius × (1 + 0.15 × (level - 1))
```

| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| `base_radius` | float | 0.5-6.0 units | `SpellDefinitionSO` | Radius at level 1 |
| `level` | int | 1-5 | Spell Slot runtime state | Spell level |
| `orbit_radius` | float | 0.5-9.6 units | Calculated | Final orbit lane radius |

**Expected output range**: min `base_radius`, max `base_radius × 1.60` at level 5.

### 3) Orbit Angular Motion

```text
angle_next = angle_current + orbit_speed_deg_per_sec × delta_time
```

| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| `angle_current` | float | 0-360+ | Runtime effect state | Current orbit angle |
| `orbit_speed_deg_per_sec` | float | 30-360 deg/s | `SpellDefinitionSO` | Rotation speed |
| `delta_time` | float | 0-0.1 s/frame | Update tick | Time step |

**Expected output range**: wraps modulo 360 for transform representation.

### 4) Orbit Contact Tick Gate

```text
can_tick_enemy = (current_time - enemy_last_tick_time) >= tick_interval
```

| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| `current_time` | float | 0-run_duration | Runtime clock | Current run time |
| `enemy_last_tick_time` | float | 0-run_duration | Per-enemy contact map | Last successful tick moment |
| `tick_interval` | float | 0.1-1.0 s | `SpellDefinitionSO` (default 0.3) | Minimum interval per enemy |

**Expected output range**: boolean true/false.

### 5) Projectile Lifetime

```text
projectile_lifetime = max_distance / projectile_speed
```

| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| `max_distance` | float | 1-30 units | `SpellDefinitionSO` | Maximum travel distance |
| `projectile_speed` | float | 1-40 units/s | `SpellDefinitionSO` | Projectile speed |
| `projectile_lifetime` | float | 0.025-30 s | Calculated | Lifetime timeout cap |

**Expected output range**: clamped to config minimum/maximum lifetime safety bounds if needed.

### 6) Projectile Position Update

```text
position_next = position_current + normalize(direction) × projectile_speed × delta_time
```

| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| `position_current` | Vector2 | arena bounds | Runtime projectile state | Current projectile position |
| `direction` | Vector2 | non-zero preferred | Activation context | Launch direction |
| `projectile_speed` | float | 1-40 units/s | `SpellDefinitionSO` | Current level-adjusted speed |
| `delta_time` | float | 0-0.1 s/frame | Update tick | Time step |

**Expected output range**: deterministic linear path until first hit or expiry.

### 7) Level-Scaled Cooldown Reference (Projectile Spells)

```text
cooldown_duration = base_cooldown × (1 - cooldown_reduction_per_level × (level - 1))
```

| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| `base_cooldown` | float | 0.1-20 s | `SpellDefinitionSO` | Level 1 cooldown |
| `cooldown_reduction_per_level` | float | 0.00-0.20 | Slot/Spell config | Level scaling factor |
| `level` | int | 1-5 | Spell runtime state | Current level |

**Expected output range**: clamped to minimum cooldown floor by Slot Manager rules.

## Edge Cases

| Scenario | Expected Behavior | Rationale |
|---|---|---|
| Object Pool exhausted during activation | Log warning, skip this spawn/activation, return failure for that cycle | Prevent stalls/crashes and preserve frame stability |
| Sword projectile exits arena bounds | Return projectile to pool immediately | Avoid orphaned entities and wasted updates |
| Orbit contact triggers multiple colliders same frame | Tick gate allows max one damage application per enemy per interval | Prevent accidental multi-hit burst from overlap jitter |
| Player moves rapidly/dashes | Orbit anchor follows player transform smoothly each frame; no detached orbit center | Preserve readability and fairness of collision space |
| Multiple spell effects active together | Maintain independent timers/state maps per spell instance | Avoid cross-spell timing contamination |
| Spell upgraded while effect already active | Apply upgraded profile on next activation snapshot (projectile volley / orbit reconfigure pass) | Deterministic progression without retroactive recalculation |
| No enemies in projectile assist range while player stationary | Use last known movement direction fallback | Keep deterministic firing direction and avoid null-target failures |
| Last movement direction is zero at run start | Use player facing direction from activation context | Guarantee valid initial launch vector |
| Projectile spawned with invalid speed <= 0 from bad config | Fail activation safely and log validation error | Prevent NaN/inf movement and broken lifetimes |
| Orbit entity returned unexpectedly (external cleanup) | Reconcile orbit slot on next update and request replacement if available | Keep continuous effects resilient |
| Enemy dies between collision check and damage apply | Revalidate target before damage call; skip if invalid | Prevent ghost hits and event noise |
| Run ends mid-volley or mid-orbit | Return all active spell entities to pool during cleanup state | No state leak into next run |
| Event Bus unavailable in teardown order | Continue gameplay cleanup without publishing non-critical events | Cleanup safety has higher priority than telemetry |
| Projectile reaches max distance and hits enemy same frame | Resolve collision first by deterministic priority, then return once | Consistent one-hit behavior across frames |
| Orbit tick interval configured too low | Clamp to safe minimum from config validation | Avoid performance spikes and unreadable DPS bursts |

## Dependencies

| System | Direction | Nature of Dependency |
|---|---|---|
| Spell Slot Manager | Spell Effects depends on Slot Manager activation timing | Receives activation requests and level context |
| Damage Calculator | Spell Effects depends on Damage Calculator | Uses centralized spell damage formula outputs |
| Object Pool | Spell Effects depends on Object Pool | Takes/returns projectile and orbit entities without runtime instantiation |
| Event Bus | Spell Effects depends on Event Bus | Publishes activation, hit, fail, and cleanup signals |
| Enemy Health & Damage | Enemy system depends on Spell Effects hit events | Applies health reduction and death logic from validated hits |
| SpellDefinitionSO data | Spell Effects depends on configuration assets | Reads all tunables (radius, speed, tick, damage base, cooldown metadata) |
| Player movement/facing context | Spell Effects depends on movement state feed | Uses position anchor and directional aim fallback |
| HUD/VFX/Audio presentation | Presentation depends on Spell Effects events | Displays feedback without owning gameplay outcomes |

## Tuning Knobs

| Parameter | Current Value | Safe Range | Effect of Increase | Effect of Decrease |
|---|---|---|---|---|
| `AlevHalkasi.baseDamage` | data-driven | 5-25 | Stronger orbit attrition | Weaker close-range pressure |
| `AlevHalkasi.baseRadius` | data-driven | 0.5-6.0 | Wider control zone | Tighter close defense |
| `AlevHalkasi.orbitSpeed` | data-driven | 30-360 deg/s | More contact opportunities, harder readability at extreme | Slower, easier to read but less uptime |
| `AlevHalkasi.tickInterval` | 0.3s default | 0.1-1.0s | Higher DPS frequency and CPU load | Lower DPS frequency, gentler pacing |
| `AlevHalkasi.ringCountByLevel` | 1/1/2/2/3 | fixed MVP profile | More map coverage and contact lanes | Less coverage complexity |
| `KilicFirtinasi.baseDamage` | 12 | 5-25 | Higher burst lethality | Lower impact per volley |
| `KilicFirtinasi.baseCooldown` | data-driven | 0.1-20s | Slower fire cadence | Faster cadence and pressure |
| `KilicFirtinasi.projectileSpeed` | data-driven | 1-40 units/s | Faster hit confirmation, less dodge window | Slower travel, clearer telegraph |
| `KilicFirtinasi.maxDistance` | data-driven | 1-30 units | Longer lane reach | Shorter effective range |
| `KilicFirtinasi.spreadAngleByLevel` | profile-driven | 0-90 deg | Better multi-target coverage | More focused single-lane pressure |
| `KilicFirtinasi.swordCountByLevel` | 1/1/2/3/4 | fixed MVP profile | Larger volley density | Lower projectile saturation |
| `KayaKalkani.baseDamage` | data-driven | 5-25 | Stronger contact punishment | Lower defensive offense |
| `KayaKalkani.baseRadius` | data-driven | 0.5-6.0 | Wider shield path around player | Tighter shield ring |
| `KayaKalkani.orbitSpeed` | data-driven | 20-300 deg/s | More frequent contact windows | Slower defensive sweep |
| `KayaKalkani.tickInterval` | 0.3s default | 0.1-1.0s | Higher damage cadence | Lower cadence, heavier-feel hits |
| `KayaKalkani.rockCountByLevel` | 2/2/3/4/5 | fixed MVP profile | Greater angular shield coverage | More vulnerable approach gaps |
| `OrbitRadiusLevelScale` | 0.15 per level | 0.05-0.30 | Faster radius growth across levels | Flatter orbit growth |
| `PoolOverflowPolicy` | ReturnNull + warning | ExpandOrNull / ReturnNull | More resilience if expand allowed | More strict spawn skipping, tighter perf control |

## Acceptance Criteria
- [ ] Spell Effects defines and uses an abstract `SpellEffectBase` contract for all concrete effects.
- [ ] Activation inputs always include spell level, player position, and player facing direction.
- [ ] `Alev Halkası`, `Kılıç Fırtınası`, and `Kaya Kalkanı` are the only spell effects in MVP-1 scope for this system.
- [ ] Orbit effects (`Alev Halkası`, `Kaya Kalkanı`) run as continuous active effects and do not implement local cooldown loops.
- [ ] Projectile effect (`Kılıç Fırtınası`) executes on periodic activation requests from Spell Slot Manager.
- [ ] All spawned spell entities are requested from Object Pool and returned to pool on hit/expiry/cleanup.
- [ ] Pool exhaustion results in warning + graceful skip, never runtime crash.
- [ ] All spell damage values are requested through Damage Calculator Spell Damage formula; no local damage math exists in Spell Effects.
- [ ] Orbit radius scales with formula `base_radius × (1 + 0.15 × (level - 1))`.
- [ ] Orbit contact damage uses configurable per-enemy tick gate, defaulting to 0.3s.
- [ ] Alev Halkası level scaling matches profile: 1 ring (L1), 1 larger ring (L2), 2 rings (L3), 2 larger (L4), 3 rings (L5).
- [ ] Kılıç Fırtınası level scaling matches profile: 1/1/2/3/4 swords with increasing spread and speed.
- [ ] Kılıç Fırtınası uses base damage 12 from spell data and respects 5-25 allowed range rules.
- [ ] Sword projectiles travel in straight line, hit first enemy only, then return to pool.
- [ ] Projectile lifetime uses `max_distance / projectile_speed` and returns to pool on timeout.
- [ ] Projectiles leaving arena bounds are returned to pool immediately.
- [ ] Kaya Kalkanı level scaling matches profile: 2/2/3/4/5 rocks with larger size/coverage over levels.
- [ ] Kaya Kalkanı MVP-1 does not implement projectile blocking/absorbing behavior.
- [ ] No UnityEvents are used in runtime spell effect execution.
- [ ] All tunables are data-driven through `SpellDefinitionSO`/config assets with no hardcoded gameplay values.
- [ ] System supports deterministic cleanup: on run end, all active spell entities are returned to pool.
