using UnityEngine;

namespace KokTengri.Core
{
    [CreateAssetMenu(fileName = "XPConfig", menuName = "KokTengri/Data/XP Config")]
    public sealed class XPConfigSO : ScriptableObject
    {
        [field: SerializeField, Min(1f)] public float BaseXpForLevel2 { get; private set; } = 10f;

        [field: SerializeField, Min(1f)] public float XpGrowthFactor { get; private set; } = 1.3f;

        [field: SerializeField, Min(10)] public int MaxLevel { get; private set; } = 100;

        [field: SerializeField, Min(1f)] public float EliteXpMultiplier { get; private set; } = 3.0f;

        [field: SerializeField, Min(0.5f)] public float XpGemMagnetRadius { get; private set; } = 3.0f;

        [field: SerializeField, Min(1f)] public float XpGemMoveSpeed { get; private set; } = 8.0f;

        public float GetXpForLevel(int level)
        {
            if (level <= 1)
            {
                return 0f;
            }

            return BaseXpForLevel2 * Mathf.Pow(XpGrowthFactor, level - 2);
        }
    }
}
