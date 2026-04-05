using KokTengri.Core;
using UnityEngine;

namespace KokTengri.Gameplay
{
    /// <summary>
    /// Context payload used to activate a spell effect from gameplay systems.
    /// </summary>
    public struct SpellActivationContext
    {
        public SpellActivationContext(
            string spellId,
            int spellLevel,
            Vector2 playerPosition,
            Vector2 playerFacingDirection,
            Vector2? lastMovementDirection)
        {
            SpellId = spellId;
            SpellLevel = spellLevel;
            PlayerPosition = playerPosition;
            PlayerFacingDirection = playerFacingDirection;
            LastMovementDirection = lastMovementDirection;
        }

        public string SpellId;
        public int SpellLevel;
        public Vector2 PlayerPosition;
        public Vector2 PlayerFacingDirection;
        public Vector2? LastMovementDirection;
    }

    /// <summary>
    /// Activation result returned by spell effects after an activate request.
    /// </summary>
    public enum SpellActivationResult
    {
        Succeeded = 0,
        Failed = 1,
    }

    /// <summary>
    /// Base behaviour shared by all runtime spell effects.
    /// </summary>
    public abstract class SpellEffectBase : MonoBehaviour
    {
        private HeroClass _currentHeroClass = HeroClass.None;

        protected HeroClass CurrentHeroClass => _currentHeroClass;

        private void OnEnable()
        {
            EventBus.Subscribe<RunStartEvent>(HandleRunStart);
            EventBus.Subscribe<RunEndEvent>(HandleRunEndEvent);
            OnEffectEnabled();
        }

        private void OnDisable()
        {
            OnEffectDisabled();
            EventBus.Unsubscribe<RunStartEvent>(HandleRunStart);
            EventBus.Unsubscribe<RunEndEvent>(HandleRunEndEvent);
        }

        /// <summary>
        /// Activates the effect for the supplied spell context.
        /// </summary>
        public abstract SpellActivationResult Activate(SpellActivationContext context);

        /// <summary>
        /// Deactivates the effect and returns all owned entities to their pools.
        /// </summary>
        public abstract void Deactivate();

        /// <summary>
        /// Performs deterministic cleanup when the run ends.
        /// </summary>
        public abstract void OnRunEnd();

        protected virtual void OnEffectEnabled()
        {
        }

        protected virtual void OnEffectDisabled()
        {
        }

        /// <summary>
        /// Requests spell damage using the shared damage calculator rules.
        /// </summary>
        protected int RequestDamage(
            float baseDamage,
            int spellLevel,
            ElementType elementA,
            ElementType elementB,
            EnemyType targetType,
            HeroClass heroClass)
        {
            return DamageCalculator.CalculateSpellDamage(baseDamage, spellLevel, elementA, elementB, targetType, heroClass);
        }

        private void HandleRunStart(RunStartEvent eventData)
        {
            _currentHeroClass = ResolveHeroClass(eventData.ClassId);
        }

        private void HandleRunEndEvent(RunEndEvent eventData)
        {
            OnRunEnd();
        }

        private static HeroClass ResolveHeroClass(string classId)
        {
            if (string.IsNullOrWhiteSpace(classId))
            {
                return HeroClass.None;
            }

            return System.Enum.TryParse(classId, true, out HeroClass heroClass)
                ? heroClass
                : HeroClass.None;
        }
    }
}
