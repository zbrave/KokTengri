using System.Collections.Generic;
using KokTengri.Core;
using KokTengri.Gameplay;
using NUnit.Framework;

namespace KokTengri.Tests.Integration
{
    /// <summary>
    /// Integration tests for the SpellCrafting + ElementInventory + SpellSlotManager pipeline.
    /// Verifies that element selection produces correct crafting results,
    /// upgrades, and inventory state changes.
    /// </summary>
    [TestFixture]
    public sealed class SpellCraftingIntegrationTests
    {
        private ElementInventory _inventory;
        private SpellCrafting _crafting;
        private SpellSlotManager _spellSlots;

        private readonly List<SpellCraftedEvent> _craftedEvents = new();
        private readonly List<SpellUpgradedEvent> _upgradedEvents = new();

        [SetUp]
        public void SetUp()
        {
            EventBus.Reset();
            _inventory = new ElementInventory();
            _spellSlots = new SpellSlotManager(maxSlots: 6, maxLevel: 5);
            _crafting = new SpellCrafting(_inventory);

            _craftedEvents.Clear();
            _upgradedEvents.Clear();

            EventBus.Subscribe<SpellCraftedEvent>(e => _craftedEvents.Add(e));
            EventBus.Subscribe<SpellUpgradedEvent>(e => _upgradedEvents.Add(e));
        }

        [TearDown]
        public void TearDown()
        {
            EventBus.Reset();
        }

        // --- New Spell Crafting ---

        [Test]
        public void ProcessSelection_TwoOdElements_CraftsAlevHalkasi()
        {
            _inventory.TryAdd(ElementType.Od);

            var result = _crafting.ProcessSelection(ElementType.Od, _spellSlots.GetAllSpells(), _spellSlots.MaxSlots);

            Assert.That(result.Type, Is.EqualTo(CraftingResultType.NewSpell));
            Assert.That(result.SpellId, Is.EqualTo("alev_halkasi"));
            Assert.That(result.Kind, Is.EqualTo(SpellKind.Orbit));
            Assert.That(result.NewLevel, Is.EqualTo(1));
        }

        [Test]
        public void ProcessSelection_OdPlusTemur_CraftsKilicFirtinasi()
        {
            _inventory.TryAdd(ElementType.Od);

            var result = _crafting.ProcessSelection(ElementType.Temur, _spellSlots.GetAllSpells(), _spellSlots.MaxSlots);

            Assert.That(result.Type, Is.EqualTo(CraftingResultType.NewSpell));
            Assert.That(result.SpellId, Is.EqualTo("kilic_firtinasi"));
            Assert.That(result.Kind, Is.EqualTo(SpellKind.Projectile));
        }

        [Test]
        public void ProcessSelection_SubPlusYel_CraftsBuzRuzgari()
        {
            _inventory.TryAdd(ElementType.Sub);

            var result = _crafting.ProcessSelection(ElementType.Yel, _spellSlots.GetAllSpells(), _spellSlots.MaxSlots);

            Assert.That(result.Type, Is.EqualTo(CraftingResultType.NewSpell));
            Assert.That(result.SpellId, Is.EqualTo("buz_ruzgari"));
            Assert.That(result.Kind, Is.EqualTo(SpellKind.AoE));
        }

        // --- Inventory State After Crafting ---

        [Test]
        public void ProcessSelection_ConsumesInventoryElementOnCraft()
        {
            _inventory.TryAdd(ElementType.Od);

            _crafting.ProcessSelection(ElementType.Od, _spellSlots.GetAllSpells(), _spellSlots.MaxSlots);

            Assert.That(_inventory.OccupiedCount, Is.EqualTo(0));
            Assert.That(_inventory.HasFreeSlot, Is.True);
        }

        // --- Evaluate (Preview) Does Not Mutate ---

        [Test]
        public void EvaluateSelection_DoesNotConsumeInventory()
        {
            _inventory.TryAdd(ElementType.Od);

            var preview = _crafting.EvaluateSelection(ElementType.Od, _spellSlots.GetAllSpells(), _spellSlots.MaxSlots);

            Assert.That(preview.Type, Is.EqualTo(CraftingResultType.NewSpell));
            Assert.That(_inventory.OccupiedCount, Is.EqualTo(1));
        }

        // --- Spell Upgrade Flow ---

        [Test]
        public void ProcessSelection_SameRecipeTwice_UpgradesSpell()
        {
            _inventory.TryAdd(ElementType.Od);
            _crafting.ProcessSelection(ElementType.Od, _spellSlots.GetAllSpells(), _spellSlots.MaxSlots);

            _inventory.TryAdd(ElementType.Od);
            var slotsWithSpell = CreateSlotsWithSpell("alev_halkasi", 1, SpellKind.Orbit);
            var upgradeResult = _crafting.ProcessSelection(ElementType.Od, slotsWithSpell, _spellSlots.MaxSlots);

            Assert.That(upgradeResult.Type, Is.EqualTo(CraftingResultType.UpgradeSpell));
            Assert.That(upgradeResult.SpellId, Is.EqualTo("alev_halkasi"));
            Assert.That(upgradeResult.NewLevel, Is.EqualTo(2));
        }

        [Test]
        public void ProcessSelection_UpgradeFromLevel4To5_Succeeds()
        {
            _inventory.TryAdd(ElementType.Yer);
            var slotsWithSpell = CreateSlotsWithSpell("kaya_kalkani", 4, SpellKind.Orbit);

            var result = _crafting.ProcessSelection(ElementType.Yer, slotsWithSpell, _spellSlots.MaxSlots);

            Assert.That(result.Type, Is.EqualTo(CraftingResultType.UpgradeSpell));
            Assert.That(result.NewLevel, Is.EqualTo(5));
        }

        [Test]
        public void ProcessSelection_SpellAtMaxLevel_BlockedByFullSlots()
        {
            _inventory.TryAdd(ElementType.Od);
            var slotsWithMaxedSpell = CreateSlotsWithSpell("alev_halkasi", 5, SpellKind.Orbit);

            var result = _crafting.ProcessSelection(ElementType.Od, slotsWithMaxedSpell, _spellSlots.MaxSlots);

            Assert.That(result.Type, Is.EqualTo(CraftingResultType.BlockedByFullSlots));
        }

        // --- No Recipe Match ---

        [Test]
        public void ProcessSelection_NoInventoryElement_AddsToInventory()
        {
            var result = _crafting.ProcessSelection(ElementType.Od, _spellSlots.GetAllSpells(), _spellSlots.MaxSlots);

            Assert.That(result.Type, Is.EqualTo(CraftingResultType.AddToInventory));
            Assert.That(_inventory.OccupiedCount, Is.EqualTo(1));
        }

        // --- All 15 Recipes Produce Correct SpellId ---

        [TestCase(ElementType.Od, ElementType.Od, "alev_halkasi", SpellKind.Orbit)]
        [TestCase(ElementType.Sub, ElementType.Sub, "sifa_pinari", SpellKind.Passive)]
        [TestCase(ElementType.Yer, ElementType.Yer, "kaya_kalkani", SpellKind.Orbit)]
        [TestCase(ElementType.Yel, ElementType.Yel, "ruzgar_kosusu", SpellKind.Aura)]
        [TestCase(ElementType.Temur, ElementType.Temur, "demir_yagmuru", SpellKind.AoE)]
        [TestCase(ElementType.Od, ElementType.Temur, "kilic_firtinasi", SpellKind.Projectile)]
        [TestCase(ElementType.Sub, ElementType.Yel, "buz_ruzgari", SpellKind.AoE)]
        [TestCase(ElementType.Yel, ElementType.Temur, "ok_yagmuru", SpellKind.AoE)]
        [TestCase(ElementType.Od, ElementType.Sub, "buhar_patlamasi", SpellKind.AoE)]
        [TestCase(ElementType.Yer, ElementType.Temur, "deprem", SpellKind.AoE)]
        [TestCase(ElementType.Od, ElementType.Yel, "ates_kasirgasi", SpellKind.Projectile)]
        [TestCase(ElementType.Yer, ElementType.Sub, "bataklik", SpellKind.AoE)]
        [TestCase(ElementType.Od, ElementType.Yer, "lav_seli", SpellKind.AoE)]
        [TestCase(ElementType.Sub, ElementType.Temur, "buz_kilici", SpellKind.Projectile)]
        [TestCase(ElementType.Yer, ElementType.Yel, "kum_firtinasi", SpellKind.AoE)]
        public void ProcessSelection_All15Recipes_ProduceCorrectSpell(
            ElementType elementA, ElementType elementB, string expectedSpellId, SpellKind expectedKind)
        {
            _inventory.TryAdd(elementA);

            var result = _crafting.ProcessSelection(elementB, _spellSlots.GetAllSpells(), _spellSlots.MaxSlots);

            Assert.That(result.Type, Is.EqualTo(CraftingResultType.NewSpell));
            Assert.That(result.SpellId, Is.EqualTo(expectedSpellId));
            Assert.That(result.Kind, Is.EqualTo(expectedKind));
        }

        // --- EventBus Event Verification ---

        [Test]
        public void ProcessSelection_NewSpell_PublishesSpellCraftedEvent()
        {
            _inventory.TryAdd(ElementType.Od);

            _crafting.ProcessSelection(ElementType.Od, _spellSlots.GetAllSpells(), _spellSlots.MaxSlots);

            Assert.That(_craftedEvents.Count, Is.EqualTo(1));
            Assert.That(_craftedEvents[0].SpellId, Is.EqualTo("alev_halkasi"));
            Assert.That(_craftedEvents[0].Level, Is.EqualTo(1));
            Assert.That(_craftedEvents[0].Kind, Is.EqualTo(SpellKind.Orbit));
        }

        [Test]
        public void ProcessSelection_UpgradeSpell_PublishesSpellUpgradedEvent()
        {
            _inventory.TryAdd(ElementType.Od);
            _crafting.ProcessSelection(ElementType.Od, _spellSlots.GetAllSpells(), _spellSlots.MaxSlots);
            _craftedEvents.Clear();

            _inventory.TryAdd(ElementType.Od);
            var slotsWithSpell = CreateSlotsWithSpell("alev_halkasi", 1, SpellKind.Orbit);
            _crafting.ProcessSelection(ElementType.Od, slotsWithSpell, _spellSlots.MaxSlots);

            Assert.That(_upgradedEvents.Count, Is.EqualTo(1));
            Assert.That(_upgradedEvents[0].SpellId, Is.EqualTo("alev_halkasi"));
            Assert.That(_upgradedEvents[0].NewLevel, Is.EqualTo(2));
        }

        // --- Helpers ---

        private static List<SpellSlotEntry> CreateSlotsWithSpell(string spellId, int level, SpellKind kind)
        {
            return new List<SpellSlotEntry> { new SpellSlotEntry(spellId, level, kind) };
        }
    }
}
