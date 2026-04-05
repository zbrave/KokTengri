using System;
using UnityEngine;

namespace KokTengri.Core
{
    [Serializable]
    public struct EnemyUnlockEntry
    {
        public EnemyUnlockEntry(EnemyType type, float unlockTimeMinutes)
        {
            Type = type;
            UnlockTimeMinutes = unlockTimeMinutes;
        }

        [field: SerializeField] public EnemyType Type { get; private set; }

        [field: SerializeField, Min(0f)] public float UnlockTimeMinutes { get; private set; }
    }

    [Serializable]
    public struct BossScheduleEntry
    {
        public BossScheduleEntry(string bossId, float triggerTimeMinutes)
        {
            BossId = bossId;
            TriggerTimeMinutes = triggerTimeMinutes;
        }

        [field: SerializeField] public string BossId { get; private set; }

        [field: SerializeField, Min(0f)] public float TriggerTimeMinutes { get; private set; }
    }

    [CreateAssetMenu(fileName = "WaveManagerConfig", menuName = "KokTengri/Data/Wave Manager Config")]
    public sealed class WaveManagerConfigSO : ScriptableObject
    {
        [field: SerializeField, Min(0.2f)] public float BaseSpawnRateEnemiesPerSecond { get; private set; } = 1f;

        [field: SerializeField, Min(0.01f)] public float SpawnRateIncreasePerMinute { get; private set; } = 0.10f;

        [field: SerializeField, Min(3)] public int BossIntervalMinutes { get; private set; } = 5;

        [field: SerializeField, Min(100)] public int MaxActiveEnemies { get; private set; } = 300;

        [field: SerializeField, Min(0f)] public float EliteStartMinute { get; private set; } = 10f;

        [field: SerializeField, Range(0f, 1f)] public float EliteSpawnProbability { get; private set; } = 0.05f;

        [field: SerializeField, Min(10f)] public float WaveSegmentDurationSeconds { get; private set; } = 60f;

        [field: SerializeField, Min(1f)] public float HeroModeSpawnMultiplier { get; private set; } = 1.5f;

        [field: SerializeField, Min(1f)] public float HeroModeEnemyHpMultiplier { get; private set; } = 1.5f;

        [field: SerializeField, Min(1f)] public float BossSpawnRetryWindowSeconds { get; private set; } = 3f;

        [field: SerializeField] public EnemyUnlockEntry[] EnemyUnlockSchedule { get; private set; } =
        {
            new(EnemyType.KaraKurt, 0f),
            new(EnemyType.YekUsagi, 2f),
            new(EnemyType.Albasti, 5f),
            new(EnemyType.Cor, 8f),
            new(EnemyType.DemirciCin, 12f),
            new(EnemyType.GolAynasi, 18f),
        };

        [field: SerializeField] public BossScheduleEntry[] BossSchedule { get; private set; } =
        {
            new("tepegoz", 5f),
            new("yer_tanrisi", 10f),
            new("erlik_hanin_elcisi", 15f),
            new("boz_ejderha", 20f),
            new("erlik_han", 25f),
        };
    }
}
