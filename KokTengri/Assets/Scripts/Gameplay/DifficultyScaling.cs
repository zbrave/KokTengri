using System;
using System.Collections.Generic;
using KokTengri.Core;
using UnityEngine;

namespace KokTengri.Gameplay
{
    /// <summary>
    /// Calculates deterministic difficulty multipliers from elapsed run time and context flags.
    /// </summary>
    public static class DifficultyScaling
    {
        private const float DefaultHpSlopePerMinute = 0.12f;
        private const float DefaultDamageSlopePerMinute = 0.08f;
        private const float DefaultSpawnSlopePerMinute = 0.10f;
        private const float DefaultEliteStartMinute = 10f;
        private const float DefaultEliteHpMultiplier = 3.0f;
        private const float DefaultEliteXpMultiplier = 3.0f;
        private const float DefaultHeroModeHpMultiplier = 1.5f;
        private const float DefaultHeroModeSpawnMultiplier = 1.5f;
        private const float MinSlope = 0.01f;
        private const float MinMultiplier = 1.0f;
        private const string MissingConfigWarning = "DifficultyScaling received a null DifficultyConfigSO. Falling back to GDD default values.";

        /// <summary>
        /// Calculates the base HP multiplier from elapsed minutes.
        /// </summary>
        public static float GetHpMultiplier(float elapsedMinutes, DifficultyConfigSO config)
        {
            return 1.0f + (GetHpSlopePerMinute(config) * ClampElapsedMinutes(elapsedMinutes));
        }

        /// <summary>
        /// Calculates the base damage multiplier from elapsed minutes.
        /// </summary>
        public static float GetDamageMultiplier(float elapsedMinutes, DifficultyConfigSO config)
        {
            return 1.0f + (GetDamageSlopePerMinute(config) * ClampElapsedMinutes(elapsedMinutes));
        }

        /// <summary>
        /// Calculates the base spawn multiplier from elapsed minutes.
        /// </summary>
        public static float GetSpawnMultiplier(float elapsedMinutes, DifficultyConfigSO config)
        {
            return 1.0f + (GetSpawnSlopePerMinute(config) * ClampElapsedMinutes(elapsedMinutes));
        }

        /// <summary>
        /// Calculates final HP multiplier after applying Hero Mode and Elite modifiers multiplicatively.
        /// </summary>
        public static float GetFinalHpMultiplier(float elapsedMinutes, bool isHeroMode, bool isElite, DifficultyConfigSO config)
        {
            float multiplier = GetHpMultiplier(elapsedMinutes, config);

            if (isHeroMode)
            {
                multiplier *= GetHeroModeHpMultiplier(config);
            }

            if (isElite)
            {
                multiplier *= GetEliteHpMultiplier(config);
            }

            return multiplier;
        }

        /// <summary>
        /// Calculates final spawn multiplier after applying Hero Mode multiplicatively.
        /// </summary>
        public static float GetFinalSpawnMultiplier(float elapsedMinutes, bool isHeroMode, DifficultyConfigSO config)
        {
            float multiplier = GetSpawnMultiplier(elapsedMinutes, config);

            if (isHeroMode)
            {
                multiplier *= GetHeroModeSpawnMultiplier(config);
            }

            return multiplier;
        }

        /// <summary>
        /// Calculates the final XP multiplier for an enemy context.
        /// </summary>
        public static float GetFinalXpMultiplier(bool isElite, DifficultyConfigSO config)
        {
            return isElite ? GetEliteXpMultiplier(config) : 1.0f;
        }

        /// <summary>
        /// Checks whether a specific enemy type is unlocked at the provided elapsed time.
        /// </summary>
        public static bool IsEnemyUnlocked(EnemyType enemyType, float elapsedMinutes, DifficultyConfigSO config)
        {
            if (!Enum.IsDefined(typeof(EnemyType), enemyType))
            {
                return false;
            }

            return ClampElapsedMinutes(elapsedMinutes) >= GetUnlockTimeMinutes(enemyType, config);
        }

        /// <summary>
        /// Returns all currently unlocked enemy types in deterministic enum order.
        /// </summary>
        public static IReadOnlyList<EnemyType> GetUnlockedEnemies(float elapsedMinutes, DifficultyConfigSO config)
        {
            float clampedElapsedMinutes = ClampElapsedMinutes(elapsedMinutes);
            var unlockedEnemies = new List<EnemyType>();

            foreach (EnemyType enemyType in Enum.GetValues(typeof(EnemyType)))
            {
                if (clampedElapsedMinutes >= GetUnlockTimeMinutes(enemyType, config))
                {
                    unlockedEnemies.Add(enemyType);
                }
            }

            return unlockedEnemies;
        }

        private static float ClampElapsedMinutes(float elapsedMinutes)
        {
            return Mathf.Max(0.0f, elapsedMinutes);
        }

        private static float GetHpSlopePerMinute(DifficultyConfigSO config)
        {
            return ResolveValue(config, difficultyConfig => difficultyConfig.HpSlopePerMinute, DefaultHpSlopePerMinute, MinSlope);
        }

        private static float GetDamageSlopePerMinute(DifficultyConfigSO config)
        {
            return ResolveValue(config, difficultyConfig => difficultyConfig.DamageSlopePerMinute, DefaultDamageSlopePerMinute, MinSlope);
        }

        private static float GetSpawnSlopePerMinute(DifficultyConfigSO config)
        {
            return ResolveValue(config, difficultyConfig => difficultyConfig.SpawnSlopePerMinute, DefaultSpawnSlopePerMinute, MinSlope);
        }

        private static float GetEliteHpMultiplier(DifficultyConfigSO config)
        {
            return ResolveValue(config, difficultyConfig => difficultyConfig.EliteHpMultiplier, DefaultEliteHpMultiplier, MinMultiplier);
        }

        private static float GetEliteXpMultiplier(DifficultyConfigSO config)
        {
            return ResolveValue(config, difficultyConfig => difficultyConfig.EliteXpMultiplier, DefaultEliteXpMultiplier, MinMultiplier);
        }

        private static float GetHeroModeHpMultiplier(DifficultyConfigSO config)
        {
            return ResolveValue(config, difficultyConfig => difficultyConfig.HeroModeHpMultiplier, DefaultHeroModeHpMultiplier, MinMultiplier);
        }

        private static float GetHeroModeSpawnMultiplier(DifficultyConfigSO config)
        {
            return ResolveValue(config, difficultyConfig => difficultyConfig.HeroModeSpawnMultiplier, DefaultHeroModeSpawnMultiplier, MinMultiplier);
        }

        private static float GetEliteStartMinute(DifficultyConfigSO config)
        {
            return ResolveValue(config, difficultyConfig => difficultyConfig.EliteStartMinute, DefaultEliteStartMinute, 0f);
        }

        private static float GetUnlockTimeMinutes(EnemyType enemyType, DifficultyConfigSO config)
        {
            float fallbackUnlockTime = GetDefaultUnlockTimeMinutes(enemyType);
            var schedule = GetEnemyUnlockSchedule(config);

            if (schedule == null)
            {
                return fallbackUnlockTime;
            }

            for (int i = 0; i < schedule.Length; i++)
            {
                if (schedule[i].Type != enemyType)
                {
                    continue;
                }

                return Mathf.Max(0f, schedule[i].UnlockTimeMinutes);
            }

            return fallbackUnlockTime;
        }

        private static EnemyUnlockEntry[] GetEnemyUnlockSchedule(DifficultyConfigSO config)
        {
            if (config == null)
            {
                WarnMissingConfig();
                return null;
            }

            return config.EnemyUnlockSchedule;
        }

        private static float GetDefaultUnlockTimeMinutes(EnemyType enemyType)
        {
            switch (enemyType)
            {
                case EnemyType.KaraKurt:
                    return 0f;
                case EnemyType.YekUsagi:
                    return 2f;
                case EnemyType.Albasti:
                    return 5f;
                case EnemyType.Cor:
                    return 8f;
                case EnemyType.DemirciCin:
                    return 12f;
                case EnemyType.GolAynasi:
                    return 18f;
                default:
                    return float.PositiveInfinity;
            }
        }

        private static float ResolveValue(DifficultyConfigSO config, Func<DifficultyConfigSO, float> getter, float fallbackValue, float minimumValue)
        {
            if (config == null)
            {
                WarnMissingConfig();
                return fallbackValue;
            }

            return Mathf.Max(minimumValue, getter(config));
        }

        private static void WarnMissingConfig()
        {
            Debug.LogWarning(MissingConfigWarning);
        }
    }
}
