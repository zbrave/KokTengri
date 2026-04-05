using UnityEngine;

namespace KokTengri.Core
{
    [CreateAssetMenu(fileName = "DifficultyConfig", menuName = "KokTengri/Data/Difficulty Config")]
    public sealed class DifficultyConfigSO : ScriptableObject
    {
        [field: SerializeField, Min(0.01f)] public float HpSlopePerMinute { get; private set; } = 0.12f;

        [field: SerializeField, Min(0.01f)] public float DamageSlopePerMinute { get; private set; } = 0.08f;

        [field: SerializeField, Min(0.01f)] public float SpawnSlopePerMinute { get; private set; } = 0.10f;

        [field: SerializeField, Min(0f)] public float EliteStartMinute { get; private set; } = 10f;

        [field: SerializeField, Min(1f)] public float EliteHpMultiplier { get; private set; } = 3.0f;

        [field: SerializeField, Min(1f)] public float EliteXpMultiplier { get; private set; } = 3.0f;

        [field: SerializeField, Min(1f)] public float HeroModeHpMultiplier { get; private set; } = 1.5f;

        [field: SerializeField, Min(1f)] public float HeroModeSpawnMultiplier { get; private set; } = 1.5f;

        [field: SerializeField] public EnemyUnlockEntry[] EnemyUnlockSchedule { get; private set; } =
        {
            new EnemyUnlockEntry(EnemyType.KaraKurt, 0f),
            new EnemyUnlockEntry(EnemyType.YekUsagi, 2f),
            new EnemyUnlockEntry(EnemyType.Albasti, 5f),
            new EnemyUnlockEntry(EnemyType.Cor, 8f),
            new EnemyUnlockEntry(EnemyType.DemirciCin, 12f),
            new EnemyUnlockEntry(EnemyType.GolAynasi, 18f),
        };
    }
}
