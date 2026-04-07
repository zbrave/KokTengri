using KokTengri.Core;
using KokTengri.Gameplay;
using NUnit.Framework;
using UnityEngine;

namespace KokTengri.Tests.Integration
{
    /// <summary>
    /// Integration tests for DamageCalculator verifying spell damage formulas,
    /// element affinity tables, and class bonus rules match design spec.
    /// </summary>
    [TestFixture]
    public sealed class DamageCalculatorIntegrationTests
    {
        // --- Single Element Damage ---

        [Test]
        public void CalculateSpellDamage_Level1BaseDamage10_NoBonus_Returns10()
        {
            int damage = DamageCalculator.CalculateSpellDamage(
                baseDamage: 10f,
                spellLevel: 1,
                spellElement: ElementType.Od,
                targetEnemyType: EnemyType.KaraKurt,
                heroClass: HeroClass.None);

            // level_multiplier = 1 + 0.25*(1-1) = 1.0
            // element: Od vs KaraKurt (affinity[0,0]) = 1.5 (Weakness)
            // class: None = 1.0
            // raw = 10 * 1.0 * 1.5 * 1.0 * 1.0 = 15
            Assert.That(damage, Is.EqualTo(15));
        }

        [Test]
        public void CalculateSpellDamage_Level3BaseDamage12_OdVsGolAynasi_ReturnsCorrectValue()
        {
            int damage = DamageCalculator.CalculateSpellDamage(
                baseDamage: 12f,
                spellLevel: 3,
                spellElement: ElementType.Od,
                targetEnemyType: EnemyType.GolAynasi,
                heroClass: HeroClass.None);

            // level_multiplier = 1 + 0.25*(3-1) = 1.5
            // element: Od vs GolAynasi (affinity[0,5]) = 1.5 (Weakness)
            // class: None = 1.0
            // raw = 12 * 1.5 * 1.5 * 1.0 * 1.0 = 27
            Assert.That(damage, Is.EqualTo(27));
        }

        [Test]
        public void CalculateSpellDamage_ResistantElement_ReducesDamage()
        {
            int damage = DamageCalculator.CalculateSpellDamage(
                baseDamage: 10f,
                spellLevel: 1,
                spellElement: ElementType.Od,
                targetEnemyType: EnemyType.YekUsagi,
                heroClass: HeroClass.None);

            // affinity[0,3] = 0.6 (Resistant)
            // raw = 10 * 1.0 * 0.6 * 1.0 = 6
            Assert.That(damage, Is.EqualTo(6));
        }

        [Test]
        public void CalculateSpellDamage_NeutralElement_ReturnsBaseTimesLevel()
        {
            int damage = DamageCalculator.CalculateSpellDamage(
                baseDamage: 20f,
                spellLevel: 2,
                spellElement: ElementType.Od,
                targetEnemyType: EnemyType.Cor,
                heroClass: HeroClass.None);

            // affinity[0,1] = 1.0 (Neutral), level = 1 + 0.25*(2-1) = 1.25
            // raw = 20 * 1.25 * 1.0 * 1.0 = 25
            Assert.That(damage, Is.EqualTo(25));
        }

        // --- Dual Element Damage ---

        [Test]
        public void CalculateSpellDamage_DualElement_TakesBestMultiplier()
        {
            int damage = DamageCalculator.CalculateSpellDamage(
                baseDamage: 10f,
                spellLevel: 1,
                elementA: ElementType.Od,
                elementB: ElementType.Temur,
                targetEnemyType: EnemyType.KaraKurt,
                heroClass: HeroClass.None);

            // Od vs KaraKurt = 1.5 (Weakness), Temur vs KaraKurt = 1.0 (Neutral)
            // Max = 1.5, raw = 10 * 1.0 * 1.5 * 1.0 = 15
            Assert.That(damage, Is.EqualTo(15));
        }

        // --- Class Bonus ---

        [Test]
        public void CalculateSpellDamage_KamWithDualElement_GetsBonus()
        {
            int singleDamage = DamageCalculator.CalculateSpellDamage(
                baseDamage: 10f, spellLevel: 1,
                elementA: ElementType.Od, elementB: ElementType.Temur,
                targetEnemyType: EnemyType.KaraKurt, heroClass: HeroClass.None);

            int kamDamage = DamageCalculator.CalculateSpellDamage(
                baseDamage: 10f, spellLevel: 1,
                elementA: ElementType.Od, elementB: ElementType.Temur,
                targetEnemyType: EnemyType.KaraKurt, heroClass: HeroClass.Kam);

            // Kam dual element bonus = 1.15x
            Assert.That(kamDamage, Is.GreaterThan(singleDamage));
        }

        // --- Meta Power Multiplier ---

        [Test]
        public void CalculateSpellDamage_WithMetaPowerMultiplier_ScalesDamage()
        {
            int baseDamage = DamageCalculator.CalculateSpellDamage(
                baseDamage: 10f, spellLevel: 1,
                spellElement: ElementType.Od,
                targetEnemyType: EnemyType.KaraKurt,
                heroClass: HeroClass.None,
                metaPowerMultiplier: 1.0f);

            int boostedDamage = DamageCalculator.CalculateSpellDamage(
                baseDamage: 10f, spellLevel: 1,
                spellElement: ElementType.Od,
                targetEnemyType: EnemyType.KaraKurt,
                heroClass: HeroClass.None,
                metaPowerMultiplier: 2.0f);

            Assert.That(boostedDamage, Is.EqualTo(baseDamage * 2));
        }

        // --- Minimum Damage Floor ---

        [Test]
        public void CalculateSpellDamage_VeryLowBaseDamage_AlwaysAtLeast1()
        {
            int damage = DamageCalculator.CalculateSpellDamage(
                baseDamage: 0.01f,
                spellLevel: 1,
                spellElement: ElementType.Od,
                targetEnemyType: EnemyType.YekUsagi,
                heroClass: HeroClass.None,
                metaPowerMultiplier: 0.01f);

            Assert.That(damage, Is.GreaterThanOrEqualTo(1));
        }

        // --- Enemy Contact Damage ---

        [Test]
        public void CalculateEnemyContactDamage_AtTimeZero_ReturnsBaseDamage()
        {
            int damage = DamageCalculator.CalculateEnemyContactDamage(
                baseContactDamage: 10f,
                elapsedMinutes: 0f);

            Assert.That(damage, Is.EqualTo(10));
        }

        [Test]
        public void CalculateEnemyContactDamage_After5Minutes_IncreasesByTimeFactor()
        {
            int damage = DamageCalculator.CalculateEnemyContactDamage(
                baseContactDamage: 10f,
                elapsedMinutes: 5f);

            // time_mult = 1.0 + 0.08 * 5 = 1.4, raw = 10 * 1.4 = 14
            Assert.That(damage, Is.EqualTo(14));
        }

        // --- Level Scaling Consistency ---

        [Test]
        public void CalculateSpellDamage_Level5_Has25PercentMoreMultiplierThanLevel1()
        {
            int level1 = DamageCalculator.CalculateSpellDamage(
                baseDamage: 20f, spellLevel: 1,
                spellElement: ElementType.Od,
                targetEnemyType: EnemyType.Cor,
                heroClass: HeroClass.None);

            int level5 = DamageCalculator.CalculateSpellDamage(
                baseDamage: 20f, spellLevel: 5,
                spellElement: ElementType.Od,
                targetEnemyType: EnemyType.Cor,
                heroClass: HeroClass.None);

            // Level multiplier: L1=1.0, L5=2.0
            // level5 should be exactly 2x level1 for neutral element
            Assert.That(level5, Is.EqualTo(level1 * 2));
        }
    }
}
