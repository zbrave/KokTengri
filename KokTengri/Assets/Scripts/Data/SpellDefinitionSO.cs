using UnityEngine;

namespace KokTengri.Core
{
    [CreateAssetMenu(fileName = "SpellDefinition", menuName = "KokTengri/Data/Spell Definition")]
    public sealed class SpellDefinitionSO : ScriptableObject
    {
        /// <summary>
        /// Gets the unique runtime identifier for the spell.
        /// </summary>
        [field: SerializeField]
        public string SpellId { get; private set; } = string.Empty;

        /// <summary>
        /// Gets the player-facing spell name.
        /// </summary>
        [field: SerializeField]
        public string DisplayName { get; private set; } = string.Empty;

        /// <summary>
        /// Gets the spell activation pattern.
        /// </summary>
        [field: SerializeField]
        public SpellKind Kind { get; private set; } = SpellKind.AoE;

        /// <summary>
        /// Gets the level 1 base damage.
        /// </summary>
        [field: SerializeField, Min(1f)]
        public float BaseDamage { get; private set; } = 10f;

        /// <summary>
        /// Gets the highest level this spell can reach.
        /// </summary>
        [field: SerializeField, Min(1)]
        public int MaxLevel { get; private set; } = 5;

        /// <summary>
        /// Gets the first recipe element.
        /// </summary>
        [field: SerializeField]
        public ElementType ElementA { get; private set; } = ElementType.Od;

        /// <summary>
        /// Gets the second recipe element.
        /// </summary>
        [field: SerializeField]
        public ElementType ElementB { get; private set; } = ElementType.Od;

        /// <summary>
        /// Gets the damage increase applied per extra spell level.
        /// </summary>
        [field: SerializeField, Min(0.01f)]
        public float DamageScalingPerLevel { get; private set; } = 0.25f;

        /// <summary>
        /// Gets the interval between activations.
        /// </summary>
        [field: SerializeField, Min(0.1f)]
        public float CooldownSeconds { get; private set; } = 1.0f;

        /// <summary>
        /// Gets the active duration for persistent spell effects.
        /// </summary>
        [field: SerializeField, Min(0f)]
        public float DurationSeconds { get; private set; } = 0f;

        /// <summary>
        /// Gets the effective targeting or influence range.
        /// </summary>
        [field: SerializeField, Min(0.1f)]
        public float Range { get; private set; } = 5.0f;

        /// <summary>
        /// Gets the area radius for AoE-style spells.
        /// </summary>
        [field: SerializeField, Min(0.1f)]
        public float AreaRadius { get; private set; } = 1.0f;

        /// <summary>
        /// Gets the number of projectiles spawned per activation.
        /// </summary>
        [field: SerializeField, Min(1)]
        public int ProjectileCount { get; private set; } = 1;

        /// <summary>
        /// Gets the projectile travel speed.
        /// </summary>
        [field: SerializeField, Min(0.1f)]
        public float Speed { get; private set; } = 5.0f;
    }
}
