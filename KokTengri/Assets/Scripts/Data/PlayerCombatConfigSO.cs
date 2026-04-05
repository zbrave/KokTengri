using UnityEngine;

namespace KokTengri.Gameplay
{
    [CreateAssetMenu(fileName = "PlayerCombatConfig", menuName = "KokTengri/Data/Player Combat Config")]
    public sealed class PlayerCombatConfigSO : ScriptableObject
    {
        /// <summary>
        /// Gets the duration over which knockback displacement is applied.
        /// </summary>
        [field: SerializeField, Min(0.01f)]
        public float KnockbackDurationSeconds { get; private set; } = 0.15f;

        /// <summary>
        /// Gets the total knockback displacement magnitude applied away from the damage source.
        /// </summary>
        [field: SerializeField, Min(0.1f)]
        public float KnockbackForce { get; private set; } = 1.8f;

        /// <summary>
        /// Gets the invincibility window that follows knockback recovery.
        /// </summary>
        [field: SerializeField, Min(0.01f)]
        public float IFrameDurationSeconds { get; private set; } = 0.5f;

        /// <summary>
        /// Gets the movement speed multiplier applied during the recovery invincibility window.
        /// </summary>
        [field: SerializeField, Range(0.1f, 1.0f)]
        public float RecoverySpeedMultiplier { get; private set; } = 0.5f;
    }
}
