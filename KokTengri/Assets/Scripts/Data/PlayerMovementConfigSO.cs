using UnityEngine;

namespace KokTengri.Gameplay
{
    [CreateAssetMenu(fileName = "PlayerMovementConfig", menuName = "KokTengri/Data/Player Movement Config")]
    public sealed class PlayerMovementConfigSO : ScriptableObject
    {
        /// <summary>
        /// Gets the base top-down movement speed in world units per second.
        /// </summary>
        [field: SerializeField, Min(0.1f)]
        public float BaseMoveSpeed { get; private set; } = 3.0f;

        /// <summary>
        /// Gets the normalized input magnitude required before movement becomes valid.
        /// </summary>
        [field: SerializeField, Range(0.01f, 0.5f)]
        public float DeadzoneNormalized { get; private set; } = 0.15f;

        /// <summary>
        /// Gets the movement multiplier applied while AFK auto-move is active.
        /// </summary>
        [field: SerializeField]
        public float AfkAutoMoveMultiplier { get; private set; } = 0.3f;

        /// <summary>
        /// Gets the fallback AFK direction used when no valid player input has been recorded yet.
        /// </summary>
        [field: SerializeField]
        public Vector2 AfkFallbackDirection { get; private set; } = new Vector2(1f, 0f);

        /// <summary>
        /// Gets the authoritative arena rectangle used for player position clamping.
        /// </summary>
        [field: SerializeField]
        public Rect ArenaBounds { get; private set; } = new Rect(-50f, -50f, 100f, 100f);
    }
}
