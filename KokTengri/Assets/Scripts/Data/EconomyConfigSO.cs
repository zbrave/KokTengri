using UnityEngine;

namespace KokTengri.Core
{
    [CreateAssetMenu(fileName = "EconomyConfig", menuName = "KokTengri/Data/Economy Config")]
    public sealed class EconomyConfigSO : ScriptableObject
    {
        [field: SerializeField] public float GoldPerSurvivedMinute { get; private set; } = 10f;

        [field: SerializeField] public float GoldPerKill { get; private set; } = 0.5f;

        [field: SerializeField] public float GoldPerBoss { get; private set; } = 100f;
    }
}
