using KokTengri.Core;
using UnityEngine;

namespace KokTengri.Gameplay
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class PlayerMovement : MonoBehaviour
    {
        [SerializeField] private MonoBehaviour _inputProvider = null;
        [SerializeField] private Rigidbody2D _rigidbody = null;
        [SerializeField] private PlayerMovementConfigSO _config = null;
        [SerializeField] private PlayerCombatConfigSO _combatConfig = null;
        [SerializeField] private Rect _arenaBounds;

        private IInputProvider _resolvedInputProvider;

        private MovementState _currentState = MovementState.Idle;
        private Vector2 _currentInput;
        private Vector2 _lastValidDirection = Vector2.right;
        private Vector2 _movementVelocity;
        private Vector2 _knockbackDisplacement;

        private float _knockbackTimeRemaining;
        private float _invincibleTimeRemaining;

        private float _baseMultiplier = 1f;
        private float _spellBoost;
        private float _classPassive;
        private float _metaProgress;

        private bool _isFrozen;
        private bool _isPaused;
        private bool _isAfk;

        /// <summary>
        /// Gets the active locomotion state after priority resolution.
        /// </summary>
        public MovementState CurrentState => _currentState;

        /// <summary>
        /// Gets whether the player is currently within the invincibility recovery window.
        /// </summary>
        public bool IsInvincible => _invincibleTimeRemaining > 0f;

        private void Reset()
        {
            _rigidbody = GetComponent<Rigidbody2D>();
        }

        private void Awake()
        {
            if (_rigidbody == null)
            {
                _rigidbody = GetComponent<Rigidbody2D>();
            }

            _resolvedInputProvider = _inputProvider as IInputProvider;

            if (_resolvedInputProvider == null && _inputProvider != null)
            {
                Debug.LogError($"{nameof(PlayerMovement)} requires an input provider implementing {nameof(IInputProvider)}.", this);
            }

            if (_config != null)
            {
                var fallbackDirection = _config.AfkFallbackDirection;
                if (fallbackDirection.sqrMagnitude > 0f)
                {
                    _lastValidDirection = fallbackDirection.normalized;
                }
            }

            ConfigureRigidbody();
            ClampImmediatePosition();
            EvaluateMovementState();
        }

        private void OnEnable()
        {
            EventBus.Subscribe<PlayerDamagedEvent>(HandlePlayerDamaged);
            EventBus.Subscribe<RunPauseEvent>(HandleRunPause);

            if (_resolvedInputProvider != null)
            {
                _resolvedInputProvider.OnMove += HandleMoveInput;
                _currentInput = _resolvedInputProvider.MoveDirection;
                HandleMoveInput(_currentInput);
            }
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<PlayerDamagedEvent>(HandlePlayerDamaged);
            EventBus.Unsubscribe<RunPauseEvent>(HandleRunPause);

            if (_resolvedInputProvider != null)
            {
                _resolvedInputProvider.OnMove -= HandleMoveInput;
            }
        }

        private void OnValidate()
        {
            if (_rigidbody == null)
            {
                _rigidbody = GetComponent<Rigidbody2D>();
            }

            if (_rigidbody != null)
            {
                ConfigureRigidbody();
            }
        }

        private void Update()
        {
            if (_isPaused || _config == null || _combatConfig == null)
            {
                _movementVelocity = Vector2.zero;
                return;
            }

            if (_resolvedInputProvider != null)
            {
                _currentInput = _resolvedInputProvider.MoveDirection;
            }

            TickTimers(Time.unscaledDeltaTime);
            EvaluateMovementState();
            _movementVelocity = ComputeMovementVelocity();
        }

        private void FixedUpdate()
        {
            if (_isPaused || _rigidbody == null || _config == null || _combatConfig == null)
            {
                return;
            }

            var currentPosition = _rigidbody.position;
            var nextPosition = currentPosition;
            var fixedDeltaTime = Time.fixedUnscaledDeltaTime;

            if (_currentState == MovementState.Knockback)
            {
                var duration = Mathf.Max(_combatConfig.KnockbackDurationSeconds, Mathf.Epsilon);
                var knockbackVelocity = _knockbackDisplacement / duration;
                nextPosition += knockbackVelocity * fixedDeltaTime;
            }
            else if (_currentState != MovementState.Frozen)
            {
                nextPosition += _movementVelocity * fixedDeltaTime;
            }

            nextPosition = ClampToArena(nextPosition);
            _rigidbody.MovePosition(nextPosition);
        }

        private void LateUpdate()
        {
            if (_isPaused || _rigidbody == null || _config == null || _combatConfig == null)
            {
                return;
            }

            EventBus.Publish(new PlayerPositionEvent(_rigidbody.position, Time.unscaledTime));
        }

        /// <summary>
        /// Sets whether the player should be frozen by an external gameplay gate.
        /// </summary>
        /// <param name="frozen">True to lock movement and force the Frozen state; otherwise false.</param>
        public void SetFrozen(bool frozen)
        {
            if (_isFrozen == frozen)
            {
                return;
            }

            _isFrozen = frozen;

            if (_isFrozen)
            {
                _knockbackTimeRemaining = 0f;
                _invincibleTimeRemaining = 0f;
                _movementVelocity = Vector2.zero;
                _knockbackDisplacement = Vector2.zero;

                if (_rigidbody != null)
                {
                    _rigidbody.velocity = Vector2.zero;
                }
            }

            EvaluateMovementState();
        }

        /// <summary>
        /// Sets whether AFK auto-move should drive movement using the last valid direction.
        /// </summary>
        /// <param name="isAfk">True to enable AFK auto-move; otherwise false.</param>
        public void SetAfkState(bool isAfk)
        {
            _isAfk = isAfk;
            EvaluateMovementState();
        }

        /// <summary>
        /// Sets the baseline multiplicative speed scale applied before all additive bonuses.
        /// </summary>
        /// <param name="multiplier">The baseline multiplier where 1 means no change.</param>
        public void SetBaseSpeedMultiplier(float multiplier)
        {
            _baseMultiplier = Mathf.Max(0f, multiplier);
        }

        /// <summary>
        /// Sets the additive spell speed bonus used in the final multiplier composition.
        /// </summary>
        /// <param name="bonus">The spell bonus where 0 means no bonus and 0.2 means +20%.</param>
        public void SetSpellSpeedBoost(float bonus)
        {
            _spellBoost = bonus;
        }

        /// <summary>
        /// Sets the additive class passive speed bonus used in the final multiplier composition.
        /// </summary>
        /// <param name="bonus">The class passive bonus where 0 means no bonus and 0.2 means +20%.</param>
        public void SetClassPassiveSpeedBonus(float bonus)
        {
            _classPassive = bonus;
        }

        /// <summary>
        /// Sets the additive meta progression speed bonus used in the final multiplier composition.
        /// </summary>
        /// <param name="bonus">The meta progression bonus where 0 means no bonus and 0.2 means +20%.</param>
        public void SetMetaProgressSpeedBonus(float bonus)
        {
            _metaProgress = bonus;
        }

        private void HandleMoveInput(Vector2 moveDirection)
        {
            _currentInput = moveDirection;

            if (_config == null || !IsValidMovementInput(moveDirection))
            {
                return;
            }

            _lastValidDirection = moveDirection.normalized;

            if (_isAfk)
            {
                _isAfk = false;
            }
        }

        private void HandlePlayerDamaged(PlayerDamagedEvent eventData)
        {
            if (_isPaused || _isFrozen || _combatConfig == null || _invincibleTimeRemaining > 0f)
            {
                return;
            }

            var sourcePosition = ResolveDamageSourcePosition(eventData.SourceId);
            var playerPosition = _rigidbody != null ? _rigidbody.position : (Vector2)transform.position;
            var knockbackDirection = playerPosition - sourcePosition;

            if (knockbackDirection.sqrMagnitude <= Mathf.Epsilon)
            {
                knockbackDirection = GetFallbackDirection();
            }

            _knockbackDisplacement = knockbackDirection.normalized * _combatConfig.KnockbackForce;
            _knockbackTimeRemaining = _combatConfig.KnockbackDurationSeconds;
            _invincibleTimeRemaining = 0f;
            _movementVelocity = Vector2.zero;

            EvaluateMovementState();
        }

        private void HandleRunPause(RunPauseEvent eventData)
        {
            _isPaused = eventData.IsPaused;
            _movementVelocity = Vector2.zero;

            if (_rigidbody != null)
            {
                _rigidbody.velocity = Vector2.zero;
            }
        }

        private void TickTimers(float deltaTime)
        {
            if (_isFrozen)
            {
                return;
            }

            if (_knockbackTimeRemaining > 0f)
            {
                _knockbackTimeRemaining = Mathf.Max(0f, _knockbackTimeRemaining - deltaTime);

                if (_knockbackTimeRemaining <= 0f)
                {
                    _invincibleTimeRemaining = _combatConfig.IFrameDurationSeconds;
                }
            }
            else if (_invincibleTimeRemaining > 0f)
            {
                _invincibleTimeRemaining = Mathf.Max(0f, _invincibleTimeRemaining - deltaTime);
            }
        }

        private void EvaluateMovementState()
        {
            var nextState = ResolveNextState();

            if (nextState == _currentState)
            {
                return;
            }

            var previousState = _currentState;
            _currentState = nextState;
            EventBus.Publish(new PlayerMovementStateChangedEvent(previousState, nextState));
        }

        private MovementState ResolveNextState()
        {
            if (_config == null)
            {
                return MovementState.Idle;
            }

            if (_isFrozen)
            {
                return MovementState.Frozen;
            }

            if (_knockbackTimeRemaining > 0f)
            {
                return MovementState.Knockback;
            }

            if (_invincibleTimeRemaining > 0f)
            {
                return MovementState.Invincible;
            }

            if (_isAfk)
            {
                return MovementState.AFKAutoMove;
            }

            return IsValidMovementInput(_currentInput) ? MovementState.Moving : MovementState.Idle;
        }

        private Vector2 ComputeMovementVelocity()
        {
            if (_currentState == MovementState.Frozen || _currentState == MovementState.Knockback)
            {
                return Vector2.zero;
            }

            if (_currentState == MovementState.AFKAutoMove)
            {
                return GetAfkDirection() * (_config.BaseMoveSpeed * ComputeSpeedMultiplier());
            }

            var inputMagnitude = Mathf.Clamp01(_currentInput.magnitude);

            if (inputMagnitude < _config.DeadzoneNormalized)
            {
                return Vector2.zero;
            }

            var direction = _currentInput.normalized;
            return direction * (_config.BaseMoveSpeed * ComputeSpeedMultiplier() * inputMagnitude);
        }

        private float ComputeSpeedMultiplier()
        {
            var recoveryMultiplier = _invincibleTimeRemaining > 0f ? _combatConfig.RecoverySpeedMultiplier : 1f;
            var afkMultiplier = _currentState == MovementState.AFKAutoMove ? _config.AfkAutoMoveMultiplier : 1f;

            return _baseMultiplier
                * (1f + _spellBoost)
                * (1f + _classPassive)
                * (1f + _metaProgress)
                * recoveryMultiplier
                * afkMultiplier;
        }

        private bool IsValidMovementInput(Vector2 input)
        {
            return _config != null && input.magnitude >= _config.DeadzoneNormalized;
        }

        private Vector2 ClampToArena(Vector2 position)
        {
            var bounds = GetArenaBounds();

            if (bounds.width <= 0f || bounds.height <= 0f)
            {
                return position;
            }

            position.x = Mathf.Clamp(position.x, bounds.xMin, bounds.xMax);
            position.y = Mathf.Clamp(position.y, bounds.yMin, bounds.yMax);
            return position;
        }

        private Rect GetArenaBounds()
        {
            if (_arenaBounds.width > 0f && _arenaBounds.height > 0f)
            {
                return _arenaBounds;
            }

            return _config != null ? _config.ArenaBounds : default;
        }

        private void ClampImmediatePosition()
        {
            if (_rigidbody == null)
            {
                return;
            }

            _rigidbody.position = ClampToArena(_rigidbody.position);
        }

        private Vector2 GetAfkDirection()
        {
            if (_lastValidDirection.sqrMagnitude > 0f)
            {
                return _lastValidDirection.normalized;
            }

            var fallback = _config.AfkFallbackDirection;
            return fallback.sqrMagnitude > 0f ? fallback.normalized : Vector2.right;
        }

        private Vector2 GetFallbackDirection()
        {
            if (_lastValidDirection.sqrMagnitude > 0f)
            {
                return -_lastValidDirection.normalized;
            }

            var afkDirection = GetAfkDirection();
            return afkDirection.sqrMagnitude > 0f ? -afkDirection.normalized : Vector2.left;
        }

        private Vector2 ResolveDamageSourcePosition(int sourceId)
        {
            var sourceObject = Resources.InstanceIDToObject(sourceId);

            if (sourceObject is Component component)
            {
                return component.transform.position;
            }

            if (sourceObject is GameObject gameObject)
            {
                return gameObject.transform.position;
            }

            if (sourceObject is Transform sourceTransform)
            {
                return sourceTransform.position;
            }

            return _rigidbody != null ? _rigidbody.position - GetFallbackDirection() : (Vector2)transform.position;
        }

        private void ConfigureRigidbody()
        {
            if (_rigidbody == null)
            {
                return;
            }

            _rigidbody.bodyType = RigidbodyType2D.Kinematic;
            _rigidbody.useFullKinematicContacts = true;
            _rigidbody.interpolation = RigidbodyInterpolation2D.Interpolate;
        }
    }
}
