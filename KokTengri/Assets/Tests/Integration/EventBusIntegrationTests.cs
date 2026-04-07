using System.Collections.Generic;
using KokTengri.Core;
using KokTengri.Gameplay;
using NUnit.Framework;
using UnityEngine;

namespace KokTengri.Tests.Integration
{
    /// <summary>
    /// Integration tests verifying EventBus-driven event flows between
    /// Sprint 1 systems: Run lifecycle, XP gain, level-up, and spell crafting.
    /// </summary>
    [TestFixture]
    public sealed class EventBusIntegrationTests
    {
        private readonly List<object> _publishedEvents = new();

        [SetUp]
        public void SetUp()
        {
            EventBus.Reset();
            _publishedEvents.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            EventBus.Reset();
        }

        // --- Run Lifecycle Event Chain ---

        [Test]
        public void RunStartEvent_ReachesMultipleSubscribers()
        {
            var receivedByA = false;
            var receivedByB = false;

            EventBus.Subscribe<RunStartEvent>(e => receivedByA = true);
            EventBus.Subscribe<RunStartEvent>(e => receivedByB = true);

            EventBus.Publish(new RunStartEvent(1, "kam_01", "Kam", 42));

            Assert.That(receivedByA, Is.True);
            Assert.That(receivedByB, Is.True);
        }

        [Test]
        public void RunEndEvent_CarriesCorrectPayload()
        {
            RunEndEvent received = default;
            EventBus.Subscribe<RunEndEvent>(e => received = e);

            EventBus.Publish(new RunEndEvent(42, RunEndResultType.Victory, 180.5f, 15, 2));

            Assert.That(received.Result, Is.EqualTo(RunEndResultType.Victory));
            Assert.That(received.RunId, Is.EqualTo(42));
            Assert.That(received.Kills, Is.EqualTo(15));
            Assert.That(received.BossesDefeated, Is.EqualTo(2));
            Assert.That(received.SurvivedSeconds, Is.EqualTo(180.5f));
        }

        // --- EnemyDeath → XP Event Chain ---

        [Test]
        public void EnemyDeathEvent_CarriesCorrectPayload()
        {
            EnemyDeathEvent received = default;
            EventBus.Subscribe<EnemyDeathEvent>(e => received = e);

            EventBus.Publish(new EnemyDeathEvent(
                42, EnemyType.KaraKurt, Vector3.zero, true, 5.2f));

            Assert.That(received.EnemyId, Is.EqualTo(42));
            Assert.That(received.EnemyType, Is.EqualTo(EnemyType.KaraKurt));
            Assert.That(received.IsElite, Is.True);
            Assert.That(received.RunTime, Is.EqualTo(5.2f));
        }

        [Test]
        public void XPCollectedEvent_CarriesCorrectPayload()
        {
            XPCollectedEvent received = default;
            EventBus.Subscribe<XPCollectedEvent>(e => received = e);

            EventBus.Publish(new XPCollectedEvent(25, 1, Vector3.one, 10.5f));

            Assert.That(received.Amount, Is.EqualTo(25));
            Assert.That(received.CollectorId, Is.EqualTo(1));
            Assert.That(received.RunTime, Is.EqualTo(10.5f));
        }

        // --- Crafting Event Propagation ---

        [Test]
        public void SpellCraftedEvent_ReachesAllSubscribers()
        {
            var craftedReceived = false;
            var craftedSpellId = string.Empty;

            EventBus.Subscribe<SpellCraftedEvent>(e =>
            {
                craftedReceived = true;
                craftedSpellId = e.SpellId;
            });

            EventBus.Publish(new SpellCraftedEvent("alev_halkasi", 1, SpellKind.Orbit));

            Assert.That(craftedReceived, Is.True);
            Assert.That(craftedSpellId, Is.EqualTo("alev_halkasi"));
        }

        [Test]
        public void SpellUpgradedEvent_CarriesNewLevel()
        {
            SpellUpgradedEvent received = default;
            EventBus.Subscribe<SpellUpgradedEvent>(e => received = e);

            EventBus.Publish(new SpellUpgradedEvent("kilic_firtinasi", 3));

            Assert.That(received.SpellId, Is.EqualTo("kilic_firtinasi"));
            Assert.That(received.NewLevel, Is.EqualTo(3));
        }

        // --- Wave Events ---

        [Test]
        public void WaveCompletedEvent_CarriesWaveNumber()
        {
            WaveCompletedEvent received = default;
            EventBus.Subscribe<WaveCompletedEvent>(e => received = e);

            EventBus.Publish(new WaveCompletedEvent(5, 8, 3.2f));

            Assert.That(received.WaveIndex, Is.EqualTo(5));
            Assert.That(received.RemainingEnemies, Is.EqualTo(8));
        }

        [Test]
        public void BossSpawnedEvent_ReachesSubscribers()
        {
            var bossReceived = false;
            EventBus.Subscribe<BossSpawnedEvent>(e => bossReceived = true);

            EventBus.Publish(new BossSpawnedEvent("albasti", Vector3.zero, 7.5f));

            Assert.That(bossReceived, Is.True);
        }

        // --- Level Up Event Chain ---

        [Test]
        public void LevelUpEvent_CarriesLevelAndRunTime()
        {
            LevelUpEvent received = default;
            EventBus.Subscribe<LevelUpEvent>(e => received = e);

            EventBus.Publish(new LevelUpEvent(5, 0f, 120f));

            Assert.That(received.NewLevel, Is.EqualTo(5));
            Assert.That(received.OverflowXp, Is.EqualTo(0f));
            Assert.That(received.RunTime, Is.EqualTo(120f));
        }

        // --- Event Isolation Between Types ---

        [Test]
        public void DifferentEventTypes_DoNotCrossContaminate()
        {
            var spellCraftedCount = 0;
            var spellUpgradedCount = 0;

            EventBus.Subscribe<SpellCraftedEvent>(e => spellCraftedCount++);
            EventBus.Subscribe<SpellUpgradedEvent>(e => spellUpgradedCount++);

            EventBus.Publish(new SpellCraftedEvent("alev_halkasi", 1, SpellKind.Orbit));
            EventBus.Publish(new SpellUpgradedEvent("alev_halkasi", 2));
            EventBus.Publish(new SpellCraftedEvent("kaya_kalkani", 1, SpellKind.Orbit));

            Assert.That(spellCraftedCount, Is.EqualTo(2));
            Assert.That(spellUpgradedCount, Is.EqualTo(1));
        }

        // --- FIFO Ordering ---

        [Test]
        public void Events_ProcessInFIFOOrder()
        {
            var order = new List<string>();

            EventBus.Subscribe<RunStartEvent>(e => order.Add("start"));
            EventBus.Subscribe<RunEndEvent>(e => order.Add("end"));

            EventBus.Publish(new RunStartEvent(1, "kam_01", "Kam", 1));
            EventBus.Publish(new RunEndEvent(1, RunEndResultType.Defeat, 60f, 5, 0));

            Assert.That(order, Is.EqualTo(new[] { "start", "end" }));
        }
    }
}
