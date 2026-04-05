using UnityEngine;

namespace KokTengri.Core
{
    [CreateAssetMenu(fileName = "RunManagerConfig", menuName = "KokTengri/Data/Run Manager Config")]
    public sealed class RunManagerConfigSO : ScriptableObject
    {
        [field: SerializeField, Min(900)] public float RunDurationSeconds { get; private set; } = 1800f;

        [field: SerializeField, Min(300)] public float MinRunDurationSeconds { get; private set; } = 900f;

        [field: SerializeField, Min(1)] public float HeroModeEnemyHpMultiplier { get; private set; } = 1.5f;

        [field: SerializeField, Min(1)] public float HeroModeSpawnRateMultiplier { get; private set; } = 1.5f;

        [field: SerializeField, Min(1)] public float HeroModeGoldMultiplier { get; private set; } = 1.5f;

        [field: SerializeField, Min(1)] public float AfkThresholdSeconds { get; private set; } = 10f;

        [field: SerializeField, Min(0.05f)] public float PauseResumeDebounceSeconds { get; private set; } = 0.3f;

        [field: SerializeField, Min(0.01f)] public float TimeBroadcastIntervalSeconds { get; private set; } = 0.1f;

        [field: SerializeField] public bool VictoryAtTimerCapEnabled { get; private set; } = true;
    }
}
