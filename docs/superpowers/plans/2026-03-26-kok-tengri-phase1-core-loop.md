# Kök Tengri — Phase 1: Core Loop Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build the playable core loop — player movement, enemy waves, element collection, spell crafting, basic combat — so a run can be played end-to-end in Unity Editor.

**Architecture:** Unity 2D (URP) with C#. Data-driven design via ScriptableObjects for all balance values. EventBus for decoupled communication. Object pooling for enemies/projectiles. Unity New Input System for touch/joystick.

**Tech Stack:** Unity 2022.3 LTS, URP 2D, C#, Unity Test Framework (NUnit), New Input System, TextMeshPro

**Spec:** `docs/superpowers/specs/2026-03-26-kok-tengri-game-design.md`

**Scope:** Month 1 of 4-month MVP. Phases 2-4 will be separate plans.

**Phase 1 delivers:**
- Player movement with touch joystick
- 2 enemy types (Kara Kurt, Yek Uşağı) with object pooling
- Wave spawner with time-based difficulty scaling
- Element inventory (max 3) + spell crafting (all 15 recipes)
- 3 working spell effects (Alev Halkası, Kılıç Fırtınası, Kaya Kalkanı)
- XP system + level-up element selection popup
- Basic HUD (HP bar, spell slots, timer)
- Run manager (start → play → death → summary)
- All balance values in ScriptableObjects

---

## File Structure

```
Assets/
├── Scripts/
│   ├── Core/
│   │   ├── EventBus.cs                    # Pub/sub event system
│   │   ├── GameEvents.cs                  # All event type definitions
│   │   ├── GenericObjectPool.cs           # Reusable object pool
│   │   └── RunManager.cs                  # Run lifecycle (start, timer, end)
│   ├── Data/
│   │   ├── ElementType.cs                 # Element enum (Od, Sub, Yer, Yel, Temur)
│   │   ├── SpellRecipe.cs                 # Element pair → spell mapping
│   │   ├── SpellDefinitionSO.cs           # ScriptableObject: spell stats
│   │   ├── EnemyDefinitionSO.cs           # ScriptableObject: enemy stats
│   │   ├── WaveConfigSO.cs               # ScriptableObject: wave timing/scaling
│   │   ├── PlayerConfigSO.cs             # ScriptableObject: player base stats
│   │   └── LevelUpConfigSO.cs            # ScriptableObject: XP curve, element choices
│   ├── Gameplay/
│   │   ├── Player/
│   │   │   ├── PlayerController.cs        # Movement via New Input System
│   │   │   ├── PlayerHealth.cs            # HP, damage, death
│   │   │   └── XPCollector.cs             # XP gem collection, level-up trigger
│   │   ├── Enemies/
│   │   │   ├── EnemyBase.cs               # Shared enemy behavior (move toward player, contact damage)
│   │   │   ├── EnemySpawner.cs            # Spawn enemies from pool around player
│   │   │   └── EnemyHealth.cs             # HP, damage taken, element weakness/resistance, death
│   │   ├── Spells/
│   │   │   ├── ElementInventory.cs        # Max 3 element slots, match detection
│   │   │   ├── SpellCrafter.cs            # Combine elements → create/upgrade spells
│   │   │   ├── SpellSlotManager.cs        # Max 6 active spell slots
│   │   │   └── Effects/
│   │   │       ├── SpellEffectBase.cs     # Abstract base for all spell behaviors
│   │   │       ├── OrbitSpellEffect.cs    # Orbit pattern (Alev Halkası, Kaya Kalkanı)
│   │   │       └── ProjectileSpellEffect.cs # Projectile pattern (Kılıç Fırtınası)
│   │   ├── Waves/
│   │   │   └── WaveManager.cs             # Time-based wave progression
│   │   └── Pickups/
│   │       ├── XPGem.cs                   # XP drop from enemies
│   │       └── ElementDrop.cs             # Element drop (not used in Phase 1 — future elite drops)
│   └── UI/
│       ├── HUD/
│       │   ├── HealthBarUI.cs             # Player HP bar
│       │   ├── SpellSlotsUI.cs            # Active spell icons + levels
│       │   ├── ElementInventoryUI.cs      # 3 element inventory slots
│       │   └── RunTimerUI.cs              # Elapsed time display
│       └── Popups/
│           ├── LevelUpPopup.cs            # Element selection on level-up
│           └── RunEndPopup.cs             # Death/completion summary
├── ScriptableObjects/
│   ├── Spells/                            # 15 SpellDefinitionSO assets
│   ├── Enemies/                           # 2 EnemyDefinitionSO assets (Phase 1)
│   └── Config/
│       ├── PlayerConfig.asset
│       ├── WaveConfig.asset
│       └── LevelUpConfig.asset
├── Prefabs/
│   ├── Player/
│   │   └── Player.prefab
│   ├── Enemies/
│   │   ├── KaraKurt.prefab
│   │   └── YekUsagi.prefab
│   ├── Spells/
│   │   ├── AlevHalkasi.prefab
│   │   ├── KayaKalkani.prefab
│   │   └── KilicFirtinasi.prefab
│   └── Pickups/
│       └── XPGem.prefab
├── Scenes/
│   └── GameScene.unity                    # Single playable scene for Phase 1
├── InputActions/
│   └── PlayerInputActions.inputactions    # New Input System asset
└── Tests/
    ├── EditMode/
    │   ├── ElementInventoryTests.cs
    │   ├── SpellCrafterTests.cs
    │   ├── SpellRecipeTests.cs
    │   ├── XPSystemTests.cs
    │   ├── DamageFormulaTests.cs
    │   └── EnemyHealthTests.cs
    └── PlayMode/
        ├── PlayerMovementTests.cs
        └── WaveSpawnerTests.cs
```

---

## Task 1: Unity Project Setup

**Files:**
- Create: Unity project at `Assets/`, `Packages/`, `ProjectSettings/`
- Create: `Assets/Scenes/GameScene.unity`
- Create: `Assets/InputActions/PlayerInputActions.inputactions`

- [ ] **Step 1: Create Unity project**

Open Unity Hub → New Project → 2D (URP) template → name: `KokTengri` → location: `C:\dev\antigravity-workspace\game-project\`

> **Note:** Unity will create `Assets/`, `Packages/`, `ProjectSettings/`, `Library/` (gitignored). The URP template includes the 2D renderer pipeline pre-configured.

- [ ] **Step 2: Configure .gitignore**

Ensure the repo's `.gitignore` covers Unity:

```gitignore
# Unity
[Ll]ibrary/
[Tt]emp/
[Oo]bj/
[Bb]uild/
[Bb]uilds/
[Ll]ogs/
[Uu]ser[Ss]ettings/
*.csproj
*.sln
*.suo
*.user
*.pidb
*.booproj
*.svd
*.pdb
*.mdb
*.opendb
*.VC.db
*.pidb.meta
ExportedObj/
.consulo/
*.apk
*.aab
*.unitypackage
crashlytics-buildid.txt
```

- [ ] **Step 3: Install required packages**

Open Window → Package Manager:
1. Install **Input System** (com.unity.inputsystem) — switch to New Input System when prompted
2. Install **TextMeshPro** (com.unity.textmeshpro) — import TMP Essentials when prompted
3. Verify **Universal RP** is already installed (comes with 2D URP template)
4. Install **Test Framework** (com.unity.test-framework) — should be pre-installed

- [ ] **Step 4: Create folder structure**

In Unity Project window, create:
```
Assets/Scripts/Core/
Assets/Scripts/Data/
Assets/Scripts/Gameplay/Player/
Assets/Scripts/Gameplay/Enemies/
Assets/Scripts/Gameplay/Spells/Effects/
Assets/Scripts/Gameplay/Waves/
Assets/Scripts/Gameplay/Pickups/
Assets/Scripts/UI/HUD/
Assets/Scripts/UI/Popups/
Assets/ScriptableObjects/Spells/
Assets/ScriptableObjects/Enemies/
Assets/ScriptableObjects/Config/
Assets/Prefabs/Player/
Assets/Prefabs/Enemies/
Assets/Prefabs/Spells/
Assets/Prefabs/Pickups/
Assets/InputActions/
Assets/Tests/EditMode/
Assets/Tests/PlayMode/
```

- [ ] **Step 5: Create Input Actions asset**

1. Right-click `Assets/InputActions/` → Create → Input Actions → name: `PlayerInputActions`
2. Open it, create Action Map: `Gameplay`
3. Add Action: `Move` — type: Value, Control Type: Vector2
4. Add Binding: Gamepad Left Stick
5. Add Binding: WASD Composite (keyboard — for editor testing)
6. Add Binding: On-Screen Stick (for mobile touch)
7. Save and click "Generate C# Class" → path: `Assets/Scripts/Core/PlayerInputActions.cs`

- [ ] **Step 6: Create assembly definitions for tests**

Create `Assets/Tests/EditMode/EditModeTests.asmdef`:
```json
{
    "name": "EditModeTests",
    "rootNamespace": "KokTengri.Tests.EditMode",
    "references": ["KokTengri.Core", "KokTengri.Data", "KokTengri.Gameplay"],
    "includePlatforms": ["Editor"],
    "overrideReferences": true,
    "precompiledReferences": ["nunit.framework.dll"],
    "defineConstraints": ["UNITY_INCLUDE_TESTS"]
}
```

Create `Assets/Tests/PlayMode/PlayModeTests.asmdef`:
```json
{
    "name": "PlayModeTests",
    "rootNamespace": "KokTengri.Tests.PlayMode",
    "references": ["KokTengri.Core", "KokTengri.Data", "KokTengri.Gameplay", "UnityEngine.TestRunner", "UnityEditor.TestRunner"],
    "includePlatforms": [],
    "overrideReferences": true,
    "precompiledReferences": ["nunit.framework.dll"],
    "defineConstraints": ["UNITY_INCLUDE_TESTS"]
}
```

Create `Assets/Scripts/Core/KokTengri.Core.asmdef`:
```json
{
    "name": "KokTengri.Core",
    "rootNamespace": "KokTengri.Core",
    "references": ["Unity.InputSystem"],
    "includePlatforms": [],
    "allowUnsafeCode": false
}
```

Create `Assets/Scripts/Data/KokTengri.Data.asmdef`:
```json
{
    "name": "KokTengri.Data",
    "rootNamespace": "KokTengri.Data",
    "references": [],
    "includePlatforms": [],
    "allowUnsafeCode": false
}
```

Create `Assets/Scripts/Gameplay/KokTengri.Gameplay.asmdef`:
```json
{
    "name": "KokTengri.Gameplay",
    "rootNamespace": "KokTengri.Gameplay",
    "references": ["KokTengri.Core", "KokTengri.Data", "Unity.InputSystem"],
    "includePlatforms": [],
    "allowUnsafeCode": false
}
```

Create `Assets/Scripts/UI/KokTengri.UI.asmdef`:
```json
{
    "name": "KokTengri.UI",
    "rootNamespace": "KokTengri.UI",
    "references": ["KokTengri.Core", "KokTengri.Data", "KokTengri.Gameplay", "Unity.TextMeshPro"],
    "includePlatforms": [],
    "allowUnsafeCode": false
}
```

- [ ] **Step 7: Set up GameScene**

1. Open `Assets/Scenes/GameScene.unity` (created by template)
2. Set camera: Orthographic, size 10, background dark (#1a1a2e)
3. Add a temporary colored square sprite as player placeholder (32x32 white, tinted blue)
4. Set Build Settings → add GameScene to Scenes In Build

- [ ] **Step 8: Commit**

```bash
git add Assets/ .gitignore
git commit -m "feat: initialize Unity 2D URP project with folder structure, input system, and test assemblies"
```

---

## Task 2: EventBus & Game Events

**Files:**
- Create: `Assets/Scripts/Core/EventBus.cs`
- Create: `Assets/Scripts/Core/GameEvents.cs`
- Test: `Assets/Tests/EditMode/EventBusTests.cs`

- [ ] **Step 1: Write EventBus tests**

```csharp
// Assets/Tests/EditMode/EventBusTests.cs
using NUnit.Framework;
using KokTengri.Core;

namespace KokTengri.Tests.EditMode
{
    public class EventBusTests
    {
        [SetUp]
        public void SetUp()
        {
            EventBus.Reset();
        }

        [Test]
        public void Subscribe_And_Publish_Delivers_Event()
        {
            int received = 0;
            EventBus.Subscribe<TestEvent>(e => received = e.Value);

            EventBus.Publish(new TestEvent { Value = 42 });

            Assert.AreEqual(42, received);
        }

        [Test]
        public void Unsubscribe_Stops_Delivery()
        {
            int received = 0;
            void Handler(TestEvent e) => received = e.Value;

            EventBus.Subscribe<TestEvent>(Handler);
            EventBus.Unsubscribe<TestEvent>(Handler);
            EventBus.Publish(new TestEvent { Value = 99 });

            Assert.AreEqual(0, received);
        }

        [Test]
        public void Multiple_Subscribers_All_Receive()
        {
            int countA = 0, countB = 0;
            EventBus.Subscribe<TestEvent>(_ => countA++);
            EventBus.Subscribe<TestEvent>(_ => countB++);

            EventBus.Publish(new TestEvent { Value = 1 });

            Assert.AreEqual(1, countA);
            Assert.AreEqual(1, countB);
        }

        [Test]
        public void Reset_Clears_All_Subscribers()
        {
            int received = 0;
            EventBus.Subscribe<TestEvent>(e => received = e.Value);
            EventBus.Reset();
            EventBus.Publish(new TestEvent { Value = 10 });

            Assert.AreEqual(0, received);
        }

        private struct TestEvent
        {
            public int Value;
        }
    }
}
```

- [ ] **Step 2: Run tests — verify they fail**

Run: Unity → Window → General → Test Runner → EditMode → Run All
Expected: 4 failures (EventBus class does not exist)

- [ ] **Step 3: Implement EventBus**

```csharp
// Assets/Scripts/Core/EventBus.cs
using System;
using System.Collections.Generic;

namespace KokTengri.Core
{
    public static class EventBus
    {
        private static readonly Dictionary<Type, List<Delegate>> _subscribers = new();

        public static void Subscribe<T>(Action<T> handler)
        {
            var type = typeof(T);
            if (!_subscribers.ContainsKey(type))
                _subscribers[type] = new List<Delegate>();
            _subscribers[type].Add(handler);
        }

        public static void Unsubscribe<T>(Action<T> handler)
        {
            var type = typeof(T);
            if (_subscribers.ContainsKey(type))
                _subscribers[type].Remove(handler);
        }

        public static void Publish<T>(T evt)
        {
            var type = typeof(T);
            if (!_subscribers.ContainsKey(type)) return;
            // Iterate copy to allow modification during publish
            var handlers = new List<Delegate>(_subscribers[type]);
            foreach (var handler in handlers)
                ((Action<T>)handler)(evt);
        }

        public static void Reset()
        {
            _subscribers.Clear();
        }
    }
}
```

- [ ] **Step 4: Run tests — verify they pass**

Run: Unity → Test Runner → EditMode → Run All
Expected: 4 PASS

- [ ] **Step 5: Define game events**

```csharp
// Assets/Scripts/Core/GameEvents.cs
using KokTengri.Data;

namespace KokTengri.Core
{
    public struct PlayerDamagedEvent
    {
        public float Damage;
        public float CurrentHP;
        public float MaxHP;
    }

    public struct PlayerDiedEvent { }

    public struct PlayerLeveledUpEvent
    {
        public int NewLevel;
    }

    public struct XPCollectedEvent
    {
        public int Amount;
        public int CurrentXP;
        public int XPToNextLevel;
    }

    public struct ElementSelectedEvent
    {
        public ElementType Element;
    }

    public struct SpellCreatedEvent
    {
        public string SpellId;
        public int SlotIndex;
    }

    public struct SpellUpgradedEvent
    {
        public string SpellId;
        public int NewLevel;
    }

    public struct EnemyDiedEvent
    {
        public int XPValue;
        public UnityEngine.Vector3 Position;
        public ElementType Weakness;
    }

    public struct RunStartedEvent { }

    public struct RunEndedEvent
    {
        public float SurvivedMinutes;
        public int EnemiesKilled;
        public int BossesKilled;
        public bool Completed;
    }

    public struct RunTimerTickEvent
    {
        public float ElapsedSeconds;
    }
}
```

- [ ] **Step 6: Commit**

```bash
git add Assets/Scripts/Core/EventBus.cs Assets/Scripts/Core/GameEvents.cs Assets/Tests/EditMode/EventBusTests.cs
git commit -m "feat: add EventBus pub/sub system with game event definitions"
```

---

## Task 3: Element Data Model

**Files:**
- Create: `Assets/Scripts/Data/ElementType.cs`
- Create: `Assets/Scripts/Data/SpellRecipe.cs`
- Test: `Assets/Tests/EditMode/SpellRecipeTests.cs`

- [ ] **Step 1: Write SpellRecipe tests**

```csharp
// Assets/Tests/EditMode/SpellRecipeTests.cs
using NUnit.Framework;
using KokTengri.Data;

namespace KokTengri.Tests.EditMode
{
    public class SpellRecipeTests
    {
        [Test]
        public void Same_Elements_Create_Basic_Spell()
        {
            var recipe = SpellRecipe.Find(ElementType.Od, ElementType.Od);
            Assert.IsNotNull(recipe);
            Assert.AreEqual("AlevHalkasi", recipe.Value.SpellId);
        }

        [Test]
        public void Different_Elements_Create_Combined_Spell()
        {
            var recipe = SpellRecipe.Find(ElementType.Od, ElementType.Temur);
            Assert.IsNotNull(recipe);
            Assert.AreEqual("KilicFirtinasi", recipe.Value.SpellId);
        }

        [Test]
        public void Order_Does_Not_Matter()
        {
            var a = SpellRecipe.Find(ElementType.Od, ElementType.Temur);
            var b = SpellRecipe.Find(ElementType.Temur, ElementType.Od);
            Assert.AreEqual(a.Value.SpellId, b.Value.SpellId);
        }

        [Test]
        public void All_15_Recipes_Exist()
        {
            int count = SpellRecipe.AllRecipes.Count;
            Assert.AreEqual(15, count);
        }

        [Test]
        public void Every_Element_Pair_Has_Recipe()
        {
            var elements = new[] {
                ElementType.Od, ElementType.Sub, ElementType.Yer,
                ElementType.Yel, ElementType.Temur
            };

            foreach (var a in elements)
            foreach (var b in elements)
            {
                if (System.Array.IndexOf(elements, a) > System.Array.IndexOf(elements, b))
                    continue;
                var recipe = SpellRecipe.Find(a, b);
                Assert.IsNotNull(recipe, $"Missing recipe for {a} + {b}");
            }
        }
    }
}
```

- [ ] **Step 2: Run tests — verify they fail**

Run: Test Runner → EditMode → SpellRecipeTests
Expected: 5 failures

- [ ] **Step 3: Implement ElementType and SpellRecipe**

```csharp
// Assets/Scripts/Data/ElementType.cs
namespace KokTengri.Data
{
    public enum ElementType
    {
        Od,     // Fire
        Sub,    // Water
        Yer,    // Earth
        Yel,    // Air
        Temur   // Iron
    }
}
```

```csharp
// Assets/Scripts/Data/SpellRecipe.cs
using System.Collections.Generic;

namespace KokTengri.Data
{
    public struct SpellRecipe
    {
        public ElementType ElementA;
        public ElementType ElementB;
        public string SpellId;
        public string DisplayName;

        private static List<SpellRecipe> _allRecipes;

        public static IReadOnlyList<SpellRecipe> AllRecipes
        {
            get
            {
                if (_allRecipes == null) InitializeRecipes();
                return _allRecipes;
            }
        }

        public static SpellRecipe? Find(ElementType a, ElementType b)
        {
            if (_allRecipes == null) InitializeRecipes();
            // Normalize order: smaller enum value first
            if (a > b) (a, b) = (b, a);
            foreach (var recipe in _allRecipes)
            {
                var ra = recipe.ElementA;
                var rb = recipe.ElementB;
                if (ra > rb) (ra, rb) = (rb, ra);
                if (ra == a && rb == b) return recipe;
            }
            return null;
        }

        private static void InitializeRecipes()
        {
            _allRecipes = new List<SpellRecipe>
            {
                // Basic spells (same element x2)
                new() { ElementA = ElementType.Od,    ElementB = ElementType.Od,    SpellId = "AlevHalkasi",    DisplayName = "Alev Halkası" },
                new() { ElementA = ElementType.Sub,   ElementB = ElementType.Sub,   SpellId = "SifaPinari",     DisplayName = "Şifa Pınarı" },
                new() { ElementA = ElementType.Yer,   ElementB = ElementType.Yer,   SpellId = "KayaKalkani",    DisplayName = "Kaya Kalkanı" },
                new() { ElementA = ElementType.Yel,   ElementB = ElementType.Yel,   SpellId = "RuzgarKosusu",   DisplayName = "Rüzgar Koşusu" },
                new() { ElementA = ElementType.Temur, ElementB = ElementType.Temur, SpellId = "DemirYagmuru",   DisplayName = "Demir Yağmuru" },

                // Combined spells (different elements)
                new() { ElementA = ElementType.Od,  ElementB = ElementType.Temur, SpellId = "KilicFirtinasi",  DisplayName = "Kılıç Fırtınası" },
                new() { ElementA = ElementType.Sub, ElementB = ElementType.Yel,   SpellId = "BuzRuzgari",      DisplayName = "Buz Rüzgarı" },
                new() { ElementA = ElementType.Yel, ElementB = ElementType.Temur, SpellId = "OkYagmuru",       DisplayName = "Ok Yağmuru" },
                new() { ElementA = ElementType.Od,  ElementB = ElementType.Sub,   SpellId = "BuharPatlamasi",  DisplayName = "Buhar Patlaması" },
                new() { ElementA = ElementType.Yer, ElementB = ElementType.Temur, SpellId = "Deprem",          DisplayName = "Deprem" },
                new() { ElementA = ElementType.Od,  ElementB = ElementType.Yel,   SpellId = "AtesKasergasi",   DisplayName = "Ateş Kasırgası" },
                new() { ElementA = ElementType.Yer, ElementB = ElementType.Sub,   SpellId = "Bataklik",        DisplayName = "Bataklık" },
                new() { ElementA = ElementType.Od,  ElementB = ElementType.Yer,   SpellId = "LavSeli",         DisplayName = "Lav Seli" },
                new() { ElementA = ElementType.Sub, ElementB = ElementType.Temur, SpellId = "BuzKilici",       DisplayName = "Buz Kılıcı" },
                new() { ElementA = ElementType.Yer, ElementB = ElementType.Yel,   SpellId = "KumFirtinasi",    DisplayName = "Kum Fırtınası" },
            };
        }
    }
}
```

- [ ] **Step 4: Run tests — verify they pass**

Run: Test Runner → EditMode → SpellRecipeTests
Expected: 5 PASS

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/Data/ Assets/Tests/EditMode/SpellRecipeTests.cs
git commit -m "feat: add ElementType enum and SpellRecipe lookup with all 15 recipes"
```

---

## Task 4: Element Inventory System

**Files:**
- Create: `Assets/Scripts/Gameplay/Spells/ElementInventory.cs`
- Test: `Assets/Tests/EditMode/ElementInventoryTests.cs`

- [ ] **Step 1: Write ElementInventory tests**

```csharp
// Assets/Tests/EditMode/ElementInventoryTests.cs
using NUnit.Framework;
using KokTengri.Data;
using KokTengri.Gameplay.Spells;

namespace KokTengri.Tests.EditMode
{
    public class ElementInventoryTests
    {
        private ElementInventory _inventory;

        [SetUp]
        public void SetUp()
        {
            _inventory = new ElementInventory(maxSlots: 3);
        }

        [Test]
        public void Add_Element_To_Empty_Inventory()
        {
            bool added = _inventory.TryAdd(ElementType.Od);
            Assert.IsTrue(added);
            Assert.AreEqual(1, _inventory.Count);
        }

        [Test]
        public void Cannot_Exceed_Max_Slots()
        {
            _inventory.TryAdd(ElementType.Od);
            _inventory.TryAdd(ElementType.Sub);
            _inventory.TryAdd(ElementType.Yer);
            bool added = _inventory.TryAdd(ElementType.Yel);

            Assert.IsFalse(added);
            Assert.AreEqual(3, _inventory.Count);
        }

        [Test]
        public void FindMatch_Returns_Recipe_When_Pair_Exists()
        {
            _inventory.TryAdd(ElementType.Od);
            var match = _inventory.FindMatch(ElementType.Od);

            Assert.IsNotNull(match);
            Assert.AreEqual("AlevHalkasi", match.Value.SpellId);
        }

        [Test]
        public void FindMatch_Returns_Null_When_No_Pair()
        {
            _inventory.TryAdd(ElementType.Sub);
            var match = _inventory.FindMatch(ElementType.Yer);

            // Sub + Yer = Bataklık — this IS a match
            Assert.IsNotNull(match);
        }

        [Test]
        public void FindMatch_Returns_Null_When_Inventory_Empty()
        {
            var match = _inventory.FindMatch(ElementType.Od);
            Assert.IsNull(match);
        }

        [Test]
        public void ConsumeMatch_Removes_Matched_Element()
        {
            _inventory.TryAdd(ElementType.Od);
            _inventory.ConsumeMatch(ElementType.Od);

            Assert.AreEqual(0, _inventory.Count);
        }

        [Test]
        public void ConsumeMatch_Removes_Correct_Element_For_Combined()
        {
            _inventory.TryAdd(ElementType.Od);
            _inventory.TryAdd(ElementType.Sub);
            _inventory.ConsumeMatch(ElementType.Temur); // matches Od → Kılıç Fırtınası

            // Should consume Od, leave Sub
            Assert.AreEqual(1, _inventory.Count);
            Assert.IsTrue(_inventory.Contains(ElementType.Sub));
        }

        [Test]
        public void RemoveAt_Removes_Element_By_Index()
        {
            _inventory.TryAdd(ElementType.Od);
            _inventory.TryAdd(ElementType.Sub);
            _inventory.RemoveAt(0);

            Assert.AreEqual(1, _inventory.Count);
            Assert.IsTrue(_inventory.Contains(ElementType.Sub));
        }

        [Test]
        public void Clear_Empties_Inventory()
        {
            _inventory.TryAdd(ElementType.Od);
            _inventory.TryAdd(ElementType.Sub);
            _inventory.Clear();

            Assert.AreEqual(0, _inventory.Count);
        }

        [Test]
        public void IsFull_Returns_True_When_At_Capacity()
        {
            _inventory.TryAdd(ElementType.Od);
            _inventory.TryAdd(ElementType.Sub);
            _inventory.TryAdd(ElementType.Yer);

            Assert.IsTrue(_inventory.IsFull);
        }
    }
}
```

- [ ] **Step 2: Run tests — verify they fail**

Run: Test Runner → EditMode → ElementInventoryTests
Expected: 10 failures

- [ ] **Step 3: Implement ElementInventory**

```csharp
// Assets/Scripts/Gameplay/Spells/ElementInventory.cs
using System.Collections.Generic;
using KokTengri.Data;

namespace KokTengri.Gameplay.Spells
{
    public class ElementInventory
    {
        private readonly List<ElementType> _elements;
        private readonly int _maxSlots;

        public int Count => _elements.Count;
        public bool IsFull => _elements.Count >= _maxSlots;
        public IReadOnlyList<ElementType> Elements => _elements;

        public ElementInventory(int maxSlots = 3)
        {
            _maxSlots = maxSlots;
            _elements = new List<ElementType>(maxSlots);
        }

        public bool TryAdd(ElementType element)
        {
            if (IsFull) return false;
            _elements.Add(element);
            return true;
        }

        public bool Contains(ElementType element)
        {
            return _elements.Contains(element);
        }

        /// <summary>
        /// Check if adding this element would match with any existing element.
        /// Returns the recipe if a match exists, null otherwise.
        /// Does NOT consume the elements.
        /// </summary>
        public SpellRecipe? FindMatch(ElementType incoming)
        {
            foreach (var existing in _elements)
            {
                var recipe = SpellRecipe.Find(existing, incoming);
                if (recipe != null) return recipe;
            }
            return null;
        }

        /// <summary>
        /// Remove the element that matches with the incoming element.
        /// Call this after FindMatch returns a recipe and the spell is created.
        /// </summary>
        public void ConsumeMatch(ElementType incoming)
        {
            for (int i = 0; i < _elements.Count; i++)
            {
                var recipe = SpellRecipe.Find(_elements[i], incoming);
                if (recipe != null)
                {
                    _elements.RemoveAt(i);
                    return;
                }
            }
        }

        public void RemoveAt(int index)
        {
            if (index >= 0 && index < _elements.Count)
                _elements.RemoveAt(index);
        }

        public void Clear()
        {
            _elements.Clear();
        }
    }
}
```

- [ ] **Step 4: Run tests — verify they pass**

Run: Test Runner → EditMode → ElementInventoryTests
Expected: 10 PASS

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/Gameplay/Spells/ElementInventory.cs Assets/Tests/EditMode/ElementInventoryTests.cs
git commit -m "feat: add ElementInventory with max 3 slots, match detection, and consume logic"
```

---

## Task 5: SpellCrafter — Element→Spell Pipeline

**Files:**
- Create: `Assets/Scripts/Gameplay/Spells/SpellCrafter.cs`
- Create: `Assets/Scripts/Gameplay/Spells/SpellSlotManager.cs`
- Test: `Assets/Tests/EditMode/SpellCrafterTests.cs`

- [ ] **Step 1: Write SpellCrafter tests**

```csharp
// Assets/Tests/EditMode/SpellCrafterTests.cs
using NUnit.Framework;
using KokTengri.Core;
using KokTengri.Data;
using KokTengri.Gameplay.Spells;

namespace KokTengri.Tests.EditMode
{
    public class SpellCrafterTests
    {
        private ElementInventory _inventory;
        private SpellSlotManager _slots;
        private SpellCrafter _crafter;

        [SetUp]
        public void SetUp()
        {
            EventBus.Reset();
            _inventory = new ElementInventory(maxSlots: 3);
            _slots = new SpellSlotManager(maxSlots: 6);
            _crafter = new SpellCrafter(_inventory, _slots);
        }

        [Test]
        public void AddElement_No_Match_Goes_To_Inventory()
        {
            var result = _crafter.AddElement(ElementType.Od);

            Assert.AreEqual(SpellCrafter.Result.AddedToInventory, result);
            Assert.AreEqual(1, _inventory.Count);
            Assert.AreEqual(0, _slots.ActiveCount);
        }

        [Test]
        public void AddElement_With_Match_Creates_Spell()
        {
            _crafter.AddElement(ElementType.Od);
            var result = _crafter.AddElement(ElementType.Od);

            Assert.AreEqual(SpellCrafter.Result.SpellCreated, result);
            Assert.AreEqual(0, _inventory.Count); // consumed
            Assert.AreEqual(1, _slots.ActiveCount);
            Assert.AreEqual("AlevHalkasi", _slots.GetSpell(0).SpellId);
        }

        [Test]
        public void AddElement_Matching_Existing_Spell_Upgrades()
        {
            // Create Alev Halkası (Od + Od)
            _crafter.AddElement(ElementType.Od);
            _crafter.AddElement(ElementType.Od);
            Assert.AreEqual(1, _slots.GetSpell(0).Level);

            // Add another Od + Od → should upgrade to level 2
            _crafter.AddElement(ElementType.Od);
            var result = _crafter.AddElement(ElementType.Od);

            Assert.AreEqual(SpellCrafter.Result.SpellUpgraded, result);
            Assert.AreEqual(1, _slots.ActiveCount); // still 1 spell
            Assert.AreEqual(2, _slots.GetSpell(0).Level);
        }

        [Test]
        public void Spell_Cannot_Exceed_Max_Level()
        {
            // Create and max out Alev Halkası (5 levels = 10 Od elements)
            for (int i = 0; i < 10; i++)
                _crafter.AddElement(ElementType.Od);

            Assert.AreEqual(5, _slots.GetSpell(0).Level);

            // 11th and 12th Od should go to inventory, not upgrade
            _crafter.AddElement(ElementType.Od);
            _crafter.AddElement(ElementType.Od);
            Assert.AreEqual(5, _slots.GetSpell(0).Level); // still 5
        }

        [Test]
        public void Spell_Slots_Full_Prevents_New_Spell()
        {
            // Fill 6 slots with different spells
            // Slot 1: Od + Od = Alev Halkası
            _crafter.AddElement(ElementType.Od); _crafter.AddElement(ElementType.Od);
            // Slot 2: Sub + Sub = Şifa Pınarı
            _crafter.AddElement(ElementType.Sub); _crafter.AddElement(ElementType.Sub);
            // Slot 3: Yer + Yer = Kaya Kalkanı
            _crafter.AddElement(ElementType.Yer); _crafter.AddElement(ElementType.Yer);
            // Slot 4: Yel + Yel = Rüzgar Koşusu
            _crafter.AddElement(ElementType.Yel); _crafter.AddElement(ElementType.Yel);
            // Slot 5: Temür + Temür = Demir Yağmuru
            _crafter.AddElement(ElementType.Temur); _crafter.AddElement(ElementType.Temur);
            // Slot 6: Od + Temür = Kılıç Fırtınası
            _crafter.AddElement(ElementType.Od); _crafter.AddElement(ElementType.Temur);

            Assert.AreEqual(6, _slots.ActiveCount);

            // Try new spell: Sub + Yel = Buz Rüzgarı — slots full
            _crafter.AddElement(ElementType.Sub);
            var result = _crafter.AddElement(ElementType.Yel);

            // Should stay in inventory, not create spell
            Assert.AreEqual(SpellCrafter.Result.SlotsFull, result);
            Assert.AreEqual(6, _slots.ActiveCount);
        }

        [Test]
        public void Combined_Spell_Consumes_Both_Elements()
        {
            _crafter.AddElement(ElementType.Od);
            _crafter.AddElement(ElementType.Sub);  // Od + Sub = Buhar Patlaması

            Assert.AreEqual(0, _inventory.Count);
            Assert.AreEqual(1, _slots.ActiveCount);
            Assert.AreEqual("BuharPatlamasi", _slots.GetSpell(0).SpellId);
        }
    }
}
```

- [ ] **Step 2: Run tests — verify they fail**

Run: Test Runner → EditMode → SpellCrafterTests
Expected: 6 failures

- [ ] **Step 3: Implement SpellSlotManager**

```csharp
// Assets/Scripts/Gameplay/Spells/SpellSlotManager.cs
using System.Collections.Generic;

namespace KokTengri.Gameplay.Spells
{
    public class SpellSlotManager
    {
        public struct ActiveSpell
        {
            public string SpellId;
            public string DisplayName;
            public int Level;
        }

        private readonly List<ActiveSpell> _spells;
        private readonly int _maxSlots;
        private readonly int _maxLevel;

        public int ActiveCount => _spells.Count;
        public bool IsFull => _spells.Count >= _maxSlots;
        public IReadOnlyList<ActiveSpell> Spells => _spells;

        public SpellSlotManager(int maxSlots = 6, int maxLevel = 5)
        {
            _maxSlots = maxSlots;
            _maxLevel = maxLevel;
            _spells = new List<ActiveSpell>(maxSlots);
        }

        public ActiveSpell GetSpell(int index) => _spells[index];

        public int FindSpellIndex(string spellId)
        {
            for (int i = 0; i < _spells.Count; i++)
                if (_spells[i].SpellId == spellId) return i;
            return -1;
        }

        public bool TryAddSpell(string spellId, string displayName)
        {
            if (IsFull) return false;
            _spells.Add(new ActiveSpell { SpellId = spellId, DisplayName = displayName, Level = 1 });
            return true;
        }

        public bool TryUpgradeSpell(string spellId)
        {
            int index = FindSpellIndex(spellId);
            if (index < 0) return false;
            var spell = _spells[index];
            if (spell.Level >= _maxLevel) return false;
            spell.Level++;
            _spells[index] = spell;
            return true;
        }

        public bool IsMaxLevel(string spellId)
        {
            int index = FindSpellIndex(spellId);
            if (index < 0) return false;
            return _spells[index].Level >= _maxLevel;
        }

        public void Clear()
        {
            _spells.Clear();
        }
    }
}
```

- [ ] **Step 4: Implement SpellCrafter**

```csharp
// Assets/Scripts/Gameplay/Spells/SpellCrafter.cs
using KokTengri.Core;
using KokTengri.Data;

namespace KokTengri.Gameplay.Spells
{
    public class SpellCrafter
    {
        public enum Result
        {
            AddedToInventory,
            SpellCreated,
            SpellUpgraded,
            SlotsFull,
            InventoryFull
        }

        private readonly ElementInventory _inventory;
        private readonly SpellSlotManager _slots;

        public SpellCrafter(ElementInventory inventory, SpellSlotManager slots)
        {
            _inventory = inventory;
            _slots = slots;
        }

        public Result AddElement(ElementType element)
        {
            // Check if this element matches something in inventory
            var recipe = _inventory.FindMatch(element);

            if (recipe != null)
            {
                string spellId = recipe.Value.SpellId;
                int existingIndex = _slots.FindSpellIndex(spellId);

                if (existingIndex >= 0)
                {
                    // Spell already exists — try upgrade
                    if (_slots.TryUpgradeSpell(spellId))
                    {
                        _inventory.ConsumeMatch(element);
                        var spell = _slots.GetSpell(existingIndex);
                        EventBus.Publish(new SpellUpgradedEvent { SpellId = spellId, NewLevel = spell.Level });
                        return Result.SpellUpgraded;
                    }
                    // Max level — just add to inventory
                }
                else if (!_slots.IsFull)
                {
                    // New spell — create it
                    _inventory.ConsumeMatch(element);
                    _slots.TryAddSpell(spellId, recipe.Value.DisplayName);
                    int slotIndex = _slots.FindSpellIndex(spellId);
                    EventBus.Publish(new SpellCreatedEvent { SpellId = spellId, SlotIndex = slotIndex });
                    return Result.SpellCreated;
                }
                else
                {
                    // Slots full — can't create new spell, element stays in inventory
                    if (!_inventory.IsFull)
                    {
                        _inventory.TryAdd(element);
                        return Result.SlotsFull;
                    }
                    return Result.InventoryFull;
                }
            }

            // No match — add to inventory
            if (_inventory.TryAdd(element))
                return Result.AddedToInventory;

            return Result.InventoryFull;
        }
    }
}
```

- [ ] **Step 5: Run tests — verify they pass**

Run: Test Runner → EditMode → SpellCrafterTests
Expected: 6 PASS

- [ ] **Step 6: Commit**

```bash
git add Assets/Scripts/Gameplay/Spells/SpellSlotManager.cs Assets/Scripts/Gameplay/Spells/SpellCrafter.cs Assets/Tests/EditMode/SpellCrafterTests.cs
git commit -m "feat: add SpellCrafter pipeline — element inventory → match → create/upgrade spells"
```

---

## Task 6: ScriptableObject Data Definitions

**Files:**
- Create: `Assets/Scripts/Data/SpellDefinitionSO.cs`
- Create: `Assets/Scripts/Data/EnemyDefinitionSO.cs`
- Create: `Assets/Scripts/Data/PlayerConfigSO.cs`
- Create: `Assets/Scripts/Data/WaveConfigSO.cs`
- Create: `Assets/Scripts/Data/LevelUpConfigSO.cs`

- [ ] **Step 1: Create SpellDefinitionSO**

```csharp
// Assets/Scripts/Data/SpellDefinitionSO.cs
using UnityEngine;

namespace KokTengri.Data
{
    [CreateAssetMenu(fileName = "NewSpell", menuName = "KokTengri/Spell Definition")]
    public class SpellDefinitionSO : ScriptableObject
    {
        [Header("Identity")]
        public string spellId;
        public string displayName;
        public ElementType elementA;
        public ElementType elementB;

        [Header("Combat")]
        public float baseDamage = 10f;
        public float damagePerLevel = 0.25f;  // multiplier: 1 + damagePerLevel * (level - 1)
        public float tickRate = 1f;            // seconds between damage ticks
        public float range = 2f;

        [Header("Behavior")]
        public SpellPattern pattern;
        public float speed = 3f;               // for projectiles
        public int projectileCount = 1;        // for projectile spells
        public float orbitRadius = 1.5f;       // for orbit spells
        public float duration = -1f;           // -1 = permanent

        [Header("Visuals")]
        public Color spellColor = Color.white;
        public GameObject prefab;

        public float CalculateDamage(int level, float elementMultiplier, float classBonus)
        {
            return baseDamage * (1f + damagePerLevel * (level - 1)) * elementMultiplier * classBonus;
        }
    }

    public enum SpellPattern
    {
        Orbit,       // Circles around player (Alev Halkası, Kaya Kalkanı)
        Projectile,  // Fires outward (Kılıç Fırtınası, Ok Yağmuru)
        Aura,        // Area around player (Buhar Patlaması, Bataklık)
        Trail,       // Follows player path (Rüzgar Koşusu, Lav Seli)
        Rain,        // Random area drops (Demir Yağmuru, Kum Fırtınası)
        Cone,        // Directional cone (Buz Rüzgarı)
        Buff         // Stat modification (Şifa Pınarı)
    }
}
```

- [ ] **Step 2: Create EnemyDefinitionSO**

```csharp
// Assets/Scripts/Data/EnemyDefinitionSO.cs
using UnityEngine;

namespace KokTengri.Data
{
    [CreateAssetMenu(fileName = "NewEnemy", menuName = "KokTengri/Enemy Definition")]
    public class EnemyDefinitionSO : ScriptableObject
    {
        [Header("Identity")]
        public string enemyId;
        public string displayName;

        [Header("Stats")]
        public float baseHP = 10f;
        public float baseDamage = 5f;
        public float moveSpeed = 2f;

        [Header("Scaling — per spec: hp = baseHP * (1 + 0.12 * minute)")]
        public float hpScalePerMinute = 0.12f;
        public float damageScalePerMinute = 0.08f;

        [Header("Elements")]
        public ElementType weakness;
        public ElementType resistance;

        [Header("XP")]
        public int xpValue = 1;

        [Header("Spawn")]
        public float firstAppearMinute = 0f;
        public GameObject prefab;

        public float GetHP(float elapsedMinutes)
        {
            return baseHP * (1f + hpScalePerMinute * elapsedMinutes);
        }

        public float GetDamage(float elapsedMinutes)
        {
            return baseDamage * (1f + damageScalePerMinute * elapsedMinutes);
        }
    }
}
```

- [ ] **Step 3: Create config ScriptableObjects**

```csharp
// Assets/Scripts/Data/PlayerConfigSO.cs
using UnityEngine;

namespace KokTengri.Data
{
    [CreateAssetMenu(fileName = "PlayerConfig", menuName = "KokTengri/Config/Player Config")]
    public class PlayerConfigSO : ScriptableObject
    {
        [Header("Base Stats")]
        public float baseHP = 100f;
        public float baseMoveSpeed = 3f;
        public float pickupRadius = 1.5f;
        public float invincibilityDuration = 0.5f;

        [Header("Element Inventory")]
        public int maxElementSlots = 3;
        public int maxSpellSlots = 6;
        public int maxSpellLevel = 5;
    }
}
```

```csharp
// Assets/Scripts/Data/WaveConfigSO.cs
using UnityEngine;

namespace KokTengri.Data
{
    [CreateAssetMenu(fileName = "WaveConfig", menuName = "KokTengri/Config/Wave Config")]
    public class WaveConfigSO : ScriptableObject
    {
        [Header("Timing")]
        public float runDurationSeconds = 1800f;  // 30 minutes
        public float bossIntervalSeconds = 300f;   // every 5 min

        [Header("Spawn Rate")]
        public float baseSpawnInterval = 1.5f;     // seconds between spawns
        public float spawnRateIncreasePerMinute = 0.10f;  // +10%/min
        public float minSpawnInterval = 0.3f;
        public int baseEnemiesPerSpawn = 3;
        public float spawnRadius = 12f;            // spawn distance from player

        [Header("Elite")]
        public float eliteStartMinute = 10f;
        public float eliteChance = 0.05f;          // 5% per spawn
        public float eliteHPMultiplier = 3f;
        public float eliteDamageMultiplier = 1.5f;
        public int eliteXPMultiplier = 3;
    }
}
```

```csharp
// Assets/Scripts/Data/LevelUpConfigSO.cs
using UnityEngine;

namespace KokTengri.Data
{
    [CreateAssetMenu(fileName = "LevelUpConfig", menuName = "KokTengri/Config/LevelUp Config")]
    public class LevelUpConfigSO : ScriptableObject
    {
        [Header("XP Curve — XP_needed = baseXP * level^exponent")]
        public float baseXP = 10f;
        public float exponent = 1.4f;

        [Header("Element Selection")]
        public int elementChoices = 3;
        public float startingElementBiasPercent = 15f;  // +15% chance for hero's element

        public int GetXPForLevel(int level)
        {
            return Mathf.RoundToInt(baseXP * Mathf.Pow(level, exponent));
        }
    }
}
```

- [ ] **Step 4: Create ScriptableObject assets in Unity**

In Unity Editor:
1. Right-click `Assets/ScriptableObjects/Config/` → Create → KokTengri → Config → Player Config → name: `PlayerConfig`
2. Right-click `Assets/ScriptableObjects/Config/` → Create → KokTengri → Config → Wave Config → name: `WaveConfig`
3. Right-click `Assets/ScriptableObjects/Config/` → Create → KokTengri → Config → LevelUp Config → name: `LevelUpConfig`
4. Right-click `Assets/ScriptableObjects/Enemies/` → Create → KokTengri → Enemy Definition → name: `KaraKurt`
   - Set: enemyId=`KaraKurt`, displayName=`Kara Kurt`, baseHP=8, baseDamage=5, moveSpeed=3.5, weakness=Od, resistance=Yel, xpValue=1, firstAppearMinute=0
5. Right-click `Assets/ScriptableObjects/Enemies/` → Create → KokTengri → Enemy Definition → name: `YekUsagi`
   - Set: enemyId=`YekUsagi`, displayName=`Yek Uşağı`, baseHP=25, baseDamage=8, moveSpeed=1.5, weakness=Yel, resistance=Temur, xpValue=3, firstAppearMinute=2
6. Create 3 SpellDefinitionSO assets in `Assets/ScriptableObjects/Spells/`:
   - `AlevHalkasi`: spellId=`AlevHalkasi`, baseDamage=8, pattern=Orbit, orbitRadius=1.5, tickRate=0.5, spellColor=Red, elementA=Od, elementB=Od
   - `KayaKalkani`: spellId=`KayaKalkani`, baseDamage=5, pattern=Orbit, orbitRadius=1.2, tickRate=0.8, spellColor=Brown, elementA=Yer, elementB=Yer
   - `KilicFirtinasi`: spellId=`KilicFirtinasi`, baseDamage=12, pattern=Projectile, speed=6, projectileCount=1, tickRate=1.5, spellColor=Orange, elementA=Od, elementB=Temur

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/Data/ Assets/ScriptableObjects/
git commit -m "feat: add ScriptableObject definitions for spells, enemies, and config"
```

---

## Task 7: XP System & Damage Formulas

**Files:**
- Create: `Assets/Scripts/Gameplay/Player/XPCollector.cs` (logic-only part)
- Test: `Assets/Tests/EditMode/XPSystemTests.cs`
- Test: `Assets/Tests/EditMode/DamageFormulaTests.cs`

- [ ] **Step 1: Write XP system tests**

```csharp
// Assets/Tests/EditMode/XPSystemTests.cs
using NUnit.Framework;
using KokTengri.Data;
using UnityEngine;

namespace KokTengri.Tests.EditMode
{
    public class XPSystemTests
    {
        [Test]
        public void Level_1_Requires_10_XP()
        {
            var config = ScriptableObject.CreateInstance<LevelUpConfigSO>();
            config.baseXP = 10f;
            config.exponent = 1.4f;

            Assert.AreEqual(10, config.GetXPForLevel(1));
        }

        [Test]
        public void Level_5_Requires_More_Than_Level_1()
        {
            var config = ScriptableObject.CreateInstance<LevelUpConfigSO>();
            config.baseXP = 10f;
            config.exponent = 1.4f;

            int xp1 = config.GetXPForLevel(1);
            int xp5 = config.GetXPForLevel(5);
            Assert.Greater(xp5, xp1);
        }

        [Test]
        public void XP_Curve_Is_Exponential()
        {
            var config = ScriptableObject.CreateInstance<LevelUpConfigSO>();
            config.baseXP = 10f;
            config.exponent = 1.4f;

            int xp5 = config.GetXPForLevel(5);
            int xp10 = config.GetXPForLevel(10);
            int xp20 = config.GetXPForLevel(20);

            // Gaps should increase: (10-5) gap < (20-10) gap
            Assert.Greater(xp20 - xp10, xp10 - xp5);
        }
    }
}
```

- [ ] **Step 2: Write damage formula tests**

```csharp
// Assets/Tests/EditMode/DamageFormulaTests.cs
using NUnit.Framework;
using KokTengri.Data;
using UnityEngine;

namespace KokTengri.Tests.EditMode
{
    public class DamageFormulaTests
    {
        [Test]
        public void Spell_Damage_At_Level_1_Equals_Base()
        {
            var spell = ScriptableObject.CreateInstance<SpellDefinitionSO>();
            spell.baseDamage = 10f;
            spell.damagePerLevel = 0.25f;

            float damage = spell.CalculateDamage(level: 1, elementMultiplier: 1f, classBonus: 1f);
            Assert.AreEqual(10f, damage, 0.01f);
        }

        [Test]
        public void Spell_Damage_Scales_With_Level()
        {
            var spell = ScriptableObject.CreateInstance<SpellDefinitionSO>();
            spell.baseDamage = 10f;
            spell.damagePerLevel = 0.25f;

            float dmg1 = spell.CalculateDamage(level: 1, elementMultiplier: 1f, classBonus: 1f);
            float dmg5 = spell.CalculateDamage(level: 5, elementMultiplier: 1f, classBonus: 1f);

            // Level 5: 10 * (1 + 0.25 * 4) = 10 * 2.0 = 20
            Assert.AreEqual(10f, dmg1, 0.01f);
            Assert.AreEqual(20f, dmg5, 0.01f);
        }

        [Test]
        public void Weakness_Multiplier_Increases_Damage()
        {
            var spell = ScriptableObject.CreateInstance<SpellDefinitionSO>();
            spell.baseDamage = 10f;
            spell.damagePerLevel = 0.25f;

            float normal = spell.CalculateDamage(level: 1, elementMultiplier: 1.0f, classBonus: 1f);
            float weak = spell.CalculateDamage(level: 1, elementMultiplier: 1.5f, classBonus: 1f);

            Assert.AreEqual(15f, weak, 0.01f);
            Assert.Greater(weak, normal);
        }

        [Test]
        public void Resistance_Multiplier_Reduces_Damage()
        {
            var spell = ScriptableObject.CreateInstance<SpellDefinitionSO>();
            spell.baseDamage = 10f;
            spell.damagePerLevel = 0.25f;

            float resist = spell.CalculateDamage(level: 1, elementMultiplier: 0.6f, classBonus: 1f);
            Assert.AreEqual(6f, resist, 0.01f);
        }

        [Test]
        public void Enemy_HP_Scales_With_Time()
        {
            var enemy = ScriptableObject.CreateInstance<EnemyDefinitionSO>();
            enemy.baseHP = 8f;
            enemy.hpScalePerMinute = 0.12f;

            float hp0 = enemy.GetHP(0f);
            float hp10 = enemy.GetHP(10f);
            float hp30 = enemy.GetHP(30f);

            Assert.AreEqual(8f, hp0, 0.01f);
            Assert.AreEqual(17.6f, hp10, 0.1f);   // 8 * (1 + 0.12 * 10) = 8 * 2.2
            Assert.AreEqual(36.8f, hp30, 0.1f);   // 8 * (1 + 0.12 * 30) = 8 * 4.6
        }

        [Test]
        public void Class_Bonus_Applies_Multiplicatively()
        {
            var spell = ScriptableObject.CreateInstance<SpellDefinitionSO>();
            spell.baseDamage = 10f;
            spell.damagePerLevel = 0.25f;

            // With 25% class bonus
            float withBonus = spell.CalculateDamage(level: 1, elementMultiplier: 1f, classBonus: 1.25f);
            Assert.AreEqual(12.5f, withBonus, 0.01f);
        }
    }
}
```

- [ ] **Step 3: Run tests — verify they pass**

Run: Test Runner → EditMode → XPSystemTests + DamageFormulaTests
Expected: All 9 PASS (formulas are already implemented in ScriptableObjects from Task 6)

- [ ] **Step 4: Commit**

```bash
git add Assets/Tests/EditMode/XPSystemTests.cs Assets/Tests/EditMode/DamageFormulaTests.cs
git commit -m "test: add XP curve and damage formula tests — verify spec formulas"
```

---

## Task 8: Player Controller — Movement & Health

**Files:**
- Create: `Assets/Scripts/Gameplay/Player/PlayerController.cs`
- Create: `Assets/Scripts/Gameplay/Player/PlayerHealth.cs`

- [ ] **Step 1: Implement PlayerController**

```csharp
// Assets/Scripts/Gameplay/Player/PlayerController.cs
using UnityEngine;
using UnityEngine.InputSystem;
using KokTengri.Data;

namespace KokTengri.Gameplay.Player
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] private PlayerConfigSO config;

        private Rigidbody2D _rb;
        private Vector2 _moveInput;
        private PlayerInputActions _inputActions;

        public Vector2 MoveInput => _moveInput;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _rb.gravityScale = 0f;
            _rb.freezeRotation = true;

            _inputActions = new PlayerInputActions();
        }

        private void OnEnable()
        {
            _inputActions.Gameplay.Enable();
            _inputActions.Gameplay.Move.performed += OnMove;
            _inputActions.Gameplay.Move.canceled += OnMove;
        }

        private void OnDisable()
        {
            _inputActions.Gameplay.Move.performed -= OnMove;
            _inputActions.Gameplay.Move.canceled -= OnMove;
            _inputActions.Gameplay.Disable();
        }

        private void OnMove(InputAction.CallbackContext ctx)
        {
            _moveInput = ctx.ReadValue<Vector2>();
        }

        private void FixedUpdate()
        {
            float speed = config != null ? config.baseMoveSpeed : 3f;
            _rb.linearVelocity = _moveInput.normalized * speed;
        }
    }
}
```

- [ ] **Step 2: Implement PlayerHealth**

```csharp
// Assets/Scripts/Gameplay/Player/PlayerHealth.cs
using UnityEngine;
using KokTengri.Core;
using KokTengri.Data;

namespace KokTengri.Gameplay.Player
{
    public class PlayerHealth : MonoBehaviour
    {
        [SerializeField] private PlayerConfigSO config;

        private float _currentHP;
        private float _maxHP;
        private float _invincibilityTimer;

        public float CurrentHP => _currentHP;
        public float MaxHP => _maxHP;
        public float HPPercent => _maxHP > 0 ? _currentHP / _maxHP : 0f;
        public bool IsDead => _currentHP <= 0f;
        public bool IsInvincible => _invincibilityTimer > 0f;

        private void Awake()
        {
            _maxHP = config != null ? config.baseHP : 100f;
            _currentHP = _maxHP;
        }

        private void Update()
        {
            if (_invincibilityTimer > 0f)
                _invincibilityTimer -= Time.deltaTime;
        }

        public void TakeDamage(float damage)
        {
            if (IsDead || IsInvincible) return;

            _currentHP = Mathf.Max(0f, _currentHP - damage);
            _invincibilityTimer = config != null ? config.invincibilityDuration : 0.5f;

            EventBus.Publish(new PlayerDamagedEvent
            {
                Damage = damage,
                CurrentHP = _currentHP,
                MaxHP = _maxHP
            });

            if (_currentHP <= 0f)
            {
                EventBus.Publish(new PlayerDiedEvent());
            }
        }

        public void Heal(float amount)
        {
            if (IsDead) return;
            _currentHP = Mathf.Min(_maxHP, _currentHP + amount);
        }
    }
}
```

- [ ] **Step 3: Set up Player prefab**

1. In GameScene, select the placeholder player sprite
2. Add components: `PlayerController`, `PlayerHealth`, `Rigidbody2D`, `CircleCollider2D`
3. Rigidbody2D: Body Type = Dynamic, Gravity Scale = 0, Freeze Rotation Z = true
4. CircleCollider2D: Radius = 0.4
5. Assign `PlayerConfig` ScriptableObject to both components
6. Tag as "Player"
7. Drag to `Assets/Prefabs/Player/` → creates `Player.prefab`

- [ ] **Step 4: Test in editor**

Play GameScene → WASD should move the player. Verify:
- Smooth movement
- No rotation
- Speed matches config (3 units/sec)

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/Gameplay/Player/ Assets/Prefabs/Player/
git commit -m "feat: add PlayerController (input movement) and PlayerHealth (HP, damage, death)"
```

---

## Task 9: Enemy Base System with Object Pooling

**Files:**
- Create: `Assets/Scripts/Core/GenericObjectPool.cs`
- Create: `Assets/Scripts/Gameplay/Enemies/EnemyBase.cs`
- Create: `Assets/Scripts/Gameplay/Enemies/EnemyHealth.cs`

- [ ] **Step 1: Implement GenericObjectPool**

```csharp
// Assets/Scripts/Core/GenericObjectPool.cs
using System.Collections.Generic;
using UnityEngine;

namespace KokTengri.Core
{
    public class GenericObjectPool : MonoBehaviour
    {
        [SerializeField] private GameObject prefab;
        [SerializeField] private int initialSize = 20;
        [SerializeField] private Transform poolParent;

        private readonly Queue<GameObject> _available = new();

        private void Awake()
        {
            if (poolParent == null)
            {
                poolParent = new GameObject($"Pool_{prefab.name}").transform;
                poolParent.SetParent(transform);
            }

            for (int i = 0; i < initialSize; i++)
                CreateNew();
        }

        private void CreateNew()
        {
            var obj = Instantiate(prefab, poolParent);
            obj.SetActive(false);
            _available.Enqueue(obj);
        }

        public GameObject Get(Vector3 position)
        {
            if (_available.Count == 0)
                CreateNew();

            var obj = _available.Dequeue();
            obj.transform.position = position;
            obj.SetActive(true);
            return obj;
        }

        public void Return(GameObject obj)
        {
            obj.SetActive(false);
            _available.Enqueue(obj);
        }
    }
}
```

- [ ] **Step 2: Implement EnemyBase**

```csharp
// Assets/Scripts/Gameplay/Enemies/EnemyBase.cs
using UnityEngine;
using KokTengri.Core;
using KokTengri.Data;

namespace KokTengri.Gameplay.Enemies
{
    [RequireComponent(typeof(Rigidbody2D), typeof(EnemyHealth))]
    public class EnemyBase : MonoBehaviour
    {
        private EnemyDefinitionSO _definition;
        private Transform _target;
        private Rigidbody2D _rb;
        private float _contactDamage;
        private float _contactCooldown;
        private float _contactTimer;

        public EnemyDefinitionSO Definition => _definition;

        public void Initialize(EnemyDefinitionSO definition, Transform target, float elapsedMinutes)
        {
            _definition = definition;
            _target = target;
            _rb = GetComponent<Rigidbody2D>();
            _rb.gravityScale = 0f;
            _rb.freezeRotation = true;

            _contactDamage = definition.GetDamage(elapsedMinutes);
            _contactCooldown = 0.5f;
            _contactTimer = 0f;

            var health = GetComponent<EnemyHealth>();
            health.Initialize(definition, elapsedMinutes);
        }

        private void FixedUpdate()
        {
            if (_target == null || _definition == null) return;

            Vector2 direction = ((Vector2)_target.position - (Vector2)transform.position).normalized;
            _rb.linearVelocity = direction * _definition.moveSpeed;
        }

        private void Update()
        {
            if (_contactTimer > 0f)
                _contactTimer -= Time.deltaTime;
        }

        private void OnCollisionStay2D(Collision2D collision)
        {
            if (_contactTimer > 0f) return;
            if (!collision.gameObject.CompareTag("Player")) return;

            var playerHealth = collision.gameObject.GetComponent<Player.PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(_contactDamage);
                _contactTimer = _contactCooldown;
            }
        }
    }
}
```

- [ ] **Step 3: Implement EnemyHealth**

```csharp
// Assets/Scripts/Gameplay/Enemies/EnemyHealth.cs
using UnityEngine;
using KokTengri.Core;
using KokTengri.Data;

namespace KokTengri.Gameplay.Enemies
{
    public class EnemyHealth : MonoBehaviour
    {
        private float _currentHP;
        private float _maxHP;
        private EnemyDefinitionSO _definition;
        private GenericObjectPool _pool;

        public float HPPercent => _maxHP > 0 ? _currentHP / _maxHP : 0f;

        public void SetPool(GenericObjectPool pool) => _pool = pool;

        public void Initialize(EnemyDefinitionSO definition, float elapsedMinutes)
        {
            _definition = definition;
            _maxHP = definition.GetHP(elapsedMinutes);
            _currentHP = _maxHP;
        }

        public void TakeDamage(float damage, ElementType spellElement)
        {
            float multiplier = GetElementMultiplier(spellElement);
            float finalDamage = damage * multiplier;
            _currentHP -= finalDamage;

            if (_currentHP <= 0f)
                Die();
        }

        private float GetElementMultiplier(ElementType spellElement)
        {
            if (_definition == null) return 1f;
            if (spellElement == _definition.weakness) return 1.5f;
            if (spellElement == _definition.resistance) return 0.6f;
            return 1f;
        }

        private void Die()
        {
            EventBus.Publish(new EnemyDiedEvent
            {
                XPValue = _definition.xpValue,
                Position = transform.position,
                Weakness = _definition.weakness
            });

            if (_pool != null)
                _pool.Return(gameObject);
            else
                gameObject.SetActive(false);
        }
    }
}
```

- [ ] **Step 4: Write EnemyHealth tests**

```csharp
// Assets/Tests/EditMode/EnemyHealthTests.cs
using NUnit.Framework;
using KokTengri.Core;
using KokTengri.Data;
using UnityEngine;

namespace KokTengri.Tests.EditMode
{
    public class EnemyHealthTests
    {
        [SetUp]
        public void SetUp()
        {
            EventBus.Reset();
        }

        [Test]
        public void Weakness_Deals_150_Percent_Damage()
        {
            var enemy = ScriptableObject.CreateInstance<EnemyDefinitionSO>();
            enemy.baseHP = 100f;
            enemy.weakness = ElementType.Od;
            enemy.resistance = ElementType.Yel;

            // Simulate: 10 base damage * 1.5 weakness = 15 effective
            float multiplier = GetMultiplier(ElementType.Od, enemy);
            Assert.AreEqual(1.5f, multiplier, 0.01f);
        }

        [Test]
        public void Resistance_Deals_60_Percent_Damage()
        {
            var enemy = ScriptableObject.CreateInstance<EnemyDefinitionSO>();
            enemy.weakness = ElementType.Od;
            enemy.resistance = ElementType.Yel;

            float multiplier = GetMultiplier(ElementType.Yel, enemy);
            Assert.AreEqual(0.6f, multiplier, 0.01f);
        }

        [Test]
        public void Neutral_Element_Deals_100_Percent()
        {
            var enemy = ScriptableObject.CreateInstance<EnemyDefinitionSO>();
            enemy.weakness = ElementType.Od;
            enemy.resistance = ElementType.Yel;

            float multiplier = GetMultiplier(ElementType.Sub, enemy);
            Assert.AreEqual(1.0f, multiplier, 0.01f);
        }

        // Helper — mirrors EnemyHealth.GetElementMultiplier logic
        private float GetMultiplier(ElementType spellElement, EnemyDefinitionSO def)
        {
            if (spellElement == def.weakness) return 1.5f;
            if (spellElement == def.resistance) return 0.6f;
            return 1f;
        }
    }
}
```

- [ ] **Step 5: Run tests — verify they pass**

Run: Test Runner → EditMode → EnemyHealthTests
Expected: 3 PASS

- [ ] **Step 6: Create enemy prefabs**

1. Create a 16x16 white square sprite, tint grey
2. Create `KaraKurt.prefab`: Sprite (tinted dark grey), Rigidbody2D (Dynamic, gravity=0, freeze rotation), CircleCollider2D (radius 0.3), EnemyBase, EnemyHealth
3. Create `YekUsagi.prefab`: Same components, larger sprite (24x24), tinted dark green, CircleCollider2D (radius 0.5)
4. Place both in `Assets/Prefabs/Enemies/`

- [ ] **Step 7: Commit**

```bash
git add Assets/Scripts/Core/GenericObjectPool.cs Assets/Scripts/Gameplay/Enemies/ Assets/Tests/EditMode/EnemyHealthTests.cs Assets/Prefabs/Enemies/
git commit -m "feat: add EnemyBase, EnemyHealth with element weakness/resistance, and object pooling"
```

---

## Task 10: Wave Spawner & Enemy Spawning

**Files:**
- Create: `Assets/Scripts/Gameplay/Waves/WaveManager.cs`
- Create: `Assets/Scripts/Gameplay/Enemies/EnemySpawner.cs`

- [ ] **Step 1: Implement EnemySpawner**

```csharp
// Assets/Scripts/Gameplay/Enemies/EnemySpawner.cs
using UnityEngine;
using KokTengri.Core;
using KokTengri.Data;

namespace KokTengri.Gameplay.Enemies
{
    public class EnemySpawner : MonoBehaviour
    {
        [SerializeField] private EnemyDefinitionSO[] enemyDefinitions;
        [SerializeField] private GenericObjectPool[] enemyPools;
        [SerializeField] private WaveConfigSO waveConfig;
        [SerializeField] private Transform playerTransform;

        public void SpawnWave(float elapsedMinutes)
        {
            int count = CalculateSpawnCount(elapsedMinutes);
            for (int i = 0; i < count; i++)
            {
                var def = PickEnemyType(elapsedMinutes);
                if (def == null) continue;

                int poolIndex = GetPoolIndex(def);
                if (poolIndex < 0) continue;

                Vector3 pos = GetSpawnPosition();
                var obj = enemyPools[poolIndex].Get(pos);
                var enemy = obj.GetComponent<EnemyBase>();
                enemy.Initialize(def, playerTransform, elapsedMinutes);

                var health = obj.GetComponent<EnemyHealth>();
                health.SetPool(enemyPools[poolIndex]);
            }
        }

        private int CalculateSpawnCount(float elapsedMinutes)
        {
            float multiplier = 1f + waveConfig.spawnRateIncreasePerMinute * elapsedMinutes;
            return Mathf.RoundToInt(waveConfig.baseEnemiesPerSpawn * multiplier);
        }

        private EnemyDefinitionSO PickEnemyType(float elapsedMinutes)
        {
            // Filter to enemies that have appeared by this time
            var eligible = new System.Collections.Generic.List<EnemyDefinitionSO>();
            foreach (var def in enemyDefinitions)
            {
                if (elapsedMinutes >= def.firstAppearMinute)
                    eligible.Add(def);
            }
            if (eligible.Count == 0) return null;
            return eligible[Random.Range(0, eligible.Count)];
        }

        private int GetPoolIndex(EnemyDefinitionSO def)
        {
            for (int i = 0; i < enemyDefinitions.Length; i++)
                if (enemyDefinitions[i] == def) return i;
            return -1;
        }

        private Vector3 GetSpawnPosition()
        {
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float radius = waveConfig.spawnRadius;
            Vector3 offset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * radius;
            return playerTransform.position + offset;
        }
    }
}
```

- [ ] **Step 2: Implement WaveManager**

```csharp
// Assets/Scripts/Gameplay/Waves/WaveManager.cs
using UnityEngine;
using KokTengri.Core;
using KokTengri.Data;
using KokTengri.Gameplay.Enemies;

namespace KokTengri.Gameplay.Waves
{
    public class WaveManager : MonoBehaviour
    {
        [SerializeField] private WaveConfigSO waveConfig;
        [SerializeField] private EnemySpawner enemySpawner;

        private float _elapsedTime;
        private float _spawnTimer;
        private bool _isRunning;

        public float ElapsedMinutes => _elapsedTime / 60f;
        public float ElapsedSeconds => _elapsedTime;

        public void StartWaves()
        {
            _elapsedTime = 0f;
            _spawnTimer = 0f;
            _isRunning = true;
        }

        public void StopWaves()
        {
            _isRunning = false;
        }

        private void Update()
        {
            if (!_isRunning) return;

            _elapsedTime += Time.deltaTime;
            _spawnTimer += Time.deltaTime;

            EventBus.Publish(new RunTimerTickEvent { ElapsedSeconds = _elapsedTime });

            float currentInterval = GetCurrentSpawnInterval();
            if (_spawnTimer >= currentInterval)
            {
                _spawnTimer = 0f;
                enemySpawner.SpawnWave(ElapsedMinutes);
            }
        }

        private float GetCurrentSpawnInterval()
        {
            float reduction = 1f + waveConfig.spawnRateIncreasePerMinute * ElapsedMinutes;
            float interval = waveConfig.baseSpawnInterval / reduction;
            return Mathf.Max(interval, waveConfig.minSpawnInterval);
        }
    }
}
```

- [ ] **Step 3: Wire up in GameScene**

1. Create empty GameObject `WaveSystem` in GameScene
2. Add `WaveManager` component → assign WaveConfig SO
3. Create child `EnemySpawner` → assign WaveConfig SO, Player transform
4. Create child `Pool_KaraKurt` with `GenericObjectPool` → assign KaraKurt prefab, initialSize=30
5. Create child `Pool_YekUsagi` with `GenericObjectPool` → assign YekUsagi prefab, initialSize=15
6. On EnemySpawner: assign enemyDefinitions array [KaraKurt, YekUsagi], enemyPools array [Pool_KaraKurt, Pool_YekUsagi]

- [ ] **Step 4: Test in editor**

Play → enemies should spawn around the player and move toward them. Verify:
- Kara Kurtlar appear from minute 0 (fast, small)
- Yek Uşakları appear from minute 2 (slow, large)
- Spawn rate increases over time
- Enemies cause contact damage

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/Gameplay/Waves/ Assets/Scripts/Gameplay/Enemies/EnemySpawner.cs
git commit -m "feat: add WaveManager and EnemySpawner with time-based difficulty scaling"
```

---

## Task 11: Spell Effects — Orbit & Projectile

**Files:**
- Create: `Assets/Scripts/Gameplay/Spells/Effects/SpellEffectBase.cs`
- Create: `Assets/Scripts/Gameplay/Spells/Effects/OrbitSpellEffect.cs`
- Create: `Assets/Scripts/Gameplay/Spells/Effects/ProjectileSpellEffect.cs`

- [ ] **Step 1: Implement SpellEffectBase**

```csharp
// Assets/Scripts/Gameplay/Spells/Effects/SpellEffectBase.cs
using UnityEngine;
using KokTengri.Data;
using KokTengri.Gameplay.Enemies;

namespace KokTengri.Gameplay.Spells.Effects
{
    public abstract class SpellEffectBase : MonoBehaviour
    {
        protected SpellDefinitionSO _definition;
        protected int _level = 1;
        protected float _classBonus = 1f;
        protected Transform _playerTransform;

        public string SpellId => _definition != null ? _definition.spellId : "";

        public virtual void Initialize(SpellDefinitionSO definition, Transform player, int level, float classBonus)
        {
            _definition = definition;
            _playerTransform = player;
            _level = level;
            _classBonus = classBonus;
        }

        public virtual void SetLevel(int level)
        {
            _level = level;
        }

        protected void DealDamage(GameObject target)
        {
            var enemyHealth = target.GetComponent<EnemyHealth>();
            if (enemyHealth == null) return;

            // Determine which elements this spell uses for weakness/resistance
            ElementType primaryElement = _definition.elementA;
            float damage = _definition.CalculateDamage(_level, 1f, _classBonus);

            // The EnemyHealth.TakeDamage handles the element multiplier internally
            enemyHealth.TakeDamage(damage, primaryElement);
        }
    }
}
```

- [ ] **Step 2: Implement OrbitSpellEffect**

```csharp
// Assets/Scripts/Gameplay/Spells/Effects/OrbitSpellEffect.cs
using UnityEngine;
using KokTengri.Data;

namespace KokTengri.Gameplay.Spells.Effects
{
    public class OrbitSpellEffect : SpellEffectBase
    {
        [SerializeField] private SpriteRenderer spriteRenderer;

        private float _angle;
        private float _tickTimer;
        private int _orbitIndex;
        private int _totalOrbiting;

        public void SetOrbitPosition(int index, int total)
        {
            _orbitIndex = index;
            _totalOrbiting = total;
            _angle = (360f / total) * index;
        }

        public override void Initialize(SpellDefinitionSO definition, Transform player, int level, float classBonus)
        {
            base.Initialize(definition, player, level, classBonus);
            if (spriteRenderer != null)
                spriteRenderer.color = definition.spellColor;
        }

        private void Update()
        {
            if (_playerTransform == null || _definition == null) return;

            // Orbit around player
            float speed = 180f; // degrees per second
            _angle += speed * Time.deltaTime;
            if (_angle > 360f) _angle -= 360f;

            float radius = _definition.orbitRadius + (_level - 1) * 0.2f; // grows with level
            float rad = _angle * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f) * radius;
            transform.position = _playerTransform.position + offset;
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            if (!other.CompareTag("Enemy")) return;

            _tickTimer -= Time.deltaTime;
            if (_tickTimer <= 0f)
            {
                DealDamage(other.gameObject);
                _tickTimer = _definition.tickRate;
            }
        }
    }
}
```

- [ ] **Step 3: Implement ProjectileSpellEffect**

```csharp
// Assets/Scripts/Gameplay/Spells/Effects/ProjectileSpellEffect.cs
using UnityEngine;
using KokTengri.Data;

namespace KokTengri.Gameplay.Spells.Effects
{
    public class ProjectileSpellEffect : SpellEffectBase
    {
        [SerializeField] private GameObject projectilePrefab;
        [SerializeField] private KokTengri.Core.GenericObjectPool projectilePool;

        private float _fireTimer;

        private void Update()
        {
            if (_playerTransform == null || _definition == null) return;

            _fireTimer += Time.deltaTime;
            float fireRate = _definition.tickRate / (1f + 0.15f * (_level - 1)); // faster at higher levels

            if (_fireTimer >= fireRate)
            {
                _fireTimer = 0f;
                FireProjectile();
            }
        }

        private void FireProjectile()
        {
            // Find nearest enemy
            var enemies = Physics2D.OverlapCircleAll(_playerTransform.position, _definition.range * 2f);
            Transform nearest = null;
            float nearestDist = float.MaxValue;

            foreach (var col in enemies)
            {
                if (!col.CompareTag("Enemy")) continue;
                float dist = Vector2.Distance(_playerTransform.position, col.transform.position);
                if (dist < nearestDist)
                {
                    nearestDist = dist;
                    nearest = col.transform;
                }
            }

            if (nearest == null) return;

            Vector2 direction = ((Vector2)nearest.position - (Vector2)_playerTransform.position).normalized;
            int count = _definition.projectileCount + (_level - 1); // +1 projectile per level

            for (int i = 0; i < count; i++)
            {
                float spreadAngle = (i - (count - 1) / 2f) * 15f; // 15 degree spread
                Vector2 dir = RotateVector(direction, spreadAngle);

                GameObject proj;
                if (projectilePool != null)
                    proj = projectilePool.Get(_playerTransform.position);
                else
                    proj = Instantiate(projectilePrefab, _playerTransform.position, Quaternion.identity);

                var bullet = proj.GetComponent<SpellProjectile>();
                if (bullet != null)
                    bullet.Launch(dir, _definition, _level, _classBonus);
            }
        }

        private Vector2 RotateVector(Vector2 v, float degrees)
        {
            float rad = degrees * Mathf.Deg2Rad;
            float cos = Mathf.Cos(rad);
            float sin = Mathf.Sin(rad);
            return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
        }
    }

    /// <summary>
    /// Attached to each projectile GameObject.
    /// </summary>
    public class SpellProjectile : MonoBehaviour
    {
        private Vector2 _direction;
        private SpellDefinitionSO _definition;
        private int _level;
        private float _classBonus;
        private float _lifetime = 3f;
        private float _timer;

        public void Launch(Vector2 direction, SpellDefinitionSO definition, int level, float classBonus)
        {
            _direction = direction;
            _definition = definition;
            _level = level;
            _classBonus = classBonus;
            _timer = 0f;

            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        private void Update()
        {
            transform.position += (Vector3)_direction * _definition.speed * Time.deltaTime;
            _timer += Time.deltaTime;
            if (_timer >= _lifetime)
                gameObject.SetActive(false);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Enemy")) return;

            var enemyHealth = other.GetComponent<Enemies.EnemyHealth>();
            if (enemyHealth != null)
            {
                float damage = _definition.CalculateDamage(_level, 1f, _classBonus);
                enemyHealth.TakeDamage(damage, _definition.elementA);
            }

            gameObject.SetActive(false); // destroy on hit
        }
    }
}
```

- [ ] **Step 4: Create spell prefabs**

1. **AlevHalkasi.prefab**: Small circle sprite (red tint), CircleCollider2D (trigger), OrbitSpellEffect, SpriteRenderer
2. **KayaKalkani.prefab**: Small square sprite (brown tint), CircleCollider2D (trigger), OrbitSpellEffect, SpriteRenderer
3. **KilicFirtinasi.prefab**: Empty root with ProjectileSpellEffect → child projectile prefab (small rectangle, orange tint, CircleCollider2D trigger, SpellProjectile)
4. Create projectile pool for Kılıç Fırtınası
5. Save all to `Assets/Prefabs/Spells/`
6. Assign prefabs to corresponding SpellDefinitionSO assets

- [ ] **Step 5: Test in editor**

Manually trigger spell creation in code or inspector. Verify:
- Orbit spells circle around player
- Projectile spells fire at nearest enemy
- Enemies take damage and die
- XP gems conceptually drop (EnemyDiedEvent fires)

- [ ] **Step 6: Commit**

```bash
git add Assets/Scripts/Gameplay/Spells/Effects/ Assets/Prefabs/Spells/
git commit -m "feat: add orbit and projectile spell effects with damage and level scaling"
```

---

## Task 12: XP Collection & Level-Up Flow

**Files:**
- Create: `Assets/Scripts/Gameplay/Player/XPCollector.cs`
- Create: `Assets/Scripts/Gameplay/Pickups/XPGem.cs`

- [ ] **Step 1: Implement XPCollector**

```csharp
// Assets/Scripts/Gameplay/Player/XPCollector.cs
using UnityEngine;
using KokTengri.Core;
using KokTengri.Data;

namespace KokTengri.Gameplay.Player
{
    public class XPCollector : MonoBehaviour
    {
        [SerializeField] private LevelUpConfigSO levelUpConfig;
        [SerializeField] private PlayerConfigSO playerConfig;

        private int _currentXP;
        private int _currentLevel = 1;
        private int _xpToNextLevel;

        public int CurrentLevel => _currentLevel;
        public int CurrentXP => _currentXP;
        public int XPToNextLevel => _xpToNextLevel;
        public float XPPercent => _xpToNextLevel > 0 ? (float)_currentXP / _xpToNextLevel : 0f;

        private void Awake()
        {
            _xpToNextLevel = levelUpConfig.GetXPForLevel(_currentLevel);
        }

        private void OnEnable()
        {
            EventBus.Subscribe<EnemyDiedEvent>(OnEnemyDied);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<EnemyDiedEvent>(OnEnemyDied);
        }

        private void OnEnemyDied(EnemyDiedEvent evt)
        {
            // Spawn XP gem at enemy position (handled by separate spawner)
            // For now, directly add XP
            AddXP(evt.XPValue);
        }

        public void AddXP(int amount)
        {
            _currentXP += amount;

            EventBus.Publish(new XPCollectedEvent
            {
                Amount = amount,
                CurrentXP = _currentXP,
                XPToNextLevel = _xpToNextLevel
            });

            while (_currentXP >= _xpToNextLevel)
            {
                _currentXP -= _xpToNextLevel;
                _currentLevel++;
                _xpToNextLevel = levelUpConfig.GetXPForLevel(_currentLevel);

                EventBus.Publish(new PlayerLeveledUpEvent { NewLevel = _currentLevel });
            }
        }
    }
}
```

- [ ] **Step 2: Implement XPGem pickup**

```csharp
// Assets/Scripts/Gameplay/Pickups/XPGem.cs
using UnityEngine;
using KokTengri.Core;

namespace KokTengri.Gameplay.Pickups
{
    public class XPGem : MonoBehaviour
    {
        private int _xpValue;
        private Transform _target;
        private float _pickupRadius = 1.5f;
        private float _magnetSpeed = 8f;
        private bool _magnetized;
        private GenericObjectPool _pool;

        public void Initialize(int xpValue, GenericObjectPool pool, float pickupRadius)
        {
            _xpValue = xpValue;
            _pool = pool;
            _pickupRadius = pickupRadius;
            _magnetized = false;
            _target = GameObject.FindGameObjectWithTag("Player")?.transform;
        }

        private void Update()
        {
            if (_target == null) return;

            float dist = Vector2.Distance(transform.position, _target.position);

            if (dist <= _pickupRadius)
                _magnetized = true;

            if (_magnetized)
            {
                transform.position = Vector3.MoveTowards(
                    transform.position, _target.position, _magnetSpeed * Time.deltaTime);

                if (dist < 0.2f)
                {
                    var xpCollector = _target.GetComponent<Player.XPCollector>();
                    if (xpCollector != null)
                        xpCollector.AddXP(_xpValue);

                    if (_pool != null)
                        _pool.Return(gameObject);
                    else
                        gameObject.SetActive(false);
                }
            }
        }
    }
}
```

- [ ] **Step 3: Create XPGem prefab**

1. Small diamond sprite (green tint, 8x8 pixels)
2. Add XPGem component
3. No collider needed (distance-based pickup)
4. Save to `Assets/Prefabs/Pickups/XPGem.prefab`

- [ ] **Step 4: Wire XP gem spawning to enemy death**

Update EnemyHealth.Die() to also spawn XP gem, or create a separate XPGemSpawner that listens to EnemyDiedEvent. For simplicity, add spawning in a new component:

```csharp
// Add to GameScene as a singleton-like manager
// Assets/Scripts/Gameplay/Pickups/XPGemSpawner.cs
using UnityEngine;
using KokTengri.Core;
using KokTengri.Data;

namespace KokTengri.Gameplay.Pickups
{
    public class XPGemSpawner : MonoBehaviour
    {
        [SerializeField] private GenericObjectPool xpGemPool;
        [SerializeField] private PlayerConfigSO playerConfig;

        private void OnEnable()
        {
            EventBus.Subscribe<EnemyDiedEvent>(OnEnemyDied);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<EnemyDiedEvent>(OnEnemyDied);
        }

        private void OnEnemyDied(EnemyDiedEvent evt)
        {
            var gem = xpGemPool.Get(evt.Position);
            var xpGem = gem.GetComponent<XPGem>();
            xpGem.Initialize(evt.XPValue, xpGemPool, playerConfig.pickupRadius);
        }
    }
}
```

- [ ] **Step 5: Test in editor**

Play → kill enemies → green XP gems drop → gems fly to player when close → level counter increases.

- [ ] **Step 6: Commit**

```bash
git add Assets/Scripts/Gameplay/Player/XPCollector.cs Assets/Scripts/Gameplay/Pickups/ Assets/Prefabs/Pickups/
git commit -m "feat: add XP collection from enemy deaths with magnet pickup and level-up events"
```

---

## Task 13: Level-Up Popup — Element Selection UI

**Files:**
- Create: `Assets/Scripts/UI/Popups/LevelUpPopup.cs`

- [ ] **Step 1: Implement LevelUpPopup**

```csharp
// Assets/Scripts/UI/Popups/LevelUpPopup.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using KokTengri.Core;
using KokTengri.Data;
using KokTengri.Gameplay.Spells;

namespace KokTengri.UI.Popups
{
    public class LevelUpPopup : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameObject panel;
        [SerializeField] private Button[] elementButtons;       // 3 buttons (+ 1 hidden for Kam)
        [SerializeField] private TextMeshProUGUI[] buttonLabels;
        [SerializeField] private TextMeshProUGUI[] tooltipLabels;
        [SerializeField] private Button rerollButton;
        [SerializeField] private TextMeshProUGUI levelText;

        [Header("Config")]
        [SerializeField] private LevelUpConfigSO levelUpConfig;

        private ElementType[] _currentChoices;
        private ElementInventory _inventory;
        private SpellSlotManager _slots;
        private SpellCrafter _crafter;
        private bool _rerollUsed;

        private static readonly Color[] ElementColors = {
            new Color(1f, 0.3f, 0.2f),    // Od - Red
            new Color(0.2f, 0.5f, 1f),    // Sub - Blue
            new Color(0.6f, 0.4f, 0.2f),  // Yer - Brown
            new Color(0.9f, 0.9f, 1f),    // Yel - White
            new Color(0.5f, 0.5f, 0.6f),  // Temür - Grey
        };

        private static readonly string[] ElementNames = {
            "Od (Ateş)", "Sub (Su)", "Yer (Toprak)", "Yel (Hava)", "Temür (Demir)"
        };

        public void Initialize(ElementInventory inventory, SpellSlotManager slots, SpellCrafter crafter)
        {
            _inventory = inventory;
            _slots = slots;
            _crafter = crafter;
        }

        private void OnEnable()
        {
            EventBus.Subscribe<PlayerLeveledUpEvent>(OnLevelUp);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<PlayerLeveledUpEvent>(OnLevelUp);
        }

        private void OnLevelUp(PlayerLeveledUpEvent evt)
        {
            Show(evt.NewLevel);
        }

        public void Show(int newLevel)
        {
            Time.timeScale = 0f; // Pause game
            panel.SetActive(true);
            levelText.text = $"Seviye {newLevel}";
            _rerollUsed = false;
            rerollButton.interactable = true;

            GenerateChoices();
        }

        private void GenerateChoices()
        {
            int count = levelUpConfig.elementChoices;
            _currentChoices = new ElementType[count];

            var allElements = (ElementType[])System.Enum.GetValues(typeof(ElementType));

            for (int i = 0; i < count; i++)
            {
                _currentChoices[i] = allElements[Random.Range(0, allElements.Length)];

                // Show button
                elementButtons[i].gameObject.SetActive(true);
                elementButtons[i].GetComponent<Image>().color = ElementColors[(int)_currentChoices[i]];
                buttonLabels[i].text = ElementNames[(int)_currentChoices[i]];

                // Generate tooltip — what will happen if you pick this?
                tooltipLabels[i].text = GetTooltip(_currentChoices[i]);

                int index = i; // capture for lambda
                elementButtons[i].onClick.RemoveAllListeners();
                elementButtons[i].onClick.AddListener(() => OnElementSelected(index));
            }

            // Hide extra buttons
            for (int i = count; i < elementButtons.Length; i++)
                elementButtons[i].gameObject.SetActive(false);

            rerollButton.onClick.RemoveAllListeners();
            rerollButton.onClick.AddListener(OnReroll);
        }

        private string GetTooltip(ElementType element)
        {
            // Check if it matches something in inventory
            var recipe = _inventory.FindMatch(element);
            if (recipe != null)
            {
                int existingIndex = _slots.FindSpellIndex(recipe.Value.SpellId);
                if (existingIndex >= 0)
                {
                    var spell = _slots.GetSpell(existingIndex);
                    if (spell.Level < 5)
                        return $"{recipe.Value.DisplayName} Seviye {spell.Level} → {spell.Level + 1}";
                    else
                        return $"{recipe.Value.DisplayName} MAX — Envantere eklenir";
                }
                return $"→ {recipe.Value.DisplayName} oluşacak!";
            }
            return "Envantere eklenir";
        }

        private void OnElementSelected(int index)
        {
            ElementType chosen = _currentChoices[index];
            _crafter.AddElement(chosen);

            EventBus.Publish(new ElementSelectedEvent { Element = chosen });

            Hide();
        }

        private void OnReroll()
        {
            if (_rerollUsed) return;
            _rerollUsed = true;
            rerollButton.interactable = false;
            GenerateChoices();
        }

        private void Hide()
        {
            panel.SetActive(false);
            Time.timeScale = 1f; // Resume game
        }
    }
}
```

- [ ] **Step 2: Build UI in GameScene**

1. Create Canvas (Screen Space - Overlay)
2. Create child panel `LevelUpPanel` (centered, dark semi-transparent background)
3. Add `TextMeshProUGUI` header: "Seviye X"
4. Add 3 Button objects as element choices (colored, with text label + tooltip text below)
5. Add 1 "Yeniden Çek" (Re-roll) button at bottom
6. Add `LevelUpPopup` component to panel, wire all references
7. Start with panel disabled

- [ ] **Step 3: Test in editor**

Play → gain XP → level up → popup appears → game pauses → select element → tooltip shows what spell will be created → game resumes

- [ ] **Step 4: Commit**

```bash
git add Assets/Scripts/UI/Popups/LevelUpPopup.cs
git commit -m "feat: add level-up popup with element selection, recipe tooltips, and re-roll"
```

---

## Task 14: Basic HUD

**Files:**
- Create: `Assets/Scripts/UI/HUD/HealthBarUI.cs`
- Create: `Assets/Scripts/UI/HUD/SpellSlotsUI.cs`
- Create: `Assets/Scripts/UI/HUD/ElementInventoryUI.cs`
- Create: `Assets/Scripts/UI/HUD/RunTimerUI.cs`

- [ ] **Step 1: Implement HUD components**

```csharp
// Assets/Scripts/UI/HUD/HealthBarUI.cs
using UnityEngine;
using UnityEngine.UI;
using KokTengri.Core;

namespace KokTengri.UI.HUD
{
    public class HealthBarUI : MonoBehaviour
    {
        [SerializeField] private Image fillImage;
        [SerializeField] private Color fullColor = Color.green;
        [SerializeField] private Color lowColor = Color.red;

        private void OnEnable()
        {
            EventBus.Subscribe<PlayerDamagedEvent>(OnDamaged);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<PlayerDamagedEvent>(OnDamaged);
        }

        private void OnDamaged(PlayerDamagedEvent evt)
        {
            float percent = evt.MaxHP > 0 ? evt.CurrentHP / evt.MaxHP : 0f;
            fillImage.fillAmount = percent;
            fillImage.color = Color.Lerp(lowColor, fullColor, percent);
        }
    }
}
```

```csharp
// Assets/Scripts/UI/HUD/RunTimerUI.cs
using UnityEngine;
using TMPro;
using KokTengri.Core;

namespace KokTengri.UI.HUD
{
    public class RunTimerUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI timerText;

        private void OnEnable()
        {
            EventBus.Subscribe<RunTimerTickEvent>(OnTick);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<RunTimerTickEvent>(OnTick);
        }

        private void OnTick(RunTimerTickEvent evt)
        {
            int minutes = (int)(evt.ElapsedSeconds / 60f);
            int seconds = (int)(evt.ElapsedSeconds % 60f);
            timerText.text = $"{minutes:00}:{seconds:00}";
        }
    }
}
```

```csharp
// Assets/Scripts/UI/HUD/SpellSlotsUI.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using KokTengri.Core;
using KokTengri.Gameplay.Spells;

namespace KokTengri.UI.HUD
{
    public class SpellSlotsUI : MonoBehaviour
    {
        [SerializeField] private Image[] slotIcons;          // 6 slots
        [SerializeField] private TextMeshProUGUI[] levelTexts; // "Lv.3" labels

        private SpellSlotManager _slots;

        public void Initialize(SpellSlotManager slots)
        {
            _slots = slots;
            RefreshAll();
        }

        private void OnEnable()
        {
            EventBus.Subscribe<SpellCreatedEvent>(_ => RefreshAll());
            EventBus.Subscribe<SpellUpgradedEvent>(_ => RefreshAll());
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<SpellCreatedEvent>(_ => RefreshAll());
            EventBus.Unsubscribe<SpellUpgradedEvent>(_ => RefreshAll());
        }

        private void RefreshAll()
        {
            if (_slots == null) return;

            for (int i = 0; i < slotIcons.Length; i++)
            {
                if (i < _slots.ActiveCount)
                {
                    var spell = _slots.GetSpell(i);
                    slotIcons[i].gameObject.SetActive(true);
                    levelTexts[i].text = $"Lv.{spell.Level}";
                }
                else
                {
                    slotIcons[i].gameObject.SetActive(false);
                    levelTexts[i].text = "";
                }
            }
        }
    }
}
```

```csharp
// Assets/Scripts/UI/HUD/ElementInventoryUI.cs
using UnityEngine;
using UnityEngine.UI;
using KokTengri.Data;
using KokTengri.Core;
using KokTengri.Gameplay.Spells;

namespace KokTengri.UI.HUD
{
    public class ElementInventoryUI : MonoBehaviour
    {
        [SerializeField] private Image[] inventorySlots;  // 3 slots

        private static readonly Color[] ElementColors = {
            new Color(1f, 0.3f, 0.2f),    // Od
            new Color(0.2f, 0.5f, 1f),    // Sub
            new Color(0.6f, 0.4f, 0.2f),  // Yer
            new Color(0.9f, 0.9f, 1f),    // Yel
            new Color(0.5f, 0.5f, 0.6f),  // Temür
        };

        private ElementInventory _inventory;

        public void Initialize(ElementInventory inventory)
        {
            _inventory = inventory;
        }

        private void OnEnable()
        {
            EventBus.Subscribe<ElementSelectedEvent>(_ => Refresh());
            EventBus.Subscribe<SpellCreatedEvent>(_ => Refresh());
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<ElementSelectedEvent>(_ => Refresh());
            EventBus.Unsubscribe<SpellCreatedEvent>(_ => Refresh());
        }

        private void Refresh()
        {
            if (_inventory == null) return;

            for (int i = 0; i < inventorySlots.Length; i++)
            {
                if (i < _inventory.Count)
                {
                    inventorySlots[i].gameObject.SetActive(true);
                    inventorySlots[i].color = ElementColors[(int)_inventory.Elements[i]];
                }
                else
                {
                    inventorySlots[i].gameObject.SetActive(false);
                }
            }
        }
    }
}
```

- [ ] **Step 2: Build HUD layout in GameScene**

On the existing Canvas:
1. Top-left: Health bar (Image fill, 200x20px)
2. Top-center: Timer text ("00:00")
3. Bottom-left: 3 small element inventory squares (32x32)
4. Bottom-center: 6 spell slot icons (40x40 each, horizontal layout)
5. Wire all component references

- [ ] **Step 3: Test in editor**

Play → HP bar decreases when hit → timer counts up → element inventory shows collected elements → spell slots fill as spells are crafted

- [ ] **Step 4: Commit**

```bash
git add Assets/Scripts/UI/HUD/
git commit -m "feat: add HUD — health bar, run timer, element inventory, spell slots"
```

---

## Task 15: RunManager — Game Flow Controller

**Files:**
- Create: `Assets/Scripts/Core/RunManager.cs`
- Create: `Assets/Scripts/UI/Popups/RunEndPopup.cs`

- [ ] **Step 1: Implement RunManager**

```csharp
// Assets/Scripts/Core/RunManager.cs
using UnityEngine;
using KokTengri.Data;
using KokTengri.Gameplay.Spells;
using KokTengri.Gameplay.Waves;

namespace KokTengri.Core
{
    public class RunManager : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField] private PlayerConfigSO playerConfig;
        [SerializeField] private WaveConfigSO waveConfig;

        [Header("References")]
        [SerializeField] private WaveManager waveManager;
        [SerializeField] private GameObject playerObject;

        // Owned systems (non-MonoBehaviour)
        private ElementInventory _elementInventory;
        private SpellSlotManager _spellSlots;
        private SpellCrafter _spellCrafter;

        // Run stats
        private int _enemiesKilled;
        private int _bossesKilled;
        private bool _isRunning;

        public ElementInventory ElementInventory => _elementInventory;
        public SpellSlotManager SpellSlots => _spellSlots;
        public SpellCrafter SpellCrafter => _spellCrafter;

        private void Start()
        {
            StartRun();
        }

        public void StartRun()
        {
            // Initialize systems
            _elementInventory = new ElementInventory(playerConfig.maxElementSlots);
            _spellSlots = new SpellSlotManager(playerConfig.maxSpellSlots, playerConfig.maxSpellLevel);
            _spellCrafter = new SpellCrafter(_elementInventory, _spellSlots);

            _enemiesKilled = 0;
            _bossesKilled = 0;
            _isRunning = true;

            // Wire up UI (find and initialize UI components)
            var levelUpPopup = FindAnyObjectByType<UI.Popups.LevelUpPopup>();
            if (levelUpPopup != null)
                levelUpPopup.Initialize(_elementInventory, _spellSlots, _spellCrafter);

            var spellSlotsUI = FindAnyObjectByType<UI.HUD.SpellSlotsUI>();
            if (spellSlotsUI != null)
                spellSlotsUI.Initialize(_spellSlots);

            var elementInventoryUI = FindAnyObjectByType<UI.HUD.ElementInventoryUI>();
            if (elementInventoryUI != null)
                elementInventoryUI.Initialize(_elementInventory);

            // Subscribe to events
            EventBus.Subscribe<EnemyDiedEvent>(OnEnemyDied);
            EventBus.Subscribe<PlayerDiedEvent>(OnPlayerDied);

            // Start waves
            waveManager.StartWaves();

            EventBus.Publish(new RunStartedEvent());
        }

        private void Update()
        {
            if (!_isRunning) return;

            // Check 30-minute time limit
            if (waveManager.ElapsedSeconds >= waveConfig.runDurationSeconds)
            {
                EndRun(completed: true);
            }
        }

        private void OnEnemyDied(EnemyDiedEvent evt)
        {
            _enemiesKilled++;
        }

        private void OnPlayerDied(PlayerDiedEvent evt)
        {
            EndRun(completed: false);
        }

        private void EndRun(bool completed)
        {
            if (!_isRunning) return;
            _isRunning = false;

            waveManager.StopWaves();

            float minutes = waveManager.ElapsedSeconds / 60f;
            EventBus.Publish(new RunEndedEvent
            {
                SurvivedMinutes = minutes,
                EnemiesKilled = _enemiesKilled,
                BossesKilled = _bossesKilled,
                Completed = completed
            });

            // Show run end popup
            var popup = FindAnyObjectByType<UI.Popups.RunEndPopup>();
            if (popup != null)
                popup.Show(minutes, _enemiesKilled, _bossesKilled, completed);

            // Unsubscribe
            EventBus.Unsubscribe<EnemyDiedEvent>(OnEnemyDied);
            EventBus.Unsubscribe<PlayerDiedEvent>(OnPlayerDied);
        }
    }
}
```

- [ ] **Step 2: Implement RunEndPopup**

```csharp
// Assets/Scripts/UI/Popups/RunEndPopup.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace KokTengri.UI.Popups
{
    public class RunEndPopup : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI timeText;
        [SerializeField] private TextMeshProUGUI killsText;
        [SerializeField] private TextMeshProUGUI goldText;
        [SerializeField] private Button retryButton;

        public void Show(float survivedMinutes, int kills, int bossKills, bool completed)
        {
            Time.timeScale = 0f;
            panel.SetActive(true);

            titleText.text = completed ? "DESTAN TAMAMLANDI!" : "DÜŞTÜN!";

            int minutes = (int)survivedMinutes;
            int seconds = (int)((survivedMinutes - minutes) * 60f);
            timeText.text = $"Süre: {minutes:00}:{seconds:00}";
            killsText.text = $"Düşman: {kills}";

            // Gold formula: (minutes * 10) + (kills * 0.5) + (bossKills * 100)
            int gold = (int)(survivedMinutes * 10f) + (int)(kills * 0.5f) + bossKills * 100;
            goldText.text = $"Altın: {gold}";

            retryButton.onClick.RemoveAllListeners();
            retryButton.onClick.AddListener(() =>
            {
                Time.timeScale = 1f;
                UnityEngine.SceneManagement.SceneManager.LoadScene(
                    UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
            });
        }
    }
}
```

- [ ] **Step 3: Build RunEnd popup UI**

1. Create panel under Canvas: dark background, centered
2. Title text (large), time text, kills text, gold text
3. "Tekrar Dene" button
4. Add RunEndPopup component, wire references
5. Start disabled

- [ ] **Step 4: Wire RunManager into GameScene**

1. Create empty `RunManager` object in scene root
2. Add `RunManager` component
3. Assign: PlayerConfig, WaveConfig, WaveManager, Player object
4. RunManager.Start() bootstraps the entire run

- [ ] **Step 5: Full integration test**

Play → Verify full flow:
1. Player moves with WASD
2. Enemies spawn and chase player
3. Enemies deal contact damage → HP bar decreases
4. Killing enemies drops XP gems → player collects → XP increases
5. Level up → popup appears → select element → tooltip shows
6. Element goes to inventory → second matching element creates spell
7. Spell orbits/fires → damages enemies
8. Timer counts up
9. Death → run end popup with stats
10. Retry restarts scene

- [ ] **Step 6: Commit**

```bash
git add Assets/Scripts/Core/RunManager.cs Assets/Scripts/UI/Popups/RunEndPopup.cs
git commit -m "feat: add RunManager game flow and RunEndPopup — Phase 1 core loop complete"
```

---

## Phase 1 Complete — What's Next

Phase 1 delivers a **playable core loop** in Unity Editor. What remains for Phases 2-4:

**Phase 2 (Month 2):** Remaining 12 spell effects, 2 more enemy types (Albastı, Çor), 3 bosses (Tepegöz, Yer Tanrısı, Erlik Han'ın Elçisi), polished level-up UI with recipe tooltips, elite enemies

**Phase 3 (Month 3):** Hero system (4 heroes), class system (Kam, Batur), meta-progression (persistent upgrades, save system), main menu, hero selection screen, economy (gold/ruh taşı)

**Phase 4 (Month 4):** Mobile touch input (on-screen joystick), IAP integration, balance pass, performance optimization (300+ enemies at 60fps), analytics, soft launch build

Each phase will get its own detailed implementation plan when the previous phase is complete.
