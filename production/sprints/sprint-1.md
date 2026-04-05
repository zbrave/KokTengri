# Sprint 1 — 2026-04-04 to 2026-05-04

## Sprint Goal
Core loop playable in editor — move, enemies spawn, kill enemies, collect XP, level-up, craft a spell, and 3 initial spell effects.

## Capacity
- Total days: 30 (1 month sprint)
- Buffer (20%): 6 days
- Available: 24 days

## Tasks

### Must Have (Critical Path)
| ID | Task | Agent/Owner | Est. Days | Dependencies | Acceptance Criteria |
|----|------|-------------|-----------|-------------|-------------------|
| S1-01 | Unity Project Setup | User | 0.5 | — | Unity 2022.3 LTS project created with URP 2D, Input System, and TextMeshPro installed. Folder structure matches Phase 1 plan. |
| S1-02 | Foundation: EventBus & Game Events | engine-programmer | 0.5 | S1-01 | EventBus.cs implemented with generic Subscribe/Publish. All 11 core game events defined in GameEvents.cs. Unit tests pass. |
| S1-03 | Foundation: ObjectPool System | engine-programmer | 0.5 | S1-02 | GenericObjectPool.cs implemented with IPooledObject interface. Supports warming and expansion. Unit tests pass. |
| S1-04 | Foundation: Input System | gameplay-programmer | 0.5 | S1-01 | PlayerInputActions asset created with Move (Vector2) action. Supports Gamepad, WASD, and On-Screen Stick. |
| S1-05 | Core: Run Manager | gameplay-programmer | 0.5 | S1-02 | RunManager.cs tracks elapsed time, publishes RunTimerTickEvent, and handles start/death/end states. |
| S1-06 | Core: Wave Manager | gameplay-programmer | 0.5 | S1-05 | WaveManager.cs progresses through time-based waves. Publishes spawn requests to EventBus. |
| S1-07 | Core: Difficulty Scaling | game-designer | 0.5 | S1-05 | DifficultyScaling.cs calculates HP/Damage multipliers based on elapsed minutes using formulas from GDD. |
| S1-08 | Core: Damage Calculator | game-designer | 0.5 | — | DamageCalculator.cs (pure math) calculates final damage including level, element multipliers, and resistance. |
| S1-09 | Feature: Player Movement | gameplay-programmer | 0.5 | S1-04 | PlayerController.cs moves player prefab using New Input System. Speed driven by PlayerConfigSO. |
| S1-10 | Feature: Element Inventory | game-designer | 1.5 | S1-02 | ElementInventory.cs manages max 3 slots. Detects matches for all 15 recipes. Unit tests pass. |
| S1-11 | Feature: Spell Crafting | game-designer | 3.0 | S1-10 | SpellCrafter.cs combines elements into spells or upgrades existing ones. Handles full slots/inventory edge cases. |
| S1-12 | Feature: Spell Slot Manager | game-designer | 0.5 | S1-11 | SpellSlotManager.cs tracks up to 6 active spells and their levels. |
| S1-13 | Feature: Enemy Spawner | gameplay-programmer | 0.5 | S1-03, S1-06 | EnemySpawner.cs spawns enemies from pool in a ring around the player. Respects WaveManager timing. |
| S1-14 | Feature: Enemy Health & Damage | gameplay-programmer | 1.5 | S1-03, S1-08 | EnemyHealth.cs handles HP, damage taken (with element logic), and death (XP drop). EnemyBase handles contact damage. |
| S1-15 | Feature: Enemy Behaviors (Kara Kurt + Yek Uşağı) | ai-programmer | 1.5 | S1-14, S1-09 | Kara Kurt (fast chase) and Yek Uşağı (slow tank) behaviors implemented. Move toward player. |
| S1-16 | Feature: XP & Leveling | game-designer | 1.5 | S1-14 | XPCollector.cs handles gem collection and level-up triggers. LevelUpConfigSO defines XP curve. |
| S1-17 | Feature: Spell Effects (3 initial) | gameplay-programmer | 3.0 | S1-12, S1-08 | Alev Halkası (Orbit), Kılıç Fırtınası (Projectile), Kaya Kalkanı (Orbit) implemented with SpellEffectBase. |
| S1-18 | Presentation: HUD | ui-programmer | 1.5 | S1-05, S1-09, S1-12 | HUD displays HP bar, active spell icons/levels, element inventory, and run timer. |
| S1-19 | Presentation: Level-Up Selection | ui-programmer | 1.5 | S1-16, S1-10 | Level-Up popup pauses game, offers 3 random element choices, and updates inventory on selection. |

### Should Have
| ID | Task | Agent/Owner | Est. Days | Dependencies | Acceptance Criteria |
|----|------|-------------|-----------|-------------|-------------------|
| S1-20 | Basic VFX Placeholders | technical-artist | 1.0 | S1-17 | Simple particle effects or color-coded sprites for the 3 initial spells and enemy death. |
| S1-21 | Mobile Joystick Polish | ux-designer | 0.5 | S1-04 | Virtual joystick is responsive, has deadzone, and visual feedback on touch. |
| S1-22 | Unit Test Coverage (Core) | qa-tester | 1.0 | — | 90%+ coverage for EventBus, ObjectPool, SpellCrafter, and Damage formulas. |

### Nice to Have
| ID | Task | Agent/Owner | Est. Days | Dependencies | Acceptance Criteria |
|----|------|-------------|-----------|-------------|-------------------|
| S1-23 | Early Mobile Profiling | performance-analyst | 1.0 | S1-13, S1-17 | Run prototype on mid-range Android/iOS. Identify bottlenecks in spawner or spell effects. |
| S1-24 | 4th Spell Effect (Buz Rüzgarı) | gameplay-programmer | 1.0 | S1-17 | Implement Buz Rüzgarı (Cone pattern) as a bonus spell for testing. |

## Risks
| Risk | Probability | Impact | Mitigation |
|------|------------|--------|------------|
| Performance: 300+ enemies at 60 FPS | High | High | Object pool everything (enemies, projectiles, XP gems). Profile early in Unity Editor and on device. |
| Spell crafting complexity (15 recipes) | Medium | High | Extensive unit tests for SpellCrafter and ElementInventory. GDD covers all edge cases. |
| Touch joystick feel | Medium | Medium | Use Unity New Input System's On-Screen Stick. Early playtesting on mobile. |
| Solo developer timeline | High | Medium | Strict scope discipline. Use buffer days for unplanned bugs. Prioritize Must Haves. |

## Dependencies on External Factors
- Unity project must be created by user before implementation starts (Task S1-01)
- Pixel art assets needed for spell effects (placeholder sprites acceptable for Sprint 1)

## Definition of Done for this Sprint
- [ ] All Must Have tasks completed
- [ ] Core loop playable: move → enemies spawn → kill → XP → level-up → element pick → spell craft
- [ ] 60 FPS with 50+ enemies on mid-range mobile (Unity Editor profiling)
- [ ] No hardcoded gameplay values — all from ScriptableObjects
- [ ] All code follows naming conventions in technical-preferences.md
- [ ] All unit tests pass in Unity Test Runner
