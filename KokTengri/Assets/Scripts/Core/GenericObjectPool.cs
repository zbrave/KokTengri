using System;
using System.Collections.Generic;
using UnityEngine;

namespace KokTengri.Core
{
    public sealed class ObjectPool<T> : IDisposable where T : Component, IPooledObject
    {
        private readonly T _prefab;
        private readonly Transform _parent;
        private readonly Queue<T> _inactiveQueue;
        private readonly HashSet<T> _activeSet;
        private readonly Queue<T> _activeOrderQueue;
        private readonly PoolOverflowPolicy _overflowPolicy;
        private readonly int _maxSize;

        public ObjectPool(T prefab, int initialSize, int maxSize, PoolOverflowPolicy overflowPolicy, Transform parent = null)
        {
            if (prefab == null)
            {
                throw new ArgumentNullException(nameof(prefab));
            }

            if (maxSize <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxSize), "Max size must be greater than zero.");
            }

            if (initialSize < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(initialSize), "Initial size cannot be negative.");
            }

            if (initialSize > maxSize)
            {
                throw new ArgumentOutOfRangeException(nameof(initialSize), "Initial size cannot exceed max size.");
            }

            _prefab = prefab;
            _parent = parent;
            _maxSize = maxSize;
            _overflowPolicy = overflowPolicy;

            int capacity = Mathf.Max(initialSize, maxSize, 1);
            _inactiveQueue = new Queue<T>(capacity);
            _activeSet = new HashSet<T>(capacity);
            _activeOrderQueue = new Queue<T>(capacity);

            if (initialSize > 0)
            {
                Prewarm(initialSize);
            }
        }

        public int TotalCount => _inactiveQueue.Count + _activeSet.Count;

        public int ActiveCount => _activeSet.Count;

        public int InactiveCount => _inactiveQueue.Count;

        public void Prewarm(int count)
        {
            if (count <= 0)
            {
                return;
            }

            int targetCount = Mathf.Min(_maxSize, TotalCount + count);

            while (TotalCount < targetCount)
            {
                T instance = CreateInstance();
                ReturnToInactiveState(instance);
            }
        }

        public bool TryTake(out T instance)
        {
            if (_inactiveQueue.Count > 0)
            {
                instance = _inactiveQueue.Dequeue();
                ActivateInstance(instance);
                return true;
            }

            if (TotalCount < _maxSize)
            {
                instance = CreateInstance();
                ActivateInstance(instance);
                return true;
            }

            switch (_overflowPolicy)
            {
                case PoolOverflowPolicy.Expand:
                    instance = CreateInstance();
                    ActivateInstance(instance);
                    return true;
                case PoolOverflowPolicy.ReturnNull:
                    instance = null;
                    return false;
                case PoolOverflowPolicy.RecycleOldest:
                    if (TryRecycleOldest(out instance))
                    {
                        return true;
                    }

                    instance = null;
                    return false;
                default:
                    instance = null;
                    return false;
            }
        }

        public bool Return(T instance)
        {
            if (instance == null)
            {
                return false;
            }

            if (!_activeSet.Remove(instance))
            {
                return false;
            }

            instance.OnPoolReturn();
            instance.gameObject.SetActive(false);
            _inactiveQueue.Enqueue(instance);
            return true;
        }

        public void Dispose()
        {
            DestroyQueue(_inactiveQueue);
            DestroySet(_activeSet);
            _activeOrderQueue.Clear();
            _activeSet.Clear();
        }

        private T CreateInstance()
        {
            T instance = UnityEngine.Object.Instantiate(_prefab, _parent);
            instance.OnPoolCreate();
            instance.gameObject.SetActive(false);
            return instance;
        }

        private void ActivateInstance(T instance)
        {
            _activeSet.Add(instance);
            _activeOrderQueue.Enqueue(instance);
            instance.gameObject.SetActive(true);
            instance.OnPoolTake();
        }

        private void ReturnToInactiveState(T instance)
        {
            instance.gameObject.SetActive(false);
            _inactiveQueue.Enqueue(instance);
        }

        private bool TryRecycleOldest(out T instance)
        {
            while (_activeOrderQueue.Count > 0)
            {
                T candidate = _activeOrderQueue.Dequeue();
                if (!_activeSet.Remove(candidate))
                {
                    continue;
                }

                candidate.OnPoolReturn();
                candidate.gameObject.SetActive(false);
                ActivateInstance(candidate);
                instance = candidate;
                return true;
            }

            instance = null;
            return false;
        }

        private static void DestroyQueue(Queue<T> queue)
        {
            while (queue.Count > 0)
            {
                T instance = queue.Dequeue();
                if (instance == null)
                {
                    continue;
                }

                instance.OnPoolDestroy();
                UnityEngine.Object.Destroy(instance.gameObject);
            }
        }

        private static void DestroySet(HashSet<T> set)
        {
            foreach (T instance in set)
            {
                if (instance == null)
                {
                    continue;
                }

                instance.OnPoolDestroy();
                UnityEngine.Object.Destroy(instance.gameObject);
            }
        }
    }
}
