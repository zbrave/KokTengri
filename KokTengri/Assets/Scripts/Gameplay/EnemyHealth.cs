using System;
using System.Collections.Generic;
using KokTengri.Core;
using UnityEngine;

namespace KokTengri.Gameplay
{
    [Serializable]
    public struct EnemyPoolReturnRequestedEvent
    {
        public EnemyPoolReturnRequestedEvent(int enemyId)
        {
            EnemyId = enemyId;
        }

        public int EnemyId;
    }

    [Serializable]
    public struct CorSplitRequestedEvent
    {
        public CorSplitRequestedEvent(int enemyId, Vector3 position, float splitOffsetDistance, float splitHpRatio, bool isElite, float runTime)
        {
            EnemyId = enemyId;
            Position = position;
            SplitOffsetDistance = splitOffsetDistance;
            SplitHpRatio = splitHpRatio;
            IsElite = isElite;
            RunTime = runTime;
        }

        public int EnemyId;
        public Vector3 Position;
        public float SplitOffsetDistance;
        public float SplitHpRatio;
        public bool IsElite;
        public float RunTime;
    }

    [Serializable]
    public struct EnemyDamageNumberRequestedEvent
    {
        public EnemyDamageNumberRequestedEvent(int enemyId, Vector3 position, int damageAmount, ElementType sourceElement, bool isLethal)
        {
            EnemyId = enemyId;
            Position = position;
            DamageAmount = damageAmount;
            SourceElement = sourceElement;
            IsLethal = isLethal;
        }

        public int EnemyId;
        public Vector3 Position;
        public int DamageAmount;
        public ElementType SourceElement;
        public bool IsLethal;
    }

    [DisallowMultipleComponent]
    public sealed class EnemyHealth : MonoBehaviour, IPooledObject
    {
        private const float EliteHpMultiplier = 3f;
        private const float TakingDamageStateDurationSeconds = 0.05f;
        private const int MinimumContactDamage = 1;

        [SerializeField] private EnemyDefinitionSO _definition;

        private readonly Dictionary<int, float> _lastContactDamageTimeByTargetId = new();

        private EnemyDefinitionSO _runtimeDefinition;
        private EnemyHealthState _state = EnemyHealthState.ReturnedToPool;
        private float _currentHp;
        private float _maxHp;
        private float _difficultyHpMultiplier = 1f;
        private float _difficultyDamageMultiplier = 1f;
        private float _runTime;
        private float _lastAcceptedHitTime = float.NegativeInfinity;
        private float _takingDamageStateUntilTime = float.NegativeInfinity;
        private bool _isElite;
        private bool _hasProcessedDeath;

        public float CurrentHp => _currentHp;

        public float MaxHp => _maxHp;

        public EnemyHealthState State => _state;

        public bool IsElite => _isElite;

        public EnemyType EnemyType => ActiveDefinition != null ? ActiveDefinition.Type : EnemyType.KaraKurt;

        public bool IsAlive => (_state == EnemyHealthState.Alive || _state == EnemyHealthState.TakingDamage) && _currentHp > 0f;

        public float HpPercent => _maxHp <= 0f ? 0f : _currentHp / _maxHp;

        public bool IsActive => gameObject.activeSelf;

        private EnemyDefinitionSO ActiveDefinition => _runtimeDefinition != null ? _runtimeDefinition : _definition;

        private void Update()
        {
            if (_state == EnemyHealthState.TakingDamage && Time.unscaledTime >= _takingDamageStateUntilTime && _currentHp > 0f)
            {
                _state = EnemyHealthState.Alive;
            }
        }

        public void Initialize(EnemyDefinitionSO definition, float difficultyHpMultiplier, float difficultyDamageMultiplier, bool isElite, float runTime)
        {
            _runtimeDefinition = definition != null ? definition : _definition;
            _difficultyHpMultiplier = difficultyHpMultiplier;
            _difficultyDamageMultiplier = difficultyDamageMultiplier;
            _isElite = isElite;
            _runTime = runTime;

            if (ActiveDefinition == null)
            {
                Debug.LogWarning("EnemyHealth requires an EnemyDefinitionSO during initialization.", this);
                _maxHp = 0f;
                _currentHp = 0f;
                _state = EnemyHealthState.Alive;
                return;
            }

            float eliteMultiplier = _isElite ? EliteHpMultiplier : 1f;
            _maxHp = ActiveDefinition.BaseHp * _difficultyHpMultiplier * eliteMultiplier;
            _currentHp = _maxHp;
            _state = EnemyHealthState.Alive;
            _hasProcessedDeath = false;
            _lastAcceptedHitTime = float.NegativeInfinity;
            _takingDamageStateUntilTime = float.NegativeInfinity;
            _lastContactDamageTimeByTargetId.Clear();
        }

        public void TakeDamage(float amount, ElementType sourceElement)
        {
            if (_state == EnemyHealthState.Dying || _state == EnemyHealthState.ReturnedToPool || ActiveDefinition == null)
            {
                return;
            }

            if (amount <= 0f)
            {
                return;
            }

            if (IsWithinHitInvincibilityWindow())
            {
                return;
            }

            _lastAcceptedHitTime = Time.unscaledTime;
            _currentHp = Mathf.Clamp(_currentHp - amount, 0f, _maxHp);

            bool isLethal = _currentHp <= 0f;
            EventBus.Publish(new EnemyDamageNumberRequestedEvent(GetInstanceID(), transform.position, Mathf.Max(1, Mathf.FloorToInt(amount)), sourceElement, isLethal));

            if (isLethal)
            {
                ExecuteDeathPipeline();
                return;
            }

            _state = EnemyHealthState.TakingDamage;
            _takingDamageStateUntilTime = Time.unscaledTime + TakingDamageStateDurationSeconds;
        }

        public void OnPoolCreate()
        {
            _runtimeDefinition = null;
            ResetTransientState(EnemyHealthState.ReturnedToPool);
        }

        public void OnPoolTake()
        {
            _runtimeDefinition = null;
            ResetTransientState(EnemyHealthState.Alive);
        }

        public void OnPoolReturn()
        {
            _runtimeDefinition = null;
            ResetTransientState(EnemyHealthState.ReturnedToPool);
        }

        public void OnPoolDestroy()
        {
            _runtimeDefinition = null;
            ResetTransientState(EnemyHealthState.ReturnedToPool);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            if (!IsAlive || ActiveDefinition == null)
            {
                return;
            }

            if (!TryResolvePlayerTargetId(other, out int targetId))
            {
                return;
            }

            float currentTime = Time.unscaledTime;
            if (_lastContactDamageTimeByTargetId.TryGetValue(targetId, out float lastContactTime)
                && currentTime - lastContactTime < ActiveDefinition.ContactIntervalSeconds)
            {
                return;
            }

            _lastContactDamageTimeByTargetId[targetId] = currentTime;

            int contactDamage = Mathf.Max(MinimumContactDamage, Mathf.FloorToInt(ActiveDefinition.BaseContactDamage * _difficultyDamageMultiplier));
            EventBus.Publish(new PlayerDamagedEvent(contactDamage, 0, 0, GetInstanceID(), _runTime));
        }

        private bool IsWithinHitInvincibilityWindow()
        {
            return Time.unscaledTime - _lastAcceptedHitTime < ActiveDefinition.HitInvincibilityDuration;
        }

        private void ExecuteDeathPipeline()
        {
            if (_hasProcessedDeath)
            {
                return;
            }

            _hasProcessedDeath = true;
            _currentHp = 0f;
            _state = EnemyHealthState.Dying;

            EventBus.Publish(new EnemyDeathEvent(GetInstanceID(), EnemyType, transform.position, _isElite, _runTime));

            if (EnemyType == EnemyType.Cor)
            {
                EventBus.Publish(new CorSplitRequestedEvent(
                    GetInstanceID(),
                    transform.position,
                    ActiveDefinition.CorSplitOffsetDistance,
                    ActiveDefinition.CorSplitHpRatio,
                    _isElite,
                    _runTime));
            }

            if (ActiveDefinition.IsBoss)
            {
                EventBus.Publish(new BossDefeatedEvent(ActiveDefinition.BossId, _runTime));
            }

            _state = EnemyHealthState.DeathCleanup;
            EventBus.Publish(new EnemyPoolReturnRequestedEvent(GetInstanceID()));
            _state = EnemyHealthState.ReturnedToPool;
            gameObject.SetActive(false);
        }

        private void ResetTransientState(EnemyHealthState nextState)
        {
            _state = nextState;
            _currentHp = 0f;
            _maxHp = 0f;
            _difficultyHpMultiplier = 1f;
            _difficultyDamageMultiplier = 1f;
            _runTime = 0f;
            _lastAcceptedHitTime = float.NegativeInfinity;
            _takingDamageStateUntilTime = float.NegativeInfinity;
            _isElite = false;
            _hasProcessedDeath = false;
            _lastContactDamageTimeByTargetId.Clear();
        }

        private static bool TryResolvePlayerTargetId(Collider2D other, out int targetId)
        {
            targetId = 0;

            if (other == null)
            {
                return false;
            }

            if (!other.TryGetComponent<PlayerMovement>(out var movement) && !other.GetComponentInParent<PlayerMovement>())
            {
                return false;
            }

            var targetTransform = other.attachedRigidbody != null ? other.attachedRigidbody.transform : other.transform;
            targetId = targetTransform.GetInstanceID();
            return true;
        }
    }
}
