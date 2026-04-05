using System.Collections.Generic;
using KokTengri.Core;

namespace KokTengri.Gameplay
{
    /// <summary>
    /// Fixed-size runtime container for raw element picks used by spell crafting.
    /// </summary>
    public class ElementInventory
    {
        private readonly ElementType?[] _slots = new ElementType?[MaxSlots];

        private bool _hasPublishedFullEvent;

        /// <summary>
        /// Maximum number of element slots.
        /// </summary>
        public const int MaxSlots = 3;

        /// <summary>
        /// True if any slot is empty.
        /// </summary>
        public bool HasFreeSlot => FreeCount > 0;

        /// <summary>
        /// True if all 3 slots occupied.
        /// </summary>
        public bool IsFull => OccupiedCount == MaxSlots;

        /// <summary>
        /// Number of occupied slots (0-3).
        /// </summary>
        public int OccupiedCount
        {
            get
            {
                var occupiedCount = 0;

                for (var i = 0; i < MaxSlots; i++)
                {
                    if (_slots[i].HasValue)
                    {
                        occupiedCount++;
                    }
                }

                return occupiedCount;
            }
        }

        /// <summary>
        /// Number of free slots (0-3).
        /// </summary>
        public int FreeCount => MaxSlots - OccupiedCount;

        /// <summary>
        /// Add element to first free slot. Returns false if full.
        /// </summary>
        public bool TryAdd(ElementType element)
        {
            for (var i = 0; i < MaxSlots; i++)
            {
                if (_slots[i].HasValue)
                {
                    continue;
                }

                _slots[i] = element;
                EventBus.Publish(new ElementAddedEvent(element, i));

                if (!IsFull)
                {
                    _hasPublishedFullEvent = false;
                }

                return true;
            }

            if (!_hasPublishedFullEvent)
            {
                EventBus.Publish(new InventoryFullEvent(element));
                _hasPublishedFullEvent = true;
            }

            return false;
        }

        /// <summary>
        /// Remove element at slotIndex. Returns false if empty or invalid index.
        /// </summary>
        public bool TryConsumeAt(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= MaxSlots || !_slots[slotIndex].HasValue)
            {
                return false;
            }

            var removedElement = _slots[slotIndex].Value;
            _slots[slotIndex] = null;
            _hasPublishedFullEvent = false;

            EventBus.Publish(new ElementRemovedEvent(removedElement, slotIndex));
            return true;
        }

        /// <summary>
        /// Returns ordered snapshot of slots (deterministic by index 0→2).
        /// </summary>
        public IReadOnlyList<ElementType> GetSnapshot()
        {
            var snapshot = new List<ElementType>(OccupiedCount);

            for (var i = 0; i < MaxSlots; i++)
            {
                if (_slots[i].HasValue)
                {
                    snapshot.Add(_slots[i].Value);
                }
            }

            return snapshot;
        }

        /// <summary>
        /// Get element at specific slot (nullable).
        /// </summary>
        public ElementType? GetElementAt(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= MaxSlots)
            {
                return null;
            }

            return _slots[slotIndex];
        }

        /// <summary>
        /// Clear all slots (for run reset).
        /// </summary>
        public void Clear()
        {
            for (var i = 0; i < MaxSlots; i++)
            {
                _slots[i] = null;
            }

            _hasPublishedFullEvent = false;
        }

        /// <summary>
        /// Find first slot index containing the specified element type. Returns -1 if not found.
        /// </summary>
        public int FindSlot(ElementType element)
        {
            for (var i = 0; i < MaxSlots; i++)
            {
                if (_slots[i] == element)
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
