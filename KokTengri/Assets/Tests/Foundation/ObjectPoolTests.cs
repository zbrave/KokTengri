using System;
using NUnit.Framework;
using UnityEngine;
using KokTengri.Core;

namespace KokTengri.Tests.Foundation
{
    public sealed class ObjectPoolTests
    {
        private readonly System.Collections.Generic.List<GameObject> _createdGameObjects = new();

        [TearDown]
        public void TearDown()
        {
            for (int i = 0; i < _createdGameObjects.Count; i++)
            {
                if (_createdGameObjects[i] != null)
                {
                    UnityEngine.Object.DestroyImmediate(_createdGameObjects[i]);
                }
            }

            _createdGameObjects.Clear();
        }

        [Test]
        public void Prewarm_CreatesConfiguredInactiveObjects()
        {
            TestPooledComponent prefab = CreatePrefab("prefab");
            ObjectPool<TestPooledComponent> pool = new ObjectPool<TestPooledComponent>(prefab, initialSize: 0, maxSize: 8, PoolOverflowPolicy.Expand);

            pool.Prewarm(3);

            Assert.That(pool.TotalCount, Is.EqualTo(3));
            Assert.That(pool.InactiveCount, Is.EqualTo(3));
            Assert.That(pool.ActiveCount, Is.EqualTo(0));
            Assert.That(prefab.CreateCount, Is.EqualTo(0));
        }

        [Test]
        public void TryTake_ReturnsPooledObjectAndCallsOnPoolTake()
        {
            TestPooledComponent prefab = CreatePrefab("prefab");
            ObjectPool<TestPooledComponent> pool = new ObjectPool<TestPooledComponent>(prefab, initialSize: 2, maxSize: 4, PoolOverflowPolicy.Expand);

            bool tookObject = pool.TryTake(out TestPooledComponent instance);

            Assert.That(tookObject, Is.True);
            Assert.That(instance, Is.Not.Null);
            Assert.That(instance.TakeCount, Is.EqualTo(1));
            Assert.That(instance.IsActive, Is.True);
            Assert.That(pool.ActiveCount, Is.EqualTo(1));
            Assert.That(pool.InactiveCount, Is.EqualTo(1));
        }

        [Test]
        public void Return_CallsOnPoolReturnAndMovesObjectToInactiveQueue()
        {
            TestPooledComponent prefab = CreatePrefab("prefab");
            ObjectPool<TestPooledComponent> pool = new ObjectPool<TestPooledComponent>(prefab, initialSize: 1, maxSize: 2, PoolOverflowPolicy.Expand);

            pool.TryTake(out TestPooledComponent instance);

            bool returned = pool.Return(instance);

            Assert.That(returned, Is.True);
            Assert.That(instance.ReturnCount, Is.EqualTo(1));
            Assert.That(instance.IsActive, Is.False);
            Assert.That(pool.ActiveCount, Is.EqualTo(0));
            Assert.That(pool.InactiveCount, Is.EqualTo(1));
        }

        [Test]
        public void TryTake_WhenExpandOverflowEnabled_CreatesNewObjectAtCapacityBoundary()
        {
            TestPooledComponent prefab = CreatePrefab("prefab");
            ObjectPool<TestPooledComponent> pool = new ObjectPool<TestPooledComponent>(prefab, initialSize: 1, maxSize: 2, PoolOverflowPolicy.Expand);

            pool.TryTake(out TestPooledComponent first);
            bool tookSecond = pool.TryTake(out TestPooledComponent second);

            Assert.That(tookSecond, Is.True);
            Assert.That(second, Is.Not.Null);
            Assert.That(second, Is.Not.SameAs(first));
            Assert.That(second.CreateCount, Is.EqualTo(1));
            Assert.That(pool.TotalCount, Is.EqualTo(2));
            Assert.That(pool.ActiveCount, Is.EqualTo(2));
        }

        [Test]
        public void TryTake_WhenReturnNullOverflowEnabled_ReturnsFalseAtCapacity()
        {
            TestPooledComponent prefab = CreatePrefab("prefab");
            ObjectPool<TestPooledComponent> pool = new ObjectPool<TestPooledComponent>(prefab, initialSize: 1, maxSize: 1, PoolOverflowPolicy.ReturnNull);

            pool.TryTake(out _);
            bool tookSecond = pool.TryTake(out TestPooledComponent second);

            Assert.That(tookSecond, Is.False);
            Assert.That(second, Is.Null);
            Assert.That(pool.TotalCount, Is.EqualTo(1));
            Assert.That(pool.ActiveCount, Is.EqualTo(1));
            Assert.That(pool.InactiveCount, Is.EqualTo(0));
        }

        [Test]
        public void TryTake_WhenRecycleOldestOverflowEnabled_RecyclesOldestActiveObject()
        {
            TestPooledComponent prefab = CreatePrefab("prefab");
            ObjectPool<TestPooledComponent> pool = new ObjectPool<TestPooledComponent>(prefab, initialSize: 2, maxSize: 2, PoolOverflowPolicy.RecycleOldest);

            pool.TryTake(out TestPooledComponent first);
            pool.TryTake(out TestPooledComponent second);

            bool tookThird = pool.TryTake(out TestPooledComponent recycled);

            Assert.That(tookThird, Is.True);
            Assert.That(recycled, Is.SameAs(first));
            Assert.That(first.ReturnCount, Is.EqualTo(1));
            Assert.That(first.TakeCount, Is.EqualTo(2));
            Assert.That(pool.TotalCount, Is.EqualTo(2));
            Assert.That(pool.ActiveCount, Is.EqualTo(2));
            Assert.That(pool.InactiveCount, Is.EqualTo(0));
        }

        [Test]
        public void TryTakeAndReturn_DoNotAllocateAfterPrewarm()
        {
            TestPooledComponent prefab = CreatePrefab("prefab");
            ObjectPool<TestPooledComponent> pool = new ObjectPool<TestPooledComponent>(prefab, initialSize: 1, maxSize: 1, PoolOverflowPolicy.Expand);

            pool.TryTake(out TestPooledComponent warmInstance);
            pool.Return(warmInstance);

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            long before = GC.GetAllocatedBytesForCurrentThread();

            for (int i = 0; i < 128; i++)
            {
                bool tookObject = pool.TryTake(out TestPooledComponent instance);
                bool returned = pool.Return(instance);

                Assert.That(tookObject, Is.True);
                Assert.That(returned, Is.True);
            }

            long after = GC.GetAllocatedBytesForCurrentThread();

            Assert.That(after - before, Is.EqualTo(0L));
        }

        private TestPooledComponent CreatePrefab(string name)
        {
            GameObject gameObject = new GameObject(name);
            _createdGameObjects.Add(gameObject);

            TestPooledComponent component = gameObject.AddComponent<TestPooledComponent>();
            component.gameObject.SetActive(false);
            return component;
        }

        private sealed class TestPooledComponent : MonoBehaviour, IPooledObject
        {
            public int CreateCount { get; private set; }
            public int TakeCount { get; private set; }
            public int ReturnCount { get; private set; }
            public int DestroyCount { get; private set; }

            public bool IsActive { get; private set; }

            public void OnPoolCreate()
            {
                CreateCount++;
                IsActive = false;
            }

            public void OnPoolTake()
            {
                TakeCount++;
                IsActive = true;
            }

            public void OnPoolReturn()
            {
                ReturnCount++;
                IsActive = false;
            }

            public void OnPoolDestroy()
            {
                DestroyCount++;
                IsActive = false;
            }
        }
    }
}
