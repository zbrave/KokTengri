# Systems Index: Kök Tengri

> **Status**: Approved
> **Created**: 2026-04-04
> **Last Updated**: 2026-04-04
> **Source Concept**: design/gdd/game-concept.md
> **Game Pillars**: design/gdd/game-pillars.md

---

## Overview

Kök Tengri is a Turkic mythology pixel art survivor-like mobile game. The core loop is: move → survive waves → collect elements → craft spells → fight bosses → earn currency → upgrade permanently → repeat. The game's mechanical scope covers 31 systems across 5 layers: Foundation infrastructure, Core Gameplay (movement, combat, crafting), Presentation (HUD, VFX, audio), Meta-Progression (permanent upgrades, economy, heroes), and Polish (IAP, collection). The primary differentiator is the element crafting system — 5 Turkic cosmology elements combining into 15 spells during runs.

---

## Systems Enumeration

| # | System Name | Category | Priority | Status | Design Doc | Depends On |
|---|-------------|----------|----------|--------|------------|------------|
| 1 | Player Movement | Core Gameplay | MVP-1 | Designed | design/gdd/player-movement.md | Input System, Event Bus |
| 2 | Element Inventory | Core Gameplay | MVP-1 | Designed | design/gdd/element-inventory.md | Event Bus |
| 3 | Spell Crafting | Core Gameplay | MVP-1 | Designed | design/gdd/spell-crafting.md | Element Inventory, Event Bus |
| 4 | Spell Slot Manager | Core Gameplay | MVP-1 | Designed | design/gdd/spell-slot-manager.md | Spell Crafting |
| 5 | Spell Effects | Core Gameplay | MVP-1 | Designed | design/gdd/spell-effects.md | Spell Slot Manager, Damage Calculator, Object Pool |
| 6 | XP & Leveling | Core Gameplay | MVP-1 | Designed | design/gdd/xp-leveling.md | Enemy Health (death → XP), Object Pool |
| 7 | Level-Up Selection | UI | MVP-1 | Designed | design/gdd/level-up-selection.md | XP & Leveling, Element Inventory, Spell Crafting |
| 8 | Enemy Spawner | Core Gameplay | MVP-1 | Designed | design/gdd/enemy-spawner.md | Object Pool, Wave Manager, Difficulty Scaling |
| 9 | Enemy Behaviors | Core Gameplay | MVP-1 | Designed | design/gdd/enemy-behaviors.md | Enemy Health, Player Movement |
| 10 | Enemy Health & Damage | Core Gameplay | MVP-1 | Designed | design/gdd/enemy-health-damage.md | Object Pool, Damage Calculator, Event Bus |
| 11 | Boss System | Core Gameplay | MVP-2 | Not Started | — | Enemy Spawner, Wave Manager, Event Bus |
| 12 | Wave Manager | Core | MVP-1 | Designed | design/gdd/wave-manager.md | Event Bus, Run Manager |
| 13 | Run Manager | Core | MVP-1 | Designed | design/gdd/run-manager.md | Event Bus |
| 14 | Hero System | Core Gameplay | MVP-3 | Not Started | — | Save System, Event Bus |
| 15 | Class System | Core Gameplay | MVP-3 | Not Started | — | Save System, Event Bus |
| 16 | Meta-Progression | Meta | MVP-3 | Not Started | — | Save System |
| 17 | Economy | Meta | MVP-3 | Not Started | — | Save System, Event Bus |
| 18 | Destan Collection | Meta | Post-MVP | Not Started | — | Boss System, Save System |
| 19 | IAP Store | Meta | Post-MVP | Not Started | — | Economy, Save System |
| 20 | Save System | Core | MVP-3 | Not Started | — | Event Bus |
| 21 | Event Bus | Foundation | MVP-1 | Designed | design/gdd/event-bus.md | — |
| 22 | Object Pool | Foundation | MVP-1 | Designed | design/gdd/object-pool.md | — |
| 23 | Damage Calculator | Core Gameplay | MVP-1 | Designed | design/gdd/damage-calculator.md | — (pure math utility) |
| 24 | HUD | UI | MVP-1 | Designed | design/gdd/hud.md | Player Movement, Spell Slot Manager, Element Inventory, XP |
| 25 | Run End Summary | UI | MVP-3 | Not Started | — | Run Manager, Economy, XP & Leveling |
| 26 | Hero/Class Selection | UI | MVP-3 | Not Started | — | Hero System, Class System, Run Manager |
| 27 | VFX System | Presentation | MVP-2 | Not Started | — | Spell Effects, Event Bus |
| 28 | Audio System | Presentation | MVP-2 | Not Started | — | Event Bus |
| 29 | Element Drop System | Core Gameplay | MVP-2 | Not Started | — | Enemy Health (death), Event Bus |
| 30 | Difficulty Scaling | Core | MVP-1 | Designed | design/gdd/difficulty-scaling.md | Run Manager (time) |
| 31 | Input System | Foundation | MVP-1 | Designed | design/gdd/input-system.md | — |

---

## Categories

| Category | Description | Systems in This Game |
|----------|-------------|---------------------|
| **Foundation** | Infrastructure systems everything depends on | Event Bus, Object Pool, Input System |
| **Core** | Run lifecycle, configuration, and utility systems | Run Manager, Wave Manager, Difficulty Scaling, Save System |
| **Core Gameplay** | Systems that make the game fun — combat, crafting, movement | Player Movement, Element Inventory, Spell Crafting, Spell Slot Manager, Spell Effects, XP & Leveling, Enemy Spawner, Enemy Behaviors, Enemy Health & Damage, Boss System, Damage Calculator, Element Drop System, Hero System, Class System |
| **UI** | Player-facing information displays and interaction | HUD, Level-Up Selection, Run End Summary, Hero/Class Selection |
| **Presentation** | Visual and audio feedback | VFX System, Audio System |
| **Meta** | Systems outside the core run loop | Meta-Progression, Economy, Destan Collection, IAP Store |

---

## Priority Tiers

| Tier | Definition | Target Milestone | System Count |
|------|------------|------------------|--------------|
| **MVP-1** (Month 1) | Core loop playable in editor: move, enemies, crafting, 3 spells | First playable prototype | 17 systems |
| **MVP-2** (Month 2) | Full combat: all spells, 4 enemy types, 3 bosses, VFX/audio | Combat complete | 6 systems |
| **MVP-3** (Month 3) | Meta loop: heroes, classes, progression, economy, save, menus | Full MVP | 6 systems |
| **Post-MVP** | Polish: remaining content, IAP, collection | Post-launch update | 2 systems |

---

## Dependency Map

### Foundation Layer (no dependencies)

1. **Event Bus** — Decoupled pub/sub communication; every other system uses it
2. **Object Pool** — Reusable entity pools; needed by enemies, spells, XP gems, VFX
3. **Input System** — Virtual joystick + touch; drives player movement and UI

### Core Layer (depends on foundation)

1. **Run Manager** — depends on: Event Bus
2. **Save System** — depends on: Event Bus
3. **Wave Manager** — depends on: Event Bus, Run Manager
4. **Difficulty Scaling** — depends on: Run Manager (time tracking)
5. **Damage Calculator** — depends on: — (pure math utility, no external deps)

### Feature Layer — Combat (depends on core)

1. **Player Movement** — depends on: Input System, Event Bus
2. **Element Inventory** — depends on: Event Bus
3. **Spell Crafting** — depends on: Element Inventory, Event Bus
4. **Spell Slot Manager** — depends on: Spell Crafting
5. **Enemy Spawner** — depends on: Object Pool, Wave Manager, Difficulty Scaling
6. **Enemy Health & Damage** — depends on: Object Pool, Damage Calculator, Event Bus
7. **Enemy Behaviors** — depends on: Enemy Health, Player Movement (chase target)
8. **XP & Leveling** — depends on: Enemy Health (death → XP), Object Pool
9. **Element Drop System** — depends on: Enemy Health (death event), Event Bus

### Feature Layer — Spells & Bosses (depends on combat)

1. **Spell Effects** — depends on: Spell Slot Manager, Damage Calculator, Object Pool, Event Bus
2. **Boss System** — depends on: Enemy Spawner, Wave Manager (timer), Event Bus
3. **Hero System** — depends on: Save System, Event Bus
4. **Class System** — depends on: Save System, Event Bus

### Presentation Layer (depends on features)

1. **HUD** — depends on: Player Movement, Spell Slot Manager, Element Inventory, XP & Leveling
2. **Level-Up Selection** — depends on: XP & Leveling, Element Inventory, Spell Crafting
3. **VFX System** — depends on: Spell Effects, Event Bus
4. **Audio System** — depends on: Event Bus
5. **Run End Summary** — depends on: Run Manager, Economy, XP & Leveling
6. **Hero/Class Selection** — depends on: Hero System, Class System, Run Manager

### Polish Layer (depends on everything)

1. **Meta-Progression** — depends on: Save System
2. **Economy** — depends on: Save System, Event Bus
3. **Destan Collection** — depends on: Boss System, Save System
4. **IAP Store** — depends on: Economy, Save System

---

## Recommended Design Order

| Order | System | Priority | Layer | Agent(s) | Est. Effort |
|-------|--------|----------|-------|----------|-------------|
| 1 | Event Bus | MVP-1 | Foundation | engine-programmer | S |
| 2 | Object Pool | MVP-1 | Foundation | engine-programmer | S |
| 3 | Input System | MVP-1 | Foundation | gameplay-programmer | S |
| 4 | Run Manager | MVP-1 | Core | gameplay-programmer | S |
| 5 | Wave Manager | MVP-1 | Core | gameplay-programmer | S |
| 6 | Difficulty Scaling | MVP-1 | Core | game-designer | S |
| 7 | Damage Calculator | MVP-1 | Core | game-designer | S |
| 8 | Player Movement | MVP-1 | Feature | gameplay-programmer | S |
| 9 | Element Inventory | MVP-1 | Feature | game-designer | M |
| 10 | Spell Crafting | MVP-1 | Feature | game-designer | L |
| 11 | Spell Slot Manager | MVP-1 | Feature | game-designer | S |
| 12 | Enemy Spawner | MVP-1 | Feature | gameplay-programmer | S |
| 13 | Enemy Health & Damage | MVP-1 | Feature | gameplay-programmer | M |
| 14 | Enemy Behaviors (Kara Kurt, Yek Uşağı) | MVP-1 | Feature | ai-programmer | M |
| 15 | XP & Leveling | MVP-1 | Feature | game-designer | M |
| 16 | Spell Effects (3 initial) | MVP-1 | Feature | gameplay-programmer | L |
| 17 | HUD | MVP-1 | Presentation | ui-programmer | M |
| 18 | Level-Up Selection | MVP-1 | Presentation | ui-programmer | M |
| 19 | Enemy Behaviors (Albastı, Çor) | MVP-2 | Feature | ai-programmer | M |
| 20 | Boss System (3 bosses) | MVP-2 | Feature | ai-programmer, game-designer | L |
| 21 | Spell Effects (remaining 12) | MVP-2 | Feature | gameplay-programmer | L |
| 22 | Element Drop System | MVP-2 | Feature | gameplay-programmer | S |
| 23 | VFX System | MVP-2 | Presentation | technical-artist | M |
| 24 | Audio System | MVP-2 | Presentation | sound-designer | M |
| 25 | Save System | MVP-3 | Core | engine-programmer | M |
| 26 | Hero System | MVP-3 | Feature | game-designer | M |
| 27 | Class System | MVP-3 | Feature | game-designer | M |
| 28 | Meta-Progression | MVP-3 | Polish | game-designer, economy-designer | L |
| 29 | Economy | MVP-3 | Polish | economy-designer | M |
| 30 | Run End Summary | MVP-3 | Presentation | ui-programmer | S |
| 31 | Hero/Class Selection | MVP-3 | Presentation | ui-programmer | M |
| 32 | Destan Collection | Post-MVP | Polish | game-designer, narrative-director | S |
| 33 | IAP Store | Post-MVP | Polish | economy-designer | M |
| 34 | Enemy Behaviors (Demirci Cin, Göl Aynası) | Post-MVP | Feature | ai-programmer | M |
| 35 | Boss System (Boz Ejderha, Erlik Han) | Post-MVP | Feature | ai-programmer, game-designer | L |

Effort estimates: S = 1 session, M = 2-3 sessions, L = 4+ sessions.

---

## Circular Dependencies

- **None found.** All dependencies flow in a single direction: Foundation → Core → Feature → Presentation → Polish.

---

## High-Risk Systems

| System | Risk Type | Risk Description | Mitigation |
|--------|-----------|-----------------|------------|
| **Spell Crafting** | Design | 15 recipes with element inventory management + upgrade logic is complex. Edge cases around full inventory, full spell slots, and re-rolls are easy to get wrong. | Design GDD first with all edge cases, prototype early, unit test extensively |
| **Spell Effects** | Technical/Design | 15 distinct spell behaviors must all feel good, perform well on mobile (300+ enemies), and be visually distinct. Orbit + projectile + AoE patterns need object pooling. | Prototype 3 spells in Month 1 to validate performance. Use abstract SpellEffectBase for consistent pattern |
| **Boss System** | Design | 5 bosses with unique mechanics. Phase systems, element-neutral design, difficulty balance. Bosses must be beatable with any build. | Design boss mechanics to test specific skills (positioning, timing, build). Start with Tepegöz (simplest) |
| **Performance (300+ enemies)** | Technical | Mobile target at 60 FPS with 300+ enemies, 6 spell effects, projectiles, XP gems. GC spikes from object creation will kill framerate. | Object pool everything. Profile early (Month 1). Set performance budget: 16.6ms frame time |
| **Economy Balance** | Design | F2P economy must be fair (no P2W) while generating revenue. Gold curve, soul stone rarity, IAP pricing, and unlock times must all be tuned. | Design with formulas first (spec already has them). Simulate in spreadsheet. Monitor analytics post-launch |

---

## Progress Tracker

| Metric | Count |
|--------|-------|
| Total systems identified | 31 |
| Design docs started | 11 |
| Design docs reviewed | 0 |
| Design docs approved | 0 |
| MVP-1 systems designed | 11 / 17 |
| MVP-2 systems designed | 0 / 6 |
| MVP-3 systems designed | 0 / 6 |
| Post-MVP systems designed | 0 / 2 |

---

## Next Steps

- [x] Review and approve this systems enumeration
- [x] Design MVP-1 foundation systems (Event Bus, Object Pool, Input System) → ✅ Done
- [x] Design Spell Crafting GDD (highest-risk MVP system) → ✅ Done
- [x] Design Element Inventory GDD → ✅ Done
- [x] Design MVP-1 Core systems (Run Manager, Wave Manager, Difficulty Scaling, Damage Calculator) → ✅ Done
- [x] Design MVP-1 Feature systems (Player Movement, Spell Slot Manager, Enemy Spawner, Enemy Health & Damage, Enemy Behaviors, XP & Leveling, Spell Effects) → ✅ Done
- [x] Design MVP-1 Presentation systems (HUD, Level-Up Selection) → ✅ Done
- [ ] Run `/design-review` on each completed GDD
- [ ] Run `/gate-check pre-production` when MVP-1 systems are designed
- [ ] Prototype Spell Effects early (`/prototype spell-effects`) to validate performance
- [ ] Plan first implementation sprint with `/sprint-plan new`

---

*This document lives in `design/gdd/systems-index.md` and is the master reference for all
system design work. Update the Progress Tracker as GDDs are completed.*
