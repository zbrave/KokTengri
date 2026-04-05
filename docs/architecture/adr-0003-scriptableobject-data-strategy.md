# ADR-0003: ScriptableObject Data Strategy

## Status
Proposed

## Date
2026-04-04

## Context

### Problem Statement
Kök Tengri is a highly data-driven survivor-like game. Balance values for 15+ spells, 6+ enemy types, difficulty scaling curves, and player progression must be easily tunable by designers without touching code. Hardcoding these values or using fragile text formats (like raw JSON) would slow down iteration and increase the risk of runtime errors.

### Constraints
- **Engine**: Unity 2022.3 LTS.
- **Workflow**: Designers must be able to edit values directly in the Unity Inspector.
- **Performance**: Must have zero runtime parsing overhead during active gameplay.
- **Platform**: Mobile (requires efficient memory usage).

### Requirements
- **Inspector Friendly**: Visual editing with sliders, curves, and object references.
- **Type Safety**: References between data objects (e.g., a Spell referencing its VFX prefab) must be type-safe.
- **Persistence**: Data must be stored as assets in the project.
- **Decoupling**: Gameplay logic should consume data objects, not own the values.

## Decision

We will use a **ScriptableObject-based Data Architecture** for all gameplay configuration and balance values.

1.  **Definition SOs**: Every gameplay entity will have a corresponding ScriptableObject type (e.g., `SpellDefinitionSO`, `EnemyDefinitionSO`, `HeroDefinitionSO`).
2.  **Config SOs**: Global settings (Difficulty, Wave Timing, XP Curves) will be stored in singleton-like Config SOs (e.g., `DifficultyConfigSO`, `WaveConfigSO`).
3.  **Logic-Data Separation**: MonoBehaviours (like `EnemyHealth`) will hold a reference to their `DefinitionSO` and query it for stats, rather than storing the stats themselves.
4.  **Event Integration**: When events are published via the `EventBus`, they will often include a reference to the relevant ScriptableObject (e.g., `SpellCraftedEvent` carries the `SpellDefinitionSO`).
5.  **Formula Centralization**: SOs will include helper methods for their own scaling formulas (e.g., `EnemyDefinitionSO.GetHP(float elapsedMinutes)`) to ensure consistency.

### Architecture Diagram

```text
[ Unity Inspector ] --(Edit)--> [ ScriptableObject Asset ]
                                        |
                                        | (Reference)
                                        v
[ Gameplay System ] <---------- [ Runtime Instance ]
      |                                 |
      | (Query Stats)                   | (Provide Data)
      v                                 v
[ Combat Logic ] <------------ [ Formula Methods ]
```

### Key Interfaces / Classes

```csharp
// Example Definition
[CreateAssetMenu]
public class SpellDefinitionSO : ScriptableObject
{
    public string spellId;
    public float baseDamage;
    public SpellPattern pattern;
    public GameObject prefab;
    
    public float CalculateDamage(int level, ...) { ... }
}

// Example Config
[CreateAssetMenu]
public class DifficultyConfigSO : ScriptableObject
{
    public float hpScalePerMinute;
    public float spawnRateIncrease;
}
```

## Alternatives Considered

### Alternative 1: JSON / XML Config Files
- **Description**: Store balance data in external text files.
- **Pros**: Can be edited outside Unity, supports remote config easily.
- **Cons**: No native Inspector support, requires runtime parsing (CPU/GC cost), no type-safe object references (prefabs, other SOs).
- **Rejection Reason**: The lack of Inspector integration and type-safe referencing significantly slows down designer iteration compared to ScriptableObjects.

### Alternative 2: Static Classes / Constants
- **Description**: Hardcode values in C# static classes.
- **Pros**: Fastest possible access, zero memory overhead.
- **Cons**: Requires recompile for every change, no Inspector visibility, impossible for designers to tune.
- **Rejection Reason**: Violates the core requirement of being designer-tunable and data-driven.

### Alternative 3: Database (SQLite)
- **Description**: Store all game data in a local database.
- **Pros**: Great for massive amounts of data (RPGs).
- **Cons**: Overkill for a survivor-like, complex to set up, no Inspector integration.
- **Rejection Reason**: Unnecessary complexity for the current project scale.

## Consequences

### Positive
- **Fast Iteration**: Designers can tune the game in real-time during Play Mode.
- **Type Safety**: Unity handles references between assets (prefabs, sounds, other SOs) natively.
- **Performance**: Accessing a field on an SO is as fast as accessing a field on a class; no parsing required.
- **Memory Efficiency**: Only one instance of each SO exists in memory, regardless of how many GameObjects reference it.

### Negative
- **Unity Dependency**: Data is locked into Unity's `.asset` format, making it harder to edit with external tools.
- **Merge Conflicts**: Binary or YAML-based `.asset` files can be tricky to merge in Git if multiple people edit the same file.

### Risks
- **Accidental Overwrites**: Changes made in Play Mode persist to the asset.
- **Mitigation**: Use a "Save/Load" pattern for runtime-modified data, but for *static* balance data, this persistence is actually a feature.

## Performance Implications
- **CPU**: Zero parsing cost. Access is O(1).
- **Memory**: Extremely efficient; shared instances.
- **Load Time**: Fast; handled by Unity's native asset loading.
- **Network**: N/A (Local assets).

## Migration Plan
All core systems (Spell Crafting, Enemy Spawner, Difficulty Scaling) are being implemented with ScriptableObject references from the start of Phase 1.

## Validation Criteria
- **Designer Workflow**: Verify that changing a value in `EnemyDefinitionSO` during Play Mode immediately affects newly spawned enemies.
- **Type Safety**: Ensure no "magic strings" are used for data lookup; use SO references or Enums.
- **Consistency**: Verify that `DamageCalculator` and SO helper methods return identical results for the same inputs.

## Related Decisions
- [ADR-0001: EventBus Pattern](adr-0001-eventbus-pattern.md) (Events carry SO references)
- [ADR-0002: ObjectPool Strategy](adr-0002-objectpool-strategy.md) (Pool settings stored in SOs)
- [design/gdd/difficulty-scaling.md](../design/gdd/difficulty-scaling.md)
- [design/gdd/spell-crafting.md](../design/gdd/spell-crafting.md)
