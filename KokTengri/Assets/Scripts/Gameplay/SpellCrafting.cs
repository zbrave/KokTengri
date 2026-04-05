using System;
using System.Collections.Generic;
using KokTengri.Core;

namespace KokTengri.Gameplay
{
    /// <summary>
    /// Evaluates and resolves spell crafting outcomes from selected elements and current inventory state.
    /// </summary>
    public sealed class SpellCrafting
    {
        private const int BaseCraftedSpellLevel = 1;
        private const int MaxSpellLevel = 5;

        private static readonly Dictionary<(ElementType, ElementType), RecipeEntry> _recipes = CreateRecipes();
        private static readonly Dictionary<string, RecipeEntry> _recipesBySpellId = CreateRecipeLookupBySpellId();

        private readonly ElementInventory _inventory;

        public SpellCrafting(ElementInventory inventory)
        {
            _inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
        }

        /// <summary>
        /// Immutable crafting resolution preview/result.
        /// </summary>
        public readonly struct CraftingResult
        {
            public readonly CraftingResultType Type;
            public readonly string SpellId;
            public readonly int NewLevel;
            public readonly SpellKind Kind;
            public readonly int ConsumedSlotIndex;

            public CraftingResult(CraftingResultType type, string spellId, int newLevel, SpellKind kind, int consumedSlotIndex)
            {
                Type = type;
                SpellId = spellId;
                NewLevel = newLevel;
                Kind = kind;
                ConsumedSlotIndex = consumedSlotIndex;
            }
        }

        /// <summary>
        /// Predict the outcome of selecting an element without mutating inventory state.
        /// </summary>
        public CraftingResult EvaluateSelection(ElementType selectedElement, IReadOnlyList<SpellSlotEntry> ownedSpells, int maxSpellSlots)
        {
            return ResolveSelection(selectedElement, ownedSpells, maxSpellSlots);
        }

        /// <summary>
        /// Evaluate and apply the outcome of selecting an element.
        /// </summary>
        public CraftingResult ProcessSelection(ElementType selectedElement, IReadOnlyList<SpellSlotEntry> ownedSpells, int maxSpellSlots)
        {
            CraftingResult result = ResolveSelection(selectedElement, ownedSpells, maxSpellSlots);

            switch (result.Type)
            {
                case CraftingResultType.UpgradeSpell:
                    if (_inventory.TryConsumeAt(result.ConsumedSlotIndex))
                    {
                        EventBus.Publish(new SpellUpgradedEvent(result.SpellId, result.NewLevel));
                    }

                    break;

                case CraftingResultType.NewSpell:
                    if (_inventory.TryConsumeAt(result.ConsumedSlotIndex))
                    {
                        EventBus.Publish(new SpellCraftedEvent(result.SpellId, result.NewLevel, result.Kind));
                    }

                    break;

                case CraftingResultType.BlockedByFullSlots:
                    _inventory.TryAdd(selectedElement);
                    break;

                case CraftingResultType.AddToInventory:
                    _inventory.TryAdd(selectedElement);
                    break;

                case CraftingResultType.InventoryFullNoMatch:
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }

            return result;
        }

        private static Dictionary<(ElementType, ElementType), RecipeEntry> CreateRecipes()
        {
            var recipes = new Dictionary<(ElementType, ElementType), RecipeEntry>(15);

            AddRecipe(recipes, ElementType.Od, ElementType.Od, "alev_halkasi", SpellKind.Orbit);
            AddRecipe(recipes, ElementType.Sub, ElementType.Sub, "sifa_pinari", SpellKind.Passive);
            AddRecipe(recipes, ElementType.Yer, ElementType.Yer, "kaya_kalkani", SpellKind.Orbit);
            AddRecipe(recipes, ElementType.Yel, ElementType.Yel, "ruzgar_kosusu", SpellKind.Aura);
            AddRecipe(recipes, ElementType.Temur, ElementType.Temur, "demir_yagmuru", SpellKind.AoE);
            AddRecipe(recipes, ElementType.Od, ElementType.Temur, "kilic_firtinasi", SpellKind.Projectile);
            AddRecipe(recipes, ElementType.Sub, ElementType.Yel, "buz_ruzgari", SpellKind.AoE);
            AddRecipe(recipes, ElementType.Yel, ElementType.Temur, "ok_yagmuru", SpellKind.AoE);
            AddRecipe(recipes, ElementType.Od, ElementType.Sub, "buhar_patlamasi", SpellKind.AoE);
            AddRecipe(recipes, ElementType.Yer, ElementType.Temur, "deprem", SpellKind.AoE);
            AddRecipe(recipes, ElementType.Od, ElementType.Yel, "ates_kasirgasi", SpellKind.Projectile);
            AddRecipe(recipes, ElementType.Yer, ElementType.Sub, "bataklik", SpellKind.AoE);
            AddRecipe(recipes, ElementType.Od, ElementType.Yer, "lav_seli", SpellKind.AoE);
            AddRecipe(recipes, ElementType.Sub, ElementType.Temur, "buz_kilici", SpellKind.Projectile);
            AddRecipe(recipes, ElementType.Yer, ElementType.Yel, "kum_firtinasi", SpellKind.AoE);

            return recipes;
        }

        private static Dictionary<string, RecipeEntry> CreateRecipeLookupBySpellId()
        {
            var lookup = new Dictionary<string, RecipeEntry>(StringComparer.Ordinal);

            foreach (RecipeEntry recipe in _recipes.Values)
            {
                lookup[recipe.SpellId] = recipe;
            }

            return lookup;
        }

        private static void AddRecipe(
            IDictionary<(ElementType, ElementType), RecipeEntry> recipes,
            ElementType elementA,
            ElementType elementB,
            string spellId,
            SpellKind kind)
        {
            var normalizedPair = NormalizePair(elementA, elementB);
            recipes[normalizedPair] = new RecipeEntry(spellId, kind, normalizedPair.Item1, normalizedPair.Item2);
        }

        private CraftingResult ResolveSelection(ElementType selectedElement, IReadOnlyList<SpellSlotEntry> ownedSpells, int maxSpellSlots)
        {
            if (ownedSpells == null)
            {
                throw new ArgumentNullException(nameof(ownedSpells));
            }

            for (var slotIndex = 0; slotIndex < ElementInventory.MaxSlots; slotIndex++)
            {
                ElementType? inventoryElement = _inventory.GetElementAt(slotIndex);

                if (!inventoryElement.HasValue || !TryGetRecipe(selectedElement, inventoryElement.Value, out RecipeEntry recipe))
                {
                    continue;
                }

                if (TryGetOwnedSpell(recipe.SpellId, ownedSpells, out SpellSlotEntry ownedSpell) && ownedSpell.Level < MaxSpellLevel)
                {
                    int newLevel = Math.Min(MaxSpellLevel, ownedSpell.Level + 1);
                    return new CraftingResult(CraftingResultType.UpgradeSpell, recipe.SpellId, newLevel, recipe.Kind, slotIndex);
                }
            }

            bool hasFreeSpellSlot = CountOwnedSpells(ownedSpells) < maxSpellSlots;

            for (var slotIndex = 0; slotIndex < ElementInventory.MaxSlots; slotIndex++)
            {
                ElementType? inventoryElement = _inventory.GetElementAt(slotIndex);

                if (!inventoryElement.HasValue || !TryGetRecipe(selectedElement, inventoryElement.Value, out RecipeEntry recipe))
                {
                    continue;
                }

                if (IsSpellOwned(recipe.SpellId, ownedSpells))
                {
                    continue;
                }

                CraftingResultType resultType = hasFreeSpellSlot
                    ? CraftingResultType.NewSpell
                    : CraftingResultType.BlockedByFullSlots;

                return new CraftingResult(resultType, recipe.SpellId, BaseCraftedSpellLevel, recipe.Kind, slotIndex);
            }

            if (_inventory.HasFreeSlot)
            {
                return new CraftingResult(CraftingResultType.AddToInventory, string.Empty, 0, default, -1);
            }

            return new CraftingResult(CraftingResultType.InventoryFullNoMatch, string.Empty, 0, default, -1);
        }

        private static bool TryGetRecipe(ElementType selectedElement, ElementType inventoryElement, out RecipeEntry recipe)
        {
            return _recipes.TryGetValue(NormalizePair(selectedElement, inventoryElement), out recipe);
        }

        private static (ElementType, ElementType) NormalizePair(ElementType elementA, ElementType elementB)
        {
            return elementA <= elementB ? (elementA, elementB) : (elementB, elementA);
        }

        private static bool TryGetOwnedSpell(string spellId, IReadOnlyList<SpellSlotEntry> ownedSpells, out SpellSlotEntry ownedSpell)
        {
            for (var i = 0; i < ownedSpells.Count; i++)
            {
                SpellSlotEntry candidate = ownedSpells[i];

                if (string.Equals(candidate.SpellId, spellId, StringComparison.Ordinal))
                {
                    ownedSpell = candidate;
                    return true;
                }
            }

            ownedSpell = default;
            return false;
        }

        private static bool IsSpellOwned(string spellId, IReadOnlyList<SpellSlotEntry> ownedSpells)
        {
            return TryGetOwnedSpell(spellId, ownedSpells, out _);
        }

        private static int CountOwnedSpells(IReadOnlyList<SpellSlotEntry> ownedSpells)
        {
            var count = 0;

            for (var i = 0; i < ownedSpells.Count; i++)
            {
                if (!string.IsNullOrWhiteSpace(ownedSpells[i].SpellId))
                {
                    count++;
                }
            }

            return count;
        }

        private readonly struct RecipeEntry
        {
            public RecipeEntry(string spellId, SpellKind kind, ElementType elementA, ElementType elementB)
            {
                SpellId = spellId;
                Kind = kind;
                ElementA = elementA;
                ElementB = elementB;
            }

            public string SpellId { get; }

            public SpellKind Kind { get; }

            public ElementType ElementA { get; }

            public ElementType ElementB { get; }
        }
    }
}
