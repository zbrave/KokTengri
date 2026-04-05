# Technical Preferences

<!-- Populated by /setup-engine. Updated as the user makes decisions throughout development. -->
<!-- All agents reference this file for project-specific standards and conventions. -->

## Engine & Language

- **Engine**: Unity 2022.3 LTS (URP 2D)
- **Language**: C#
- **Rendering**: URP 2D (pixel art + lighting effects)
- **Physics**: Unity 2D Physics (Box2D)

## Naming Conventions

- **Classes**: PascalCase (e.g., `PlayerController`)
- **Public fields/properties**: PascalCase (e.g., `MoveSpeed`)
- **Private fields**: _camelCase (e.g., `_moveSpeed`)
- **Methods**: PascalCase (e.g., `TakeDamage()`)
- **Files**: PascalCase matching class (e.g., `PlayerController.cs`)
- **Scenes/Prefabs**: PascalCase (e.g., `PlayerController.prefab`)
- **Constants**: UPPER_SNAKE_CASE or PascalCase (e.g., `MAX_HEALTH` or `MaxHealth`)
- **ScriptableObjects**: PascalCase with SO suffix (e.g., `SpellDefinitionSO`)

## Performance Budgets

- **Target Framerate**: 60 FPS (mid-range mobile)
- **Frame Budget**: 16.6ms
- **Draw Calls**: Keep under 100 for 2D scenes
- **Memory Ceiling**: < 512 MB
- **Max enemies on screen**: 300+
- **APK size**: < 150 MB
- **Battery**: 30min run = max 10% drain

## Testing

- **Framework**: Unity Test Framework (NUnit)
- **Minimum Coverage**: Core systems (EventBus, ObjectPool, SpellCrafter, WaveManager)
- **Required Tests**: Balance formulas, spell crafting logic, XP/leveling, economy calculations

## Forbidden Patterns

- No MonoBehaviour FindObjectOfType in hot paths — use injection or references
- No coroutines for gameplay-critical timing — use Update + timers
- No hardcoded gameplay values — all values in ScriptableObjects
- No direct UI references from gameplay code — use EventBus

## Allowed Libraries / Addons

- Unity New Input System (joystick + touch)
- TextMeshPro (UI text)
- Unity IAP (post-MVP)
- Unity Addressables (post-MVP, for asset management)
- Aseprite → Unity pipeline (pixel art)

## Architecture Decisions Log

- EventBus pattern for decoupled communication
- Object Pooling for enemies/projectiles (avoid GC spikes)
- ScriptableObjects for all balance values (data-driven design)
- JSON + encryption for save system (anti-cheat)

<!-- Quick reference linking to full ADRs in docs/architecture/ -->
- [No ADRs yet — use /architecture-decision to create one]
