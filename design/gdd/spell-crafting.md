# Spell Crafting System

> **Status**: Approved
> **Author**: zbrave + game-designer
> **Last Updated**: 2026-04-04
> **Implements Pillar**: Every Element Matters, Build Diversity Over Build Power, Discovery

## Overview

Spell Crafting is Kök Tengri's core differentiator system: during each run, the player does not pick finished spells directly, but picks elements (Od, Sub, Yer, Yel, Temür) on level-up, and the game automatically crafts or upgrades spells when recipes are matched. The player manages a small 3-slot Element Inventory while up to 6 crafted spells live in separate Spell Slots, creating a high-clarity loop of prediction, planning, and payoff. This exists to turn each level-up into a strategic crafting decision instead of a generic "choose weapon" choice, directly serving discovery and long-term mastery across runs.

## Player Fantasy

The player should feel like a mythic Tengri shaman who understands the language of the elements and bends that knowledge into power mid-battle. Early runs create discovery tension ("What does Od + Temür make?"), while later runs reward mastery (deliberately routing into specific recipes and upgrade paths). The system reinforces **Every Element Matters** by ensuring all five elements participate in viable outcomes, and reinforces **Build Diversity Over Build Power** by rewarding different combinations rather than one dominant path. Each level-up should feel like a meaningful tactical moment where knowledge is the real progression.

## Detailed Design

### Detailed Rules

#### 1) Core Data Model

- There are two separate runtime containers:
  - **Element Inventory**: max 3 slots, stores raw elements, does not consume spell slots.
  - **Spell Slots**: max 6 slots, stores crafted spells and their levels.
- Element types: `Od`, `Sub`, `Yer`, `Yel`, `Temür`.
- Spell recipes:
  - Same element x2 => Basic Spell (5 total).
  - Different element pair => Combined Spell (10 total).
- Spell levels: min 1, max 5.

#### 2) Crafting Flow (authoritative runtime sequence)

1. On level-up, player is offered **3 random element choices** and gets **1 free re-roll**.
2. Selected element is tentatively processed by Spell Crafting.
3. Spell Crafting evaluates outcomes in this priority order:
   1. **Upgrade check**: if the selected element completes a recipe for an already-owned spell, upgrade that spell by +1 (up to level 5).
   2. **New craft check**: else, if selected element plus inventory yields a valid recipe and there is a free spell slot, craft new spell at level 1.
   3. **Blocked craft check**: if recipe exists but spell slots are full (6/6), do not craft; keep element in inventory and show warning.
   4. **No recipe**: add element to inventory if there is room.
4. When a recipe is consumed for craft/upgrade, both participating elements are consumed from inventory flow:
   - one is the newly selected element,
   - one is the matched inventory element.
5. If Element Inventory is full (3/3) and contains no immediate productive match path, next level-up includes **"Discard 1 element from inventory"** option.

#### 3) Spell Slot Rules

- Max active spells per run: 6.
- Creating a new spell requires at least one empty spell slot.
- Upgrading an existing spell does **not** require an empty slot.
- If all six spells are at level 5, no further spell progression is possible; level-up should route to stat boosts.

#### 4) Spell Recipe Table

| Combination | Spell Name | Effect |
|---|---|---|
| Od + Od | Alev Halkası | Orbiting fire circle |
| Sub + Sub | Şifa Pınarı | Passive HP regeneration |
| Yer + Yer | Kaya Kalkanı | Orbiting shield stones |
| Yel + Yel | Rüzgar Koşusu | Movement speed boost + damage trail |
| Temür + Temür | Demir Yağmuru | Random falling iron shards |
| Od + Temür | Kılıç Fırtınası | Throwing burning swords |
| Sub + Yel | Buz Rüzgarı | Cone-shaped freezing wave |
| Yel + Temür | Ok Yağmuru | Random arrow barrage |
| Od + Sub | Buhar Patlaması | Close-range damage + vision block |
| Yer + Temür | Deprem | Ground cracks, AoE damage |
| Od + Yel | Ateş Kasırgası | Moving fire tornado |
| Yer + Sub | Bataklık | Ground area slows enemies |
| Od + Yer | Lav Seli | Advancing lava trail on ground |
| Sub + Temür | Buz Kılıcı | Freezing close-range strikes |
| Yer + Yel | Kum Fırtınası | Wide-area slow + damage |

#### 5) Starting Element Mechanic

- Each hero defines one **Starting Element** in `HeroConfigSO`.
- At run start, hero automatically receives 1 copy of starting element (inventory starts 1/3 full).
- First level-up: choosing the same element immediately crafts the corresponding Basic Spell.
- Starting element receives **+15% weighted appearance frequency** in level-up options.
- **Umay exception**: starts with `Sub + Yer` (inventory starts 2/3 full).

#### 6) Level-Up Selection Screen Behavior

- Each offered element shows a dynamic recipe tooltip before confirm.
- Tooltip output must resolve to one of:
  - `If you pick this -> [Spell Name] will be created`
  - `If you pick this -> [Spell Name] Level N -> N+1`
  - `Added to inventory`
  - `Spell slots full` (when craft would occur but slots are full)
- Tooltip source of truth is Spell Crafting evaluation API (single logic path shared by UI and runtime).

#### 7) Evolution System (Post-MVP, out of scope)

- Evolution is **not in MVP** implementation scope.
- Planned rule: two specified spells both at max level (5) can merge into one Evolution Spell.
- Example: max `Alev Halkası` + max `Kılıç Fırtınası` => `Ergenekon Ateşi`.
- Evolution consumes two slots and returns one ultra-power slot (6 -> 5 occupancy effect).

### States and Transitions

#### A) Element Inventory State

| State | Entry Condition | Exit Condition | Behavior |
|---|---|---|---|
| Empty | Run start with no starting element (currently unused) | Element added | No possible craft |
| Partial(1) | Exactly 1 element present | Add/remove element | Can preview potential pair completion |
| Partial(2) | Exactly 2 elements present | Add/remove element | Can contain one potential match candidate |
| Full(3) | Exactly 3 elements present | Craft consumes pair, discard, or blocked add resolution | If no productive route, enable discard option next level-up |
| MatchPending | Selected element plus inventory creates valid recipe | Craft/upgrade resolved or blocked by slots | Consumes matching pair when resolved |

#### B) Spell Slot State

| State | Entry Condition | Exit Condition | Behavior |
|---|---|---|---|
| EmptySlotsAvailable | Slot count < 6 | Reach 6 spells | New crafting allowed |
| FullSlots | Slot count = 6 | Slot freed (not expected during run) | New craft blocked, upgrades still allowed |
| HasUpgradeableSpells | At least one owned spell level < 5 | All spells level 5 | Matching recipes produce upgrades |
| AllMaxed | All owned spells level = 5 and count = 6 | Not expected in current run | Route to stat boost options only |

#### C) Crafting State Machine

| State | Entry Trigger | Valid Transitions | Output |
|---|---|---|---|
| AwaitSelection | Level-up opened | EvaluateSelection | No state change |
| EvaluateSelection | Player hovers/taps element option | UpgradeExisting, CraftNew, AddToInventory, BlockedBySlots, InventoryFullNoMatch | Tooltip + resolution plan |
| UpgradeExisting | Recipe matches owned spell and level < 5 | AwaitSelection (next level-up) | `OnSpellUpgraded`, `OnElementConsumed` |
| CraftNew | Recipe match and free spell slot | AwaitSelection (next level-up) | `OnSpellCrafted`, `OnElementConsumed` |
| BlockedBySlots | Recipe match but slots full | AwaitSelection (next level-up) | Warning UI, keep selected element in inventory |
| AddToInventory | No recipe created/upgraded and inventory has room | AwaitSelection (next level-up) | Inventory updated |
| InventoryFullNoMatch | Inventory full and action would overflow without progression | AwaitSelection (next level-up with discard option) | Offer discard option |

Transition constraints:
- "Two different recipes both match" is prevented by recipe normalization and single-pair consume rule.
- If more than one inventory candidate could pair with selected element, first deterministic match by slot index is consumed (lowest index first).

### Interactions with Other Systems

| System | Interface In | Interface Out | Responsibility Split |
|---|---|---|---|
| Element Inventory | `GetElements()`, `HasFreeSlot()`, `Add(element)`, `Consume(index)` | Updated slot state | Spell Crafting decides **what** to consume/add; Inventory executes storage operations |
| Event Bus (**PROVISIONAL**) | Publish-only API | `OnSpellCrafted(spellId, level)`, `OnSpellUpgraded(spellId, newLevel)`, `OnElementConsumed(elementType)` | Spell Crafting emits domain events; listeners (VFX, audio, UI, analytics) react |
| Spell Slot Manager | `HasFreeSpellSlot()`, `TryCreateSpell(spellId)`, `TryUpgradeSpell(spellId)` | Success/fail + new level | Spell Crafting decides recipe result; Slot Manager owns spell instance lifecycle |
| Level-Up Selection UI | `EvaluateSelection(elementType)` | Tooltip result object (create/upgrade/add/blocked) | UI displays prediction; Spell Crafting remains single source of truth |
| Spell Effects | Receives crafted/leveled spell definitions from Slot Manager | Runtime combat behavior | Spell Crafting does not execute effects; only drives ownership/level state |
| Hero System | `GetStartingElements(heroId)`, `GetElementFrequencyBias(heroId)` | Starting inventory seed + weighted bias | Hero system supplies setup modifiers; crafting logic remains hero-agnostic after init |
| Class System | Class metadata passed to Damage Calculator | Damage multiplier application | Spell Crafting is class-agnostic; class affects output damage only |

Expected provisional interfaces (until dedicated GDD exists):

- **Element Inventory (PROVISIONAL)**
  - `bool TryAdd(ElementType element)`
  - `bool TryConsumeAt(int slotIndex, out ElementType consumed)`
  - `IReadOnlyList<ElementType> GetSnapshot()`

- **Event Bus (PROVISIONAL)**
  - `void Publish<TEvent>(TEvent e)`
  - Events: `SpellCraftedEvent`, `SpellUpgradedEvent`, `ElementConsumedEvent`

## Formulas

### Spell Damage Formula

```text
spell_damage = base_damage × (1 + 0.25 × (spell_level - 1)) × element_multiplier × class_bonus
```

| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| `base_damage` | float | 5-25 | `SpellDefinitionSO` | Base per-spell damage at level 1 |
| `spell_level` | int | 1-5 | Spell Slot Manager state | Current spell level |
| `element_multiplier` | float | 0.6 / 1.0 / 1.5 | Enemy affinity table | Resistance, normal, weakness |
| `class_bonus` | float | 1.0 / 1.25 | Class rules via Damage Calculator | Applied only when class condition matches |

Derived scaling:
- Per level increase: +25% base damage.
- Level 5 multiplier vs level 1: `2.0x` (+100%).

### Recipe Matching Formula

```text
normalized_pair = sort(element_a, element_b)
spell_id = recipe_map[normalized_pair]  // null if no recipe
```

Implementation notes:
- Sorting makes `Od + Temür` equivalent to `Temür + Od`.
- Basic recipes are represented as same-value pairs (e.g., `(Od, Od)`).
- Output is deterministic and O(1) with hash map lookup.

### Element Addition Evaluation Formula

```text
Given selected_element E:
1) check upgrade_candidates = owned_spells where recipe contains E and level < max_level
2) if any: choose matching inventory pair for that recipe -> UpgradeExisting
3) else check craft_candidates = inventory elements i where recipe_map[sort(E, i)] exists
4) if candidate exists and spell_slots < max_slots -> CraftNew
5) if candidate exists and spell_slots == max_slots -> BlockedBySlots
6) if no candidate and inventory_count < max_inventory -> AddToInventory
7) else -> InventoryFullNoMatch
```

Complexity target:
- Inventory scan max size 3 => trivial O(3).
- Spell evaluation target budget: **< 0.1 ms** per selection on target hardware.

## Edge Cases

| Scenario | Expected Behavior | Rationale |
|---|---|---|
| 1) Element inventory full (3/3) + new element offered | Next level-up includes "Discard 1 element from inventory" option; player may still re-roll once. | Preserves agency and prevents hard-lock. |
| 2) Spell slots full (6/6) + new spell would be crafted | New spell is not crafted; selected element remains in inventory; show "Spell slots full." | Keeps rules deterministic and avoids silent losses. |
| 3) Slots full + no offered element upgrades existing spells | Allow 1 free re-roll; if still no upgrade path, offer stat boost choices (+3% HP, +2% speed, +2% damage). | Maintains progression when spell system is temporarily blocked. |
| 4) Starting element initialization | Standard heroes start with 1 element in inventory (1/3); Umay starts with 2/3 (`Sub + Yer`). | Delivers hero identity and faster early recipe expression. |
| 5) Same element chosen twice in a row | First copy sits in inventory; second copy immediately triggers Basic Spell auto-craft if slot available. | Supports intuitive recipe learning. |
| 6) Element discarded to make room | Discarded element is permanently removed for this run; no recovery path in MVP. | Ensures discard choice is meaningful and strategic. |
| 7) All 6 spells at max level (5) + element still offered | Spell progression disabled; level-up routes to stat boost options only. | Prevents dead choices and keeps late run decisions useful. |
| 8) Re-roll returns same options | Allowed; reroll is random and may repeat prior elements; no bad-luck protection in MVP. | Keeps MVP random system simple and transparent. |
| 9) Two different recipes both match | By design this cannot occur as an ambiguous resolution; deterministic first matching pair (lowest inventory slot index) is consumed. | Guarantees deterministic behavior and testability. |

## Dependencies

| System | Direction | Nature |
|---|---|---|
| Element Inventory (**PROVISIONAL**) | Spell Crafting depends on | Reads inventory state, consumes elements on match, stores non-matching picks |
| Event Bus (**PROVISIONAL**) | Spell Crafting depends on | Fires craft/upgrade/consume events for VFX, audio, UI, analytics |
| Spell Slot Manager | Spell Crafting depends on | Checks slot availability, creates and upgrades spell instances |
| Level-Up Selection | Depends on Spell Crafting | Calls evaluation API to build element tooltips |
| Spell Effects | Depends on Spell Crafting | Receives crafted spell definitions and level changes |
| Hero System | Spell Crafting depends on | Provides starting element(s) and +15% element bias seed |
| Class System | Indirect (via Damage Calculator) | Multiplies spell damage output; does not change crafting decisions |

Dependency notes:
- Element Inventory and Event Bus are provisional because dedicated GDDs are not yet authored.
- Spell Crafting must expose stable evaluation interfaces so UI and runtime cannot diverge.

## Tuning Knobs

| Parameter | Default | Range | Source File | Effect of Change |
|---|---|---|---|---|
| Max element inventory slots | 3 | 2-5 | `PlayerConfigSO` | More slots = easier crafting, lower planning tension |
| Max spell slots | 6 | 4-8 | `PlayerConfigSO` | More slots = broader builds, higher HUD and balance complexity |
| Max spell level | 5 | 3-7 | `SpellConfigSO` | Higher cap extends progression runway |
| Element choices per level-up | 3 | 2-5 | `LevelUpConfigSO` | More choices increase recipe targeting reliability |
| Free re-rolls per level-up | 1 | 0-3 | `LevelUpConfigSO` | More rerolls increase player agency, reduce randomness pressure |
| Starting element frequency bonus | +15% | +0% to +30% | `HeroConfigSO` | Higher bias makes hero-signature openings more consistent |
| Spell level damage scaling | +25%/level | +10% to +40% | `SpellConfigSO` | Higher scaling increases reward for upgrading vs new craft |
| Stat boost when no spell option | +3% baseline | +1% to +5% | `LevelUpConfigSO` | Higher fallback softens slot-lock frustration |

## Acceptance Criteria

- [ ] Player can receive elements on level-up and they appear in Element Inventory (max 3)
- [ ] When 2 matching elements are in inventory, a spell is auto-crafted and added to Spell Slots
- [ ] All 15 spell recipes are defined and produce the correct spell for each element pair
- [ ] Selecting an element that matches an existing spell's recipe upgrades that spell by +1 level (max 5)
- [ ] When inventory is full (3/3) with no matches, level-up screen offers "Discard element" option
- [ ] When spell slots are full (6/6), new spells are NOT crafted — element stays in inventory
- [ ] When slots full AND no upgrades possible, a stat boost option is offered
- [ ] Hero's starting element is auto-added to inventory at run start (1/3 for most, 2/3 for Umay)
- [ ] Recipe tooltips correctly show what will happen for each element choice
- [ ] Re-roll generates 3 new random element choices (can produce same elements)
- [ ] System fires events: OnSpellCrafted, OnSpellUpgraded, OnElementConsumed
- [ ] All balance values come from ScriptableObjects — zero hardcoded values
- [ ] System handles all 9 documented edge cases correctly
- [ ] Performance: crafting evaluation completes in < 0.1ms (trivial computation)

## Visual/Audio Requirements

| Event | Visual | Audio | Priority |
|---|---|---|---|
| Element added to inventory | Element icon flies to inventory slot, brief glow | Soft chime | High |
| Spell crafted (new) | Spell icon appears in slot with burst effect, screen flash | Dramatic chime + whoosh | High |
| Spell upgraded | Spell icon pulses, level number increments with particle effect | Ascending tone | High |
| Inventory full warning | Inventory slots flash red briefly | Warning buzz | Medium |
| Spell slots full warning | Text notification "Spell slots full" | Soft notification | Medium |
| Element discarded | Element icon shatters/dissolves | Soft breaking sound | Medium |

## UI Requirements

| Information | Location | Update Timing | Condition |
|---|---|---|---|
| Element inventory (3 slots) | Bottom-left HUD | On element add/remove | Always visible during run |
| Spell slots (6 slots) | Bottom-center HUD | On craft/upgrade | Always visible during run |
| Element choice options | Level-up popup center | On level-up trigger | Shown during level-up |
| Recipe tooltip | Next to each element option | On hover/tap of option | Shown during level-up |
| Current spell levels | Inside spell slot icons | On upgrade | Always visible during run |

## Open Questions

| Question | Owner | Deadline | Notes |
|---|---|---|---|
| Should element order in inventory matter for matching? | game-designer | Before implementation | Current design: no order, first match wins |
| What stat boost options when no spell possible? | game-designer | Before MVP-1 end | Current: HP/Speed/Damage — needs playtest |
| Should discarding have undo (1 second window)? | ux-designer | Before MVP-1 end | Currently no undo — may feel punishing |
| Element frequency: pure random or weighted pool? | game-designer | Before implementation | Hero bias +15% is weighted. Base pool still to finalize |
