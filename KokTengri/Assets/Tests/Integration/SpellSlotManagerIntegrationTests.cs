using System.Collections.Generic;
using KokTengri.Core;
using KokTengri.Gameplay;
using NUnit.Framework;

namespace KokTengri.Tests.Integration
{
    /// <summary>
    /// Integration tests for SpellSlotManager verifying slot lifecycle,
    /// upgrade paths, and interaction with the crafting system.
    /// </summary>
    [TestFixture]
    public sealed class SpellSlotManagerIntegrationTests
    {
        private SpellSlotManager _manager;

        [SetUp]
        public void SetUp()
        {
            _manager = new SpellSlotManager(maxSlots: 6, maxLevel: 5);
        }

        // --- Slot Capacity ---

        [Test]
        public void Constructor_SetsMaxSlotsAndMaxLevel()
        {
            Assert.That(_manager.MaxSlots, Is.EqualTo(6));
            Assert.That(_manager.MaxLevel, Is.EqualTo(5));
        }

        [Test]
        public void HasFreeSlot_Initially_ReturnsTrue()
        {
            Assert.That(_manager.HasFreeSlot, Is.True);
        }

        [Test]
        public void SpellCount_Initially_ReturnsZero()
        {
            Assert.That(_manager.SpellCount, Is.EqualTo(0));
        }

        // --- Add Spell ---

        [Test]
        public void TryAddSpell_NewSpell_ReturnsTrue()
        {
            bool added = _manager.TryAddSpell("alev_halkasi", SpellKind.Orbit, out int slotIndex);

            Assert.That(added, Is.True);
            Assert.That(slotIndex, Is.EqualTo(0));
            Assert.That(_manager.SpellCount, Is.EqualTo(1));
        }

        [Test]
        public void TryAddSpell_MultipleSpells_FillsSequentialSlots()
        {
            _manager.TryAddSpell("alev_halkasi", SpellKind.Orbit, out int slot0);
            _manager.TryAddSpell("kilic_firtinasi", SpellKind.Projectile, out int slot1);
            _manager.TryAddSpell("kaya_kalkani", SpellKind.Orbit, out int slot2);

            Assert.That(slot0, Is.EqualTo(0));
            Assert.That(slot1, Is.EqualTo(1));
            Assert.That(slot2, Is.EqualTo(2));
            Assert.That(_manager.SpellCount, Is.EqualTo(3));
        }

        // --- Upgrade Spell ---

        [Test]
        public void TryUpgradeSpell_ExistingSpell_IncrementsLevel()
        {
            _manager.TryAddSpell("alev_halkasi", SpellKind.Orbit, out _);

            bool upgraded = _manager.TryUpgradeSpell("alev_halkasi", out int newLevel);

            Assert.That(upgraded, Is.True);
            Assert.That(newLevel, Is.EqualTo(2));
        }

        [Test]
        public void TryUpgradeSpell_AtMaxLevel_ReturnsFalse()
        {
            _manager.TryAddSpell("alev_halkasi", SpellKind.Orbit, out _);

            for (int i = 0; i < 4; i++)
            {
                _manager.TryUpgradeSpell("alev_halkasi", out _);
            }

            bool upgraded = _manager.TryUpgradeSpell("alev_halkasi", out int level);

            Assert.That(upgraded, Is.False);
            Assert.That(level, Is.EqualTo(5));
        }

        // --- Remove Spell ---

        [Test]
        public void TryRemoveSpell_ExistingSpell_FreesSlot()
        {
            _manager.TryAddSpell("alev_halkasi", SpellKind.Orbit, out _);

            bool removed = _manager.TryRemoveSpell("alev_halkasi");

            Assert.That(removed, Is.True);
            Assert.That(_manager.SpellCount, Is.EqualTo(0));
            Assert.That(_manager.HasFreeSlot, Is.True);
        }

        // --- Query Spells ---

        [Test]
        public void TryGetSpell_ExistingSpell_ReturnsEntry()
        {
            _manager.TryAddSpell("kilic_firtinasi", SpellKind.Projectile, out _);

            bool found = _manager.TryGetSpell("kilic_firtinasi", out SpellSlotEntry entry);

            Assert.That(found, Is.True);
            Assert.That(entry.SpellId, Is.EqualTo("kilic_firtinasi"));
            Assert.That(entry.Kind, Is.EqualTo(SpellKind.Projectile));
            Assert.That(entry.Level, Is.EqualTo(1));
        }

        [Test]
        public void TryGetSpell_NonExistent_ReturnsFalse()
        {
            bool found = _manager.TryGetSpell("nonexistent_spell", out _);

            Assert.That(found, Is.False);
        }

        // --- Full Slots Scenario ---

        [Test]
        public void TryAddSpell_WhenAllSlotsFull_ReturnsFalse()
        {
            var smallManager = new SpellSlotManager(maxSlots: 2, maxLevel: 5);

            smallManager.TryAddSpell("spell_a", SpellKind.AoE, out _);
            smallManager.TryAddSpell("spell_b", SpellKind.Projectile, out _);

            bool added = smallManager.TryAddSpell("spell_c", SpellKind.Orbit, out _);

            Assert.That(added, Is.False);
            Assert.That(smallManager.SpellCount, Is.EqualTo(2));
            Assert.That(smallManager.HasFreeSlot, Is.False);
        }
    }
}
