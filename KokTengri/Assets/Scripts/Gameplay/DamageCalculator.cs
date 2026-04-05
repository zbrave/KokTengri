using KokTengri.Core;
using UnityEngine;

namespace KokTengri.Gameplay
{
    public static class DamageCalculator
    {
        private const int MinSpellLevel = 1;
        private const int MaxSpellLevel = 5;
        private const float BaseClassBonus = 1.0f;
        private const float KamDualElementBonus = 1.15f;
        private const float ElementalClassBonus = 1.25f;
        private const float ResistantMultiplier = 0.6f;
        private const float NeutralMultiplier = 1.0f;
        private const float WeaknessMultiplier = 1.5f;

        private static readonly float[,] _affinityTable =
        {
            { WeaknessMultiplier, NeutralMultiplier, NeutralMultiplier, ResistantMultiplier, NeutralMultiplier, WeaknessMultiplier },
            { NeutralMultiplier, NeutralMultiplier, NeutralMultiplier, ResistantMultiplier, WeaknessMultiplier, ResistantMultiplier },
            { NeutralMultiplier, NeutralMultiplier, NeutralMultiplier, NeutralMultiplier, ResistantMultiplier, NeutralMultiplier },
            { ResistantMultiplier, WeaknessMultiplier, NeutralMultiplier, NeutralMultiplier, NeutralMultiplier, NeutralMultiplier },
            { NeutralMultiplier, ResistantMultiplier, WeaknessMultiplier, WeaknessMultiplier, NeutralMultiplier, NeutralMultiplier },
        };

        /// <summary>
        /// Calculate spell damage from all inputs.
        /// </summary>
        public static int CalculateSpellDamage(
            float baseDamage,
            int spellLevel,
            ElementType spellElement,
            EnemyType targetEnemyType,
            HeroClass heroClass,
            float metaPowerMultiplier = 1.0f)
        {
            float levelMultiplier = GetSpellLevelMultiplier(spellLevel);
            float elementMultiplier = GetElementMultiplier(spellElement, targetEnemyType);
            float classBonus = GetClassBonus(heroClass, spellElement);
            float rawDamage = baseDamage * levelMultiplier * elementMultiplier * classBonus * metaPowerMultiplier;

            return Mathf.Max(1, Mathf.FloorToInt(rawDamage));
        }

        /// <summary>
        /// Calculate spell damage when spell has two elements (combined spell).
        /// </summary>
        public static int CalculateSpellDamage(
            float baseDamage,
            int spellLevel,
            ElementType elementA,
            ElementType elementB,
            EnemyType targetEnemyType,
            HeroClass heroClass,
            float metaPowerMultiplier = 1.0f)
        {
            float levelMultiplier = GetSpellLevelMultiplier(spellLevel);
            float elementMultiplierA = GetElementMultiplier(elementA, targetEnemyType);
            float elementMultiplierB = GetElementMultiplier(elementB, targetEnemyType);
            float elementMultiplier = Mathf.Max(elementMultiplierA, elementMultiplierB);
            float classBonus = GetClassBonus(heroClass, elementA, elementB);
            float rawDamage = baseDamage * levelMultiplier * elementMultiplier * classBonus * metaPowerMultiplier;

            return Mathf.Max(1, Mathf.FloorToInt(rawDamage));
        }

        /// <summary>
        /// Calculate enemy contact damage to player.
        /// </summary>
        public static int CalculateEnemyContactDamage(
            float baseContactDamage,
            float elapsedMinutes)
        {
            float clampedElapsedMinutes = Mathf.Max(0.0f, elapsedMinutes);
            float timeMultiplier = 1.0f + (0.08f * clampedElapsedMinutes);
            float rawDamage = baseContactDamage * timeMultiplier;

            return Mathf.Max(1, Mathf.FloorToInt(rawDamage));
        }

        /// <summary>
        /// Look up element affinity multiplier for spell element vs enemy type.
        /// </summary>
        public static float GetElementMultiplier(ElementType spellElement, EnemyType enemyType)
        {
            int elementIndex = (int)spellElement;
            int enemyIndex = (int)enemyType;

            if (elementIndex < 0 || elementIndex >= _affinityTable.GetLength(0))
            {
                return NeutralMultiplier;
            }

            if (enemyIndex < 0 || enemyIndex >= _affinityTable.GetLength(1))
            {
                return NeutralMultiplier;
            }

            return _affinityTable[elementIndex, enemyIndex];
        }

        /// <summary>
        /// Resolve class bonus multiplier from class and spell element context.
        /// </summary>
        public static float GetClassBonus(HeroClass heroClass, ElementType elementA, ElementType elementB = ElementType.Od)
        {
            bool isDualElementSpell = elementA != elementB;
            float bonus = BaseClassBonus;

            if (heroClass == HeroClass.Kam && isDualElementSpell)
            {
                bonus = Mathf.Max(bonus, KamDualElementBonus);
            }

            if (heroClass == HeroClass.Batur && ContainsAny(elementA, elementB, ElementType.Sub, ElementType.Yer))
            {
                bonus = Mathf.Max(bonus, ElementalClassBonus);
            }

            if (heroClass == HeroClass.Mergen && ContainsAny(elementA, elementB, ElementType.Od, ElementType.Temur))
            {
                bonus = Mathf.Max(bonus, ElementalClassBonus);
            }

            if (heroClass == HeroClass.Otaci && ContainsAny(elementA, elementB, ElementType.Yel, ElementType.Od))
            {
                bonus = Mathf.Max(bonus, ElementalClassBonus);
            }

            return bonus;
        }

        /// <summary>
        /// Clamp spell level to valid range 1-5.
        /// </summary>
        public static int ClampSpellLevel(int level)
        {
            return Mathf.Clamp(level, MinSpellLevel, MaxSpellLevel);
        }

        private static bool ContainsAny(ElementType elementA, ElementType elementB, ElementType matchA, ElementType matchB)
        {
            return elementA == matchA || elementA == matchB || elementB == matchA || elementB == matchB;
        }

        private static float GetSpellLevelMultiplier(int spellLevel)
        {
            int clampedSpellLevel = ClampSpellLevel(spellLevel);
            return 1.0f + (0.25f * (clampedSpellLevel - MinSpellLevel));
        }
    }
}
