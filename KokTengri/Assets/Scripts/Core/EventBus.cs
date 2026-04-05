using System;
using System.Collections.Generic;
using UnityEngine;

namespace KokTengri.Core
{
    public interface IEventBus
    {
        void Subscribe<T>(Action<T> listener);
        void Unsubscribe<T>(Action<T> listener);
        void Publish<T>(T eventData);
    }

    public static class EventBus
    {
        private static readonly Dictionary<Type, List<Delegate>> _listenersByType = new();
        private static readonly Dictionary<Type, List<Delegate>> _dispatchBuffersByType = new();
        private static readonly Dictionary<Type, IQueuedEventStore> _queuedEventsByType = new();
        private static readonly Queue<QueuedEventToken> _queuedDispatches = new();

        private static bool _isDispatching;

        public static void Subscribe<T>(Action<T> listener)
        {
            if (listener == null)
            {
                throw new ArgumentNullException(nameof(listener));
            }

            var eventType = typeof(T);

            if (!_listenersByType.TryGetValue(eventType, out var listeners))
            {
                listeners = new List<Delegate>();
                _listenersByType.Add(eventType, listeners);
            }

            if (listeners.Contains(listener))
            {
                return;
            }

            listeners.Add(listener);
        }

        public static void Unsubscribe<T>(Action<T> listener)
        {
            if (listener == null)
            {
                throw new ArgumentNullException(nameof(listener));
            }

            var eventType = typeof(T);

            if (!_listenersByType.TryGetValue(eventType, out var listeners))
            {
                return;
            }

            listeners.Remove(listener);

            if (listeners.Count == 0)
            {
                _listenersByType.Remove(eventType);
            }
        }

        public static void Publish<T>(T eventData)
        {
            if (_isDispatching)
            {
                Enqueue(eventData);
                return;
            }

            Dispatch(eventData);
            DrainQueue();
        }

        public static void Reset()
        {
            _listenersByType.Clear();
            _dispatchBuffersByType.Clear();

            foreach (var queuedEventStore in _queuedEventsByType.Values)
            {
                queuedEventStore.Clear();
            }

            _queuedEventsByType.Clear();
            _queuedDispatches.Clear();
            _isDispatching = false;
        }

        private static void Dispatch<T>(T eventData)
        {
            var eventType = typeof(T);

            if (!_listenersByType.TryGetValue(eventType, out var listeners) || listeners.Count == 0)
            {
                return;
            }

            if (!_dispatchBuffersByType.TryGetValue(eventType, out var dispatchBuffer))
            {
                dispatchBuffer = new List<Delegate>(listeners.Count);
                _dispatchBuffersByType.Add(eventType, dispatchBuffer);
            }

            dispatchBuffer.Clear();

            if (dispatchBuffer.Capacity < listeners.Count)
            {
                dispatchBuffer.Capacity = listeners.Count;
            }

            for (var i = 0; i < listeners.Count; i++)
            {
                dispatchBuffer.Add(listeners[i]);
            }

            _isDispatching = true;

            try
            {
                for (var i = 0; i < dispatchBuffer.Count; i++)
                {
                    if (dispatchBuffer[i] is Action<T> listener)
                    {
                        try
                        {
                            listener(eventData);
                        }
                        catch (Exception exception)
                        {
                            Debug.LogException(exception);
                        }
                    }
                }
            }
            finally
            {
                _isDispatching = false;
            }
        }

        private static void DrainQueue()
        {
            while (_queuedDispatches.Count > 0)
            {
                var queuedEventToken = _queuedDispatches.Dequeue();

                if (_queuedEventsByType.TryGetValue(queuedEventToken.EventType, out var queuedEventStore))
                {
                    queuedEventStore.DispatchNext();
                }
            }

            foreach (var queuedEventStore in _queuedEventsByType.Values)
            {
                queuedEventStore.Clear();
            }
        }

        private static void Enqueue<T>(T eventData)
        {
            var eventType = typeof(T);

            if (!_queuedEventsByType.TryGetValue(eventType, out var queuedEventStore))
            {
                queuedEventStore = new QueuedEventStore<T>();
                _queuedEventsByType.Add(eventType, queuedEventStore);
            }

            ((QueuedEventStore<T>)queuedEventStore).Enqueue(eventData);
            _queuedDispatches.Enqueue(new QueuedEventToken(eventType));
        }

        private readonly struct QueuedEventToken
        {
            public QueuedEventToken(Type eventType)
            {
                EventType = eventType;
            }

            public Type EventType { get; }
        }

        private interface IQueuedEventStore
        {
            void DispatchNext();
            void Clear();
        }

        private sealed class QueuedEventStore<T> : IQueuedEventStore
        {
            private readonly Queue<T> _queuedEvents = new();

            public void Enqueue(T eventData)
            {
                _queuedEvents.Enqueue(eventData);
            }

            public void DispatchNext()
            {
                if (_queuedEvents.Count == 0)
                {
                    return;
                }

                var eventData = _queuedEvents.Dequeue();
                Dispatch(eventData);
            }

            public void Clear()
            {
                _queuedEvents.Clear();
            }
        }
    }

    public abstract class EventChannelSO<T> : ScriptableObject
    {
        public void Subscribe(Action<T> listener)
        {
            EventBus.Subscribe(listener);
        }

        public void Unsubscribe(Action<T> listener)
        {
            EventBus.Unsubscribe(listener);
        }

        public void Publish(T eventData)
        {
            EventBus.Publish(eventData);
        }
    }
}
