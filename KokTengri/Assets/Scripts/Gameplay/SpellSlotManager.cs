using System;
using System.Collections.Generic;
using KokTengri.Core;

namespace KokTengri.Gameplay
{
    /// <summary>
    /// Runtime view of an occupied spell slot.
    /// </summary>
    [System.Serializable]
    public struct SpellSlotEntry
    {
        public string SpellId;
        public int Level;
        public SpellKind Kind;

        public SpellSlotEntry(string spellId, int level, SpellKind kind = default)
        {
            SpellId = spellId;
            Level = level;
            Kind = kind;
        }
    }

    /// <summary>
    /// Fixed-size runtime container for crafted spells and their upgrade levels.
    /// </summary>
    public sealed class SpellSlotManager
    {
        private readonly SpellSlotEntry?[] _slots;

        public SpellSlotManager(int maxSlots = 6, int maxLevel = 5)
        {
            if (maxSlots < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(maxSlots), maxSlots, "Max slots must be at least 1.");
            }

            if (maxLevel < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(maxLevel), maxLevel, "Max level must be at least 1.");
            }

            MaxSlots = maxSlots;
            MaxLevel = maxLevel;
            _slots = new SpellSlotEntry?[maxSlots];
        }

        /// <summary>
        /// True if at least one spell slot is empty.
        /// </summary>
        public bool HasFreeSlot => SpellCount < MaxSlots;

        /// <summary>
        /// Number of occupied spell slots.
        /// </summary>
        public int SpellCount
        {
            get
            {
                var count = 0;

                for (var i = 0; i < _slots.Length; i++)
                {
                    if (_slots[i].HasValue)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        /// <summary>
        /// Maximum number of spells that can be owned at once.
        /// </summary>
        public int MaxSlots { get; }

        /// <summary>
        /// Maximum level an owned spell can reach.
        /// </summary>
        public int MaxLevel { get; }

        /// <summary>
        /// Create a level 1 spell in the first free slot.
        /// </summary>
        public bool TryCreateSpell(string spellId, SpellKind kind)
        {
            if (string.IsNullOrWhiteSpace(spellId) || !HasFreeSlot || IsSpellOwned(spellId))
            {
                return false;
            }

            for (var i = 0; i < _slots.Length; i++)
            {
                if (_slots[i].HasValue)
                {
                    continue;
                }

                _slots[i] = new SpellSlotEntry(spellId, 1, kind);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Increase the level of an owned spell by exactly one.
        /// </summary>
        public bool TryUpgradeSpell(string spellId)
        {
            if (string.IsNullOrWhiteSpace(spellId))
            {
                return false;
            }

            for (var i = 0; i < _slots.Length; i++)
            {
                if (!_slots[i].HasValue)
                {
                    continue;
                }

                SpellSlotEntry entry = _slots[i].Value;

                if (!string.Equals(entry.SpellId, spellId, StringComparison.Ordinal) || entry.Level >= MaxLevel)
                {
                    continue;
                }

                entry.Level++;
                _slots[i] = entry;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Get the current level for a spell, or 0 when not owned.
        /// </summary>
        public int GetSpellLevel(string spellId)
        {
            if (string.IsNullOrWhiteSpace(spellId))
            {
                return 0;
            }

            for (var i = 0; i < _slots.Length; i++)
            {
                if (!_slots[i].HasValue)
                {
                    continue;
                }

                SpellSlotEntry entry = _slots[i].Value;

                if (string.Equals(entry.SpellId, spellId, StringComparison.Ordinal))
                {
                    return entry.Level;
                }
            }

            return 0;
        }

        /// <summary>
        /// True when a spell exists in any occupied slot.
        /// </summary>
        public bool IsSpellOwned(string spellId)
        {
            return GetSpellLevel(spellId) > 0;
        }

        /// <summary>
        /// Get the slot entry at a specific index, or null when empty/invalid.
        /// </summary>
        public SpellSlotEntry? GetSpellAt(int index)
        {
            if (index < 0 || index >= MaxSlots)
            {
                return null;
            }

            return _slots[index];
        }

        /// <summary>
        /// Returns an ordered snapshot of owned spells by slot index.
        /// </summary>
        public IReadOnlyList<SpellSlotEntry> GetAllSpells()
        {
            var snapshot = new List<SpellSlotEntry>(SpellCount);

            for (var i = 0; i < _slots.Length; i++)
            {
                if (_slots[i].HasValue)
                {
                    snapshot.Add(_slots[i].Value);
                }
            }

            return snapshot;
        }

        /// <summary>
        /// Clear all spell slots.
        /// </summary>
        public void Clear()
        {
            for (var i = 0; i < _slots.Length; i++)
            {
                _slots[i] = null;
            }
        }
    }
}
