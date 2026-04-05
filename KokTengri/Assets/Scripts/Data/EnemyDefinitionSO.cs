using UnityEngine;

namespace KokTengri.Core
{
    [CreateAssetMenu(fileName = "EnemyDefinition", menuName = "KokTengri/Data/Enemy Definition")]
    public sealed class EnemyDefinitionSO : ScriptableObject
    {
        [field: SerializeField]
        public string EnemyId { get; private set; } = string.Empty;

        [field: SerializeField]
        public EnemyType Type { get; private set; } = EnemyType.KaraKurt;

        [field: SerializeField, Min(1f)]
        public float BaseHp { get; private set; } = 10f;

        [field: SerializeField, Min(1f)]
        public float BaseContactDamage { get; private set; } = 5f;

        [field: SerializeField, Min(0.1f)]
        public float ContactIntervalSeconds { get; private set; } = 0.5f;

        [field: SerializeField, Min(0f)]
        public float HitInvincibilityDuration { get; private set; } = 0.1f;

        [field: SerializeField, Min(0)]
        public int XpReward { get; private set; } = 10;

        [field: SerializeField]
        public bool CanHaveEliteVariant { get; private set; } = true;

        [field: SerializeField]
        public bool IsBoss { get; private set; }

        [field: SerializeField]
        public string BossId { get; private set; } = string.Empty;

        [field: SerializeField]
        public bool IsClone { get; private set; }

        [field: SerializeField, Min(0.1f)]
        public float CorSplitOffsetDistance { get; private set; } = 0.75f;

        [field: SerializeField, Range(0.1f, 1f)]
        public float CorSplitHpRatio { get; private set; } = 0.5f;
    }
}
