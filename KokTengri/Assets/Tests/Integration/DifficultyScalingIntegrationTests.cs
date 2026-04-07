using KokTengri.Core;
using KokTengri.Gameplay;
using NUnit.Framework;
using UnityEngine;

namespace KokTengri.Tests.Integration
{
    /// <summary>
    /// Integration tests for DifficultyScaling verifying that multipliers
    /// scale correctly over time, with hero mode and elite modifiers.
    /// </summary>
    [TestFixture]
    public sealed class DifficultyScalingIntegrationTests
    {
        [Test]
        public void GetHpMultiplier_AtTimeZero_ReturnsOne()
        {
            float multiplier = DifficultyScaling.GetHpMultiplier(0f, null);

            Assert.That(multiplier, Is.EqualTo(1.0f).Within(0.001f));
        }

        [Test]
        public void GetHpMultiplier_After5Minutes_ScalesLinearly()
        {
            float multiplier = DifficultyScaling.GetHpMultiplier(5f, null);

            // default slope = 0.12 per minute, 1 + 0.12*5 = 1.6
            Assert.That(multiplier, Is.EqualTo(1.6f).Within(0.001f));
        }

        [Test]
        public void GetSpawnMultiplier_After10Minutes_ScalesLinearly()
        {
            float multiplier = DifficultyScaling.GetSpawnMultiplier(10f, null);

            // default slope = 0.10 per minute, 1 + 0.10*10 = 2.0
            Assert.That(multiplier, Is.EqualTo(2.0f).Within(0.001f));
        }

        [Test]
        public void GetDamageMultiplier_After3Minutes_ScalesLinearly()
        {
            float multiplier = DifficultyScaling.GetDamageMultiplier(3f, null);

            // default slope = 0.08 per minute, 1 + 0.08*3 = 1.24
            Assert.That(multiplier, Is.EqualTo(1.24f).Within(0.001f));
        }

        // --- Hero Mode Multipliers ---

        [Test]
        public void GetFinalHpMultiplier_HeroMode_AppliesHeroMultiplier()
        {
            float baseHp = DifficultyScaling.GetHpMultiplier(5f, null);
            float heroHp = DifficultyScaling.GetFinalHpMultiplier(5f, isHeroMode: true, isElite: false, null);

            // hero mode multiplier = 1.5
            Assert.That(heroHp, Is.EqualTo(baseHp * 1.5f).Within(0.001f));
        }

        [Test]
        public void GetFinalHpMultiplier_Elite_AppliesEliteMultiplier()
        {
            float baseHp = DifficultyScaling.GetHpMultiplier(5f, null);
            float eliteHp = DifficultyScaling.GetFinalHpMultiplier(5f, isHeroMode: false, isElite: true, null);

            // elite multiplier = 3.0
            Assert.That(eliteHp, Is.EqualTo(baseHp * 3.0f).Within(0.001f));
        }

        [Test]
        public void GetFinalHpMultiplier_HeroAndElite_StacksBothMultipliers()
        {
            float baseHp = DifficultyScaling.GetHpMultiplier(5f, null);
            float combined = DifficultyScaling.GetFinalHpMultiplier(5f, isHeroMode: true, isElite: true, null);

            // hero (1.5) * elite (3.0) = 4.5
            Assert.That(combined, Is.EqualTo(baseHp * 1.5f * 3.0f).Within(0.001f));
        }

        [Test]
        public void GetFinalSpawnMultiplier_HeroMode_AppliesHeroSpawnMultiplier()
        {
            float baseSpawn = DifficultyScaling.GetSpawnMultiplier(5f, null);
            float heroSpawn = DifficultyScaling.GetFinalSpawnMultiplier(5f, isHeroMode: true, null);

            // hero mode spawn multiplier = 1.5
            Assert.That(heroSpawn, Is.EqualTo(baseSpawn * 1.5f).Within(0.001f));
        }

        // --- XP Multiplier ---

        [Test]
        public void GetEliteXpMultiplier_WithDefaultConfig_Returns3x()
        {
            float xpMult = DifficultyScaling.GetEliteXpMultiplier(null);

            Assert.That(xpMult, Is.EqualTo(3.0f).Within(0.001f));
        }

        // --- Negative Time Clamped ---

        [Test]
        public void GetHpMultiplier_NegativeTime_ClampedToZero()
        {
            float multiplier = DifficultyScaling.GetHpMultiplier(-5f, null);

            Assert.That(multiplier, Is.EqualTo(1.0f).Within(0.001f));
        }
    }
}
