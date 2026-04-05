using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using NUnit.Framework;
using UnityEngine;
using KokTengri.Core;

namespace KokTengri.Tests.Foundation
{
    public sealed class EventBusTests
    {
        [SetUp]
        public void SetUp()
        {
            EventBus.Reset();
        }

        [TearDown]
        public void TearDown()
        {
            EventBus.Reset();
        }

        [Test]
        public void test_event_bus_subscribe_publish_invokes_listener()
        {
            var wasCalled = false;
            var received = default(PlayerDamagedEvent);

            void Listener(PlayerDamagedEvent eventData)
            {
                wasCalled = true;
                received = eventData;
            }

            EventBus.Subscribe<PlayerDamagedEvent>(Listener);

            var published = new PlayerDamagedEvent(12, 88, 17, 1.25f);
            EventBus.Publish(published);

            Assert.That(wasCalled, Is.True);
            Assert.That(received.DamageAmount, Is.EqualTo(12));
            Assert.That(received.CurrentHp, Is.EqualTo(88));
            Assert.That(received.SourceId, Is.EqualTo(17));
            Assert.That(received.RunTime, Is.EqualTo(1.25f));
        }

        [Test]
        public void test_event_bus_unsubscribe_removed_listener_is_not_invoked()
        {
            var callCount = 0;

            void Listener(PlayerDamagedEvent _)
            {
                callCount++;
            }

            EventBus.Subscribe<PlayerDamagedEvent>(Listener);
            EventBus.Unsubscribe<PlayerDamagedEvent>(Listener);

            EventBus.Publish(new PlayerDamagedEvent(4, 96, 2, 0.5f));

            Assert.That(callCount, Is.EqualTo(0));
        }

        [Test]
        public void test_event_bus_multiple_listeners_publish_invokes_all()
        {
            var listenerOneCalls = 0;
            var listenerTwoCalls = 0;
            var listenerThreeCalls = 0;

            void ListenerOne(PlayerDamagedEvent _) => listenerOneCalls++;
            void ListenerTwo(PlayerDamagedEvent _) => listenerTwoCalls++;
            void ListenerThree(PlayerDamagedEvent _) => listenerThreeCalls++;

            EventBus.Subscribe<PlayerDamagedEvent>(ListenerOne);
            EventBus.Subscribe<PlayerDamagedEvent>(ListenerTwo);
            EventBus.Subscribe<PlayerDamagedEvent>(ListenerThree);

            EventBus.Publish(new PlayerDamagedEvent(9, 91, 8, 2f));

            Assert.That(listenerOneCalls, Is.EqualTo(1));
            Assert.That(listenerTwoCalls, Is.EqualTo(1));
            Assert.That(listenerThreeCalls, Is.EqualTo(1));
        }

        [Test]
        public void test_event_bus_dispatch_order_fifo_uses_subscription_order()
        {
            var callOrder = new List<string>();

            void First(PlayerDamagedEvent _) => callOrder.Add("First");
            void Second(PlayerDamagedEvent _) => callOrder.Add("Second");
            void Third(PlayerDamagedEvent _) => callOrder.Add("Third");

            EventBus.Subscribe<PlayerDamagedEvent>(First);
            EventBus.Subscribe<PlayerDamagedEvent>(Second);
            EventBus.Subscribe<PlayerDamagedEvent>(Third);

            EventBus.Publish(new PlayerDamagedEvent(1, 99, 5, 0.1f));

            CollectionAssert.AreEqual(new[] { "First", "Second", "Third" }, callOrder);
        }

        [Test]
        public void test_event_bus_reentrancy_nested_publish_is_queued_after_current_dispatch()
        {
            var callOrder = new List<string>();

            void DamageListenerA(PlayerDamagedEvent _)
            {
                callOrder.Add("damage-a");
                EventBus.Publish(new XPCollectedEvent(3, 42, Vector3.one, 2f));
            }

            void DamageListenerB(PlayerDamagedEvent _)
            {
                callOrder.Add("damage-b");
            }

            void XpListener(XPCollectedEvent _)
            {
                callOrder.Add("xp");
            }

            EventBus.Subscribe<PlayerDamagedEvent>(DamageListenerA);
            EventBus.Subscribe<PlayerDamagedEvent>(DamageListenerB);
            EventBus.Subscribe<XPCollectedEvent>(XpListener);

            EventBus.Publish(new PlayerDamagedEvent(6, 72, 15, 2f));

            CollectionAssert.AreEqual(new[] { "damage-a", "damage-b", "xp" }, callOrder);
        }

        [Test]
        public void test_event_bus_type_safety_listener_only_receives_matching_event_type()
        {
            var damageCalls = 0;
            var xpCalls = 0;

            void DamageListener(PlayerDamagedEvent _) => damageCalls++;
            void XpListener(XPCollectedEvent _) => xpCalls++;

            EventBus.Subscribe<PlayerDamagedEvent>(DamageListener);
            EventBus.Subscribe<XPCollectedEvent>(XpListener);

            EventBus.Publish(new XPCollectedEvent(9, 101, Vector3.zero, 3f));

            Assert.That(damageCalls, Is.EqualTo(0));
            Assert.That(xpCalls, Is.EqualTo(1));
        }

        [Test]
        public void test_event_bus_hot_path_publish_allocates_zero_gc_after_warmup()
        {
            void Listener(XPCollectedEvent _) { }

            EventBus.Subscribe<XPCollectedEvent>(Listener);

            var warmupEvent = new XPCollectedEvent(1, 1, Vector3.zero, 0f);
            EventBus.Publish(warmupEvent);

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            var beforeBytes = GC.GetAllocatedBytesForCurrentThread();

            for (var i = 0; i < 128; i++)
            {
                EventBus.Publish(warmupEvent);
            }

            var afterBytes = GC.GetAllocatedBytesForCurrentThread();

            Assert.That(afterBytes - beforeBytes, Is.EqualTo(0L));
        }

        [Test]
        public void test_event_channel_publish_subscribe_relays_through_event_bus()
        {
            var channel = ScriptableObject.CreateInstance<PlayerDamagedEventChannelSO>();
            var wasCalled = false;

            void Listener(PlayerDamagedEvent eventData)
            {
                wasCalled = eventData.DamageAmount == 7;
            }

            channel.Subscribe(Listener);
            channel.Publish(new PlayerDamagedEvent(7, 93, 4, 0.75f));

            Assert.That(wasCalled, Is.True);
        }

        [Test]
        public void test_event_definitions_high_frequency_payloads_are_structs()
        {
            Assert.That(typeof(PlayerDamagedEvent).IsValueType, Is.True);
            Assert.That(typeof(XPCollectedEvent).IsValueType, Is.True);
            Assert.That(typeof(RunEndEvent).IsValueType, Is.True);

            Assert.That(Marshal.SizeOf<PlayerDamagedEvent>(), Is.GreaterThan(0));
            Assert.That(Marshal.SizeOf<XPCollectedEvent>(), Is.GreaterThan(0));
            Assert.That(Marshal.SizeOf<RunEndEvent>(), Is.GreaterThan(0));
        }
    }
}
