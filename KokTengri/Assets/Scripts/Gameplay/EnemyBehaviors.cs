using System;
using KokTengri.Core;
using UnityEngine;

namespace KokTengri.Gameplay
{
    [Serializable]
    internal struct EnemyBehaviorTuning
    {
        [SerializeField] public EnemyType Type;
        [SerializeField, Min(0f)] public float MoveSpeed;
        [SerializeField, Min(0f)] public float PreferredRange;
        [SerializeField, Min(0f)] public float RangeBuffer;
        [SerializeField, Range(0f, 1f)] public float KnockbackResistance;
    }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(EnemyHealth))]
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class EnemyBehaviors : MonoBehaviour
    {
        [SerializeField] private EnemyHealth _enemyHealth = null;
        [SerializeField] private Rigidbody2D _rigidbody = null;
        [SerializeField] private EnemyBehaviorTuning[] _behaviorTuning =
        {
            new EnemyBehaviorTuning { Type = EnemyType.KaraKurt, MoveSpeed = 3f, PreferredRange = 0f, RangeBuffer = 0f, KnockbackResistance = 0f },
            new EnemyBehaviorTuning { Type = EnemyType.YekUsagi, MoveSpeed = 1.5f, PreferredRange = 0f, RangeBuffer = 0f, KnockbackResistance = 0.25f },
            new EnemyBehaviorTuning { Type = EnemyType.Albasti, MoveSpeed = 2.5f, PreferredRange = 4f, RangeBuffer = 0.5f, KnockbackResistance = 0f },
            new EnemyBehaviorTuning { Type = EnemyType.Cor, MoveSpeed = 2f, PreferredRange = 0f, RangeBuffer = 0f, KnockbackResistance = 0f },
            new EnemyBehaviorTuning { Type = EnemyType.DemirciCin, MoveSpeed = 1.8f, PreferredRange = 0f, RangeBuffer = 0f, KnockbackResistance = 0f },
            new EnemyBehaviorTuning { Type = EnemyType.GolAynasi, MoveSpeed = 2.8f, PreferredRange = 0f, RangeBuffer = 0f, KnockbackResistance = 0f },
        };

        private Vector2 _lastKnownPlayerPosition;

        /// <summary>
        /// Gets the authored knockback resistance for the active enemy type.
        /// </summary>
        public float KnockbackResistance => GetBehaviorTuning().KnockbackResistance;

        private void Reset()
        {
            _enemyHealth = GetComponent<EnemyHealth>();
            _rigidbody = GetComponent<Rigidbody2D>();
            EnsureDefaultBehaviorTuning();
        }

        private void Awake()
        {
            if (_enemyHealth == null)
            {
                _enemyHealth = GetComponent<EnemyHealth>();
            }

            if (_rigidbody == null)
            {
                _rigidbody = GetComponent<Rigidbody2D>();
            }

            EnsureDefaultBehaviorTuning();
            ResetPlayerTracking();
        }

        private void OnEnable()
        {
            ResetPlayerTracking();
            EventBus.Subscribe<PlayerPositionEvent>(HandlePlayerPositionEvent);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<PlayerPositionEvent>(HandlePlayerPositionEvent);
            ResetPlayerTracking();

            if (_rigidbody != null)
            {
                _rigidbody.velocity = Vector2.zero;
            }
        }

        private void OnValidate()
        {
            if (_enemyHealth == null)
            {
                _enemyHealth = GetComponent<EnemyHealth>();
            }

            if (_rigidbody == null)
            {
                _rigidbody = GetComponent<Rigidbody2D>();
            }

            EnsureDefaultBehaviorTuning();
        }

        private void FixedUpdate()
        {
            if (_rigidbody == null || _enemyHealth == null || !_enemyHealth.IsAlive)
            {
                return;
            }

            var currentPosition = _rigidbody.position;
            var targetPosition = _lastKnownPlayerPosition;
            var behaviorTuning = GetBehaviorTuning();
            var movementDirection = ResolveMovementDirection(_enemyHealth.EnemyType, currentPosition, targetPosition, behaviorTuning);
            var nextPosition = currentPosition + (movementDirection * behaviorTuning.MoveSpeed * Time.fixedDeltaTime);

            _rigidbody.MovePosition(nextPosition);
            FaceTarget(currentPosition, targetPosition);
        }

        private void HandlePlayerPositionEvent(PlayerPositionEvent eventData)
        {
            _lastKnownPlayerPosition = eventData.Position;
        }

        private Vector2 ResolveMovementDirection(EnemyType enemyType, Vector2 currentPosition, Vector2 targetPosition, EnemyBehaviorTuning behaviorTuning)
        {
            switch (enemyType)
            {
                case EnemyType.Albasti:
                    return ResolveAlbastiDirection(currentPosition, targetPosition, behaviorTuning);
                case EnemyType.KaraKurt:
                case EnemyType.YekUsagi:
                case EnemyType.Cor:
                case EnemyType.DemirciCin:
                case EnemyType.GolAynasi:
                default:
                    return ResolveChaseDirection(currentPosition, targetPosition);
            }
        }

        private static Vector2 ResolveChaseDirection(Vector2 currentPosition, Vector2 targetPosition)
        {
            var toTarget = targetPosition - currentPosition;
            return toTarget.sqrMagnitude <= Mathf.Epsilon ? Vector2.zero : toTarget.normalized;
        }

        private static Vector2 ResolveAlbastiDirection(Vector2 currentPosition, Vector2 targetPosition, EnemyBehaviorTuning behaviorTuning)
        {
            var toTarget = targetPosition - currentPosition;
            var distanceToTarget = toTarget.magnitude;

            if (distanceToTarget <= Mathf.Epsilon)
            {
                return Vector2.zero;
            }

            if (distanceToTarget < behaviorTuning.PreferredRange)
            {
                return -toTarget / distanceToTarget;
            }

            if (distanceToTarget > behaviorTuning.PreferredRange + behaviorTuning.RangeBuffer)
            {
                return toTarget / distanceToTarget;
            }

            return Vector2.zero;
        }

        private void FaceTarget(Vector2 currentPosition, Vector2 targetPosition)
        {
            var facingDirection = targetPosition - currentPosition;
            if (facingDirection.sqrMagnitude <= Mathf.Epsilon)
            {
                return;
            }

            transform.up = facingDirection.normalized;
        }

        private EnemyBehaviorTuning GetBehaviorTuning()
        {
            var activeEnemyType = _enemyHealth != null ? _enemyHealth.EnemyType : EnemyType.KaraKurt;

            if (_behaviorTuning != null)
            {
                for (var index = 0; index < _behaviorTuning.Length; index++)
                {
                    if (_behaviorTuning[index].Type == activeEnemyType)
                    {
                        return _behaviorTuning[index];
                    }
                }
            }

            return CreateFallbackTuning(activeEnemyType);
        }

        private void EnsureDefaultBehaviorTuning()
        {
            if (_behaviorTuning == null || _behaviorTuning.Length == 0 || !ContainsEnemyType(EnemyType.KaraKurt) || !ContainsEnemyType(EnemyType.YekUsagi) || !ContainsEnemyType(EnemyType.Albasti) || !ContainsEnemyType(EnemyType.Cor) || !ContainsEnemyType(EnemyType.DemirciCin) || !ContainsEnemyType(EnemyType.GolAynasi))
            {
                _behaviorTuning = CreateDefaultBehaviorTuning();
            }
        }

        private bool ContainsEnemyType(EnemyType enemyType)
        {
            if (_behaviorTuning == null)
            {
                return false;
            }

            for (var index = 0; index < _behaviorTuning.Length; index++)
            {
                if (_behaviorTuning[index].Type == enemyType)
                {
                    return true;
                }
            }

            return false;
        }

        private void ResetPlayerTracking()
        {
            _lastKnownPlayerPosition = transform.position;
        }

        private static EnemyBehaviorTuning[] CreateDefaultBehaviorTuning()
        {
            return new[]
            {
                CreateTuning(EnemyType.KaraKurt, 3f, 0f, 0f, 0f),
                CreateTuning(EnemyType.YekUsagi, 1.5f, 0f, 0f, 0.25f),
                CreateTuning(EnemyType.Albasti, 2.5f, 4f, 0.5f, 0f),
                CreateTuning(EnemyType.Cor, 2f, 0f, 0f, 0f),
                CreateTuning(EnemyType.DemirciCin, 1.8f, 0f, 0f, 0f),
                CreateTuning(EnemyType.GolAynasi, 2.8f, 0f, 0f, 0f),
            };
        }

        private static EnemyBehaviorTuning CreateFallbackTuning(EnemyType enemyType)
        {
            switch (enemyType)
            {
                case EnemyType.YekUsagi:
                    return CreateTuning(enemyType, 1.5f, 0f, 0f, 0.25f);
                case EnemyType.Albasti:
                    return CreateTuning(enemyType, 2.5f, 4f, 0.5f, 0f);
                case EnemyType.Cor:
                    return CreateTuning(enemyType, 2f, 0f, 0f, 0f);
                case EnemyType.DemirciCin:
                    return CreateTuning(enemyType, 1.8f, 0f, 0f, 0f);
                case EnemyType.GolAynasi:
                    return CreateTuning(enemyType, 2.8f, 0f, 0f, 0f);
                case EnemyType.KaraKurt:
                default:
                    return CreateTuning(enemyType, 3f, 0f, 0f, 0f);
            }
        }

        private static EnemyBehaviorTuning CreateTuning(EnemyType type, float moveSpeed, float preferredRange, float rangeBuffer, float knockbackResistance)
        {
            return new EnemyBehaviorTuning
            {
                Type = type,
                MoveSpeed = moveSpeed,
                PreferredRange = preferredRange,
                RangeBuffer = rangeBuffer,
                KnockbackResistance = knockbackResistance,
            };
        }
    }
}
