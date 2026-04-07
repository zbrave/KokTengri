using System.Collections.Generic;
using KokTengri.Core;
using KokTengri.Gameplay;
using NUnit.Framework;

namespace KokTengri.Tests.Integration
{
    /// <summary>
    /// Integration tests for SpellSlotManager verifying slot lifecycle,
    /// upgrade paths, and query operations against the actual API.
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

        // --- Create Spell ---

        [Test]
        public void TryCreateSpell_NewSpell_ReturnsTrue()
        {
            bool created = _manager.TryCreateSpell("alev_halkasi", SpellKind.Orbit);

            Assert.That(created, Is.True);
            Assert.That(_manager.SpellCount, Is.EqualTo(1));
        }

        [Test]
        public void TryCreateSpell_MultipleSpells_FillsSequentialSlots()
        {
            _manager.TryCreateSpell("alev_halkasi", SpellKind.Orbit);
            _manager.TryCreateSpell("kilic_firtinasi", SpellKind.Projectile);
            _manager.TryCreateSpell("kaya_kalkani", SpellKind.Orbit);

            Assert.That(_manager.SpellCount, Is.EqualTo(3));

            var spell0 = _manager.GetSpellAt(0);
            var spell1 = _manager.GetSpellAt(1);
            var spell2 = _manager.GetSpellAt(2);

            Assert.That(spell0.HasValue, Is.True);
            Assert.That(spell0.Value.SpellId, Is.EqualTo("alev_halkasi"));
            Assert.That(spell1.HasValue, Is.True);
            Assert.That(spell1.Value.SpellId, Is.EqualTo("kilic_firtinasi"));
            Assert.That(spell2.HasValue, Is.True);
            Assert.That(spell2.Value.SpellId, Is.EqualTo("kaya_kalkani"));
        }

        [Test]
        public void TryCreateSpell_DuplicateSpell_ReturnsFalse()
        {
            _manager.TryCreateSpell("alev_halkasi", SpellKind.Orbit);

            bool duplicate = _manager.TryCreateSpell("alev_halkasi", SpellKind.Orbit);

            Assert.That(duplicate, Is.False);
            Assert.That(_manager.SpellCount, Is.EqualTo(1));
        }

        // --- Upgrade Spell ---

        [Test]
        public void TryUpgradeSpell_ExistingSpell_IncrementsLevel()
        {
            _manager.TryCreateSpell("alev_halkasi", SpellKind.Orbit);

            bool upgraded = _manager.TryUpgradeSpell("alev_halkasi");

            Assert.That(upgraded, Is.True);
            Assert.That(_manager.GetSpellLevel("alev_halkasi"), Is.EqualTo(2));
        }

        [Test]
        public void TryUpgradeSpell_AtMaxLevel_ReturnsFalse()
        {
            _manager.TryCreateSpell("alev_halkasi", SpellKind.Orbit);

            // Upgrade from level 1 to 5
            for (int i = 0; i < 4; i++)
            {
                _manager.TryUpgradeSpell("alev_halkasi");
            }

            Assert.That(_manager.GetSpellLevel("alev_halkasi"), Is.EqualTo(5));

            // Try to go beyond max
            bool upgraded = _manager.TryUpgradeSpell("alev_halkasi");

            Assert.That(upgraded, Is.False);
            Assert.That(_manager.GetSpellLevel("alev_halkasi"), Is.EqualTo(5));
        }

        [Test]
        public void TryUpgradeSpell_NonExistent_ReturnsFalse()
        {
            bool upgraded = _manager.TryUpgradeSpell("nonexistent_spell");

            Assert.That(upgraded, Is.False);
        }

        // --- Query Spells ---

        [Test]
        public void IsSpellOwned_ExistingSpell_ReturnsTrue()
        {
            _manager.TryCreateSpell("kilic_firtinasi", SpellKind.Projectile);

            Assert.That(_manager.IsSpellOwned("kilic_firtinasi"), Is.True);
        }

        [Test]
        public void IsSpellOwned_NonExistent_ReturnsFalse()
        {
            Assert.That(_manager.IsSpellOwned("nonexistent_spell"), Is.False);
        }

        [Test]
        public void GetSpellLevel_ExistingSpell_ReturnsCurrentLevel()
        {
            _manager.TryCreateSpell("kilic_firtinasi", SpellKind.Projectile);

            Assert.That(_manager.GetSpellLevel("kilic_firtinasi"), Is.EqualTo(1));

            _manager.TryUpgradeSpell("kilic_firtinasi");

            Assert.That(_manager.GetSpellLevel("kilic_firtinasi"), Is.EqualTo(2));
        }

        [Test]
        public void GetSpellLevel_NonExistent_ReturnsZero()
        {
            Assert.That(_manager.GetSpellLevel("nonexistent_spell"), Is.EqualTo(0));
        }

        [Test]
        public void GetSpellAt_ValidIndex_ReturnsEntry()
        {
            _manager.TryCreateSpell("kilic_firtinasi", SpellKind.Projectile);

            var entry = _manager.GetSpellAt(0);

            Assert.That(entry.HasValue, Is.True);
            Assert.That(entry.Value.SpellId, Is.EqualTo("kilic_firtinasi"));
            Assert.That(entry.Value.Kind, Is.EqualTo(SpellKind.Projectile));
            Assert.That(entry.Value.Level, Is.EqualTo(1));
        }

        [Test]
        public void GetSpellAt_EmptyIndex_ReturnsNull()
        {
            var entry = _manager.GetSpellAt(0);

            Assert.That(entry.HasValue, Is.False);
        }

        [Test]
        public void GetSpellAt_InvalidIndex_ReturnsNull()
        {
            var entry = _manager.GetSpellAt(-1);
            Assert.That(entry.HasValue, Is.False);

            entry = _manager.GetSpellAt(99);
            Assert.That(entry.HasValue, Is.False);
        }

        // --- GetAllSpells ---

        [Test]
        public void GetAllSpells_Empty_ReturnsEmptyList()
        {
            var spells = _manager.GetAllSpells();

            Assert.That(spells.Count, Is.EqualTo(0));
        }

        [Test]
        public void GetAllSpells_WithSpells_ReturnsOrderedSnapshot()
        {
            _manager.TryCreateSpell("alev_halkasi", SpellKind.Orbit);
            _manager.TryCreateSpell("kilic_firtinasi", SpellKind.Projectile);

            var spells = _manager.GetAllSpells();

            Assert.That(spells.Count, Is.EqualTo(2));
            Assert.That(spells[0].SpellId, Is.EqualTo("alev_halkasi"));
            Assert.That(spells[1].SpellId, Is.EqualTo("kilic_firtinasi"));
        }

        // --- Full Slots Scenario ---

        [Test]
        public void TryCreateSpell_WhenAllSlotsFull_ReturnsFalse()
        {
            var smallManager = new SpellSlotManager(maxSlots: 2, maxLevel: 5);

            smallManager.TryCreateSpell("spell_a", SpellKind.AoE);
            smallManager.TryCreateSpell("spell_b", SpellKind.Projectile);

            bool added = smallManager.TryCreateSpell("spell_c", SpellKind.Orbit);

            Assert.That(added, Is.False);
            Assert.That(smallManager.SpellCount, Is.EqualTo(2));
            Assert.That(smallManager.HasFreeSlot, Is.False);
        }

        // --- Clear ---

        [Test]
        public void Clear_RemovesAllSpells()
        {
            _manager.TryCreateSpell("alev_halkasi", SpellKind.Orbit);
            _manager.TryCreateSpell("kilic_firtinasi", SpellKind.Projectile);

            _manager.Clear();

            Assert.That(_manager.SpellCount, Is.EqualTo(0));
            Assert.That(_manager.HasFreeSlot, Is.True);
            Assert.That(_manager.IsSpellOwned("alev_halkasi"), Is.False);
        }
    }
}
