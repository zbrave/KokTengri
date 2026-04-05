using System.Collections.Generic;
using KokTengri.Core;
using UnityEngine;

namespace KokTengri.Gameplay
{
    /// <summary>
    /// Persistent orbit spell effect for Kaya Kalkani (Rock Shield).
    /// Spawns orbiting rock entities that deal damage to enemies on contact
    /// and scale in count and radius with spell level.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class KayaKalkaniEffect : SpellEffectBase
    {
        private const int ContactBufferSize = 16;

        [SerializeField] private SpellDefinitionSO _spellDef;

        private readonly Dictionary<int, float> _enemyLastTickTimeById = new();
        private readonly List<ActiveRock> _activeRocks = new();
        private readonly Collider2D[] _contactBuffer = new Collider2D[ContactBufferSize];

        private ObjectPool<RockEntity> _rockPool;
        private RockEntity _rockPrefab;
        private Transform _poolRoot;
        private Vector2 _playerPosition;
        private int _spellLevel = 1;
        private bool _isActive;

        /// <summary>
        /// Activates the orbiting rock shield effect.
        /// </summary>
        public override SpellActivationResult Activate(SpellActivationContext context)
        {
            if (_spellDef == null)
            {
                Debug.LogWarning($"{nameof(KayaKalkaniEffect)} requires a {nameof(SpellDefinitionSO)} reference.", this);
                return SpellActivationResult.Failed;
            }

            Deactivate();
            EnsurePool();

            _playerPosition = context.PlayerPosition;
            _spellLevel = DamageCalculator.ClampSpellLevel(context.SpellLevel);
            _isActive = true;

            float baseRadius = _spellDef.Range * (1f + (0.12f * (_spellLevel - 1)));
            float collisionRadius = Mathf.Max(0.01f, _spellDef.AreaRadius);
            float orbitSpeed = _spellDef.Speed;
            RockLayout layout = ResolveRockLayout(_spellLevel, baseRadius);

            for (int i = 0; i < layout.Count; i++)
            {
                if (!_rockPool.TryTake(out RockEntity rockEntity))
                {
                    continue;
                }

                float angle = (Mathf.PI * 2f * i) / Mathf.Max(1, layout.Count);
                float rockRadius = collisionRadius * (1f + (0.1f * (_spellLevel - 1)));
                rockEntity.Configure(rockRadius);

                ActiveRock activeRock = new(rockEntity, angle, layout.Radius, orbitSpeed);
                UpdateRockPose(ref activeRock, 0f);
                _activeRocks.Add(activeRock);
            }

            EventBus.Publish(new SpellEffectActivatedEvent(_spellDef.SpellId, _playerPosition));
            return _activeRocks.Count > 0 ? SpellActivationResult.Succeeded : SpellActivationResult.Failed;
        }

        /// <summary>
        /// Deactivates the orbit effect and returns spawned rocks to the pool.
        /// </summary>
        public override void Deactivate()
        {
            for (int i = 0; i < _activeRocks.Count; i++)
            {
                if (_activeRocks[i].Entity != null && _rockPool != null)
                {
                    _rockPool.Return(_activeRocks[i].Entity);
                }
            }

            _activeRocks.Clear();
            _enemyLastTickTimeById.Clear();
            _isActive = false;
        }

        /// <summary>
        /// Performs deterministic cleanup when a run ends.
        /// </summary>
        public override void OnRunEnd()
        {
            Deactivate();
        }

        protected override void OnEffectEnabled()
        {
            EventBus.Subscribe<PlayerPositionEvent>(HandlePlayerPosition);
        }

        protected override void OnEffectDisabled()
        {
            EventBus.Unsubscribe<PlayerPositionEvent>(HandlePlayerPosition);
            Deactivate();
        }

        private void OnDestroy()
        {
            _rockPool?.Dispose();

            if (_rockPrefab != null)
            {
                Destroy(_rockPrefab.gameObject);
            }
        }

        private void Update()
        {
            if (!_isActive || _spellDef == null)
            {
                return;
            }

            float deltaTime = Time.unscaledDeltaTime;
            for (int i = 0; i < _activeRocks.Count; i++)
            {
                ActiveRock activeRock = _activeRocks[i];
                UpdateRockPose(ref activeRock, deltaTime);
                ProcessRockContacts(activeRock);
                _activeRocks[i] = activeRock;
            }
        }

        private void ProcessRockContacts(ActiveRock activeRock)
        {
            int hitCount = Physics2D.OverlapCircleNonAlloc(activeRock.WorldPosition, Mathf.Max(0.01f, _spellDef.AreaRadius), _contactBuffer);
            float currentTime = Time.unscaledTime;
            float tickInterval = _spellDef.CooldownSeconds;

            for (int i = 0; i < hitCount; i++)
            {
                Collider2D collider = _contactBuffer[i];
                _contactBuffer[i] = null;

                if (!TryResolveEnemy(collider, out EnemyHealth enemy))
                {
                    continue;
                }

                int enemyId = enemy.GetInstanceID();
                bool canTick = !_enemyLastTickTimeById.TryGetValue(enemyId, out float enemyLastTickTime)
                    || currentTime - enemyLastTickTime >= tickInterval;

                if (!canTick)
                {
                    continue;
                }

                _enemyLastTickTimeById[enemyId] = currentTime;
                int damage = RequestDamage(_spellDef.BaseDamage, _spellLevel, _spellDef.ElementA, _spellDef.ElementB, enemy.EnemyType, CurrentHeroClass);
                enemy.TakeDamage(damage, _spellDef.ElementA);
                EventBus.Publish(new SpellEffectHitEvent(_spellDef.SpellId, enemyId, damage));
            }
        }

        private void UpdateRockPose(ref ActiveRock activeRock, float deltaTime)
        {
            activeRock.Angle += activeRock.OrbitSpeed * deltaTime;
            Vector2 offset = new(Mathf.Cos(activeRock.Angle), Mathf.Sin(activeRock.Angle));
            Vector2 rockPosition = _playerPosition + (offset * activeRock.Radius);
            activeRock.WorldPosition = rockPosition;

            if (activeRock.Entity != null)
            {
                activeRock.Entity.SetWorldPosition(rockPosition);
            }
        }

        private void HandlePlayerPosition(PlayerPositionEvent eventData)
        {
            _playerPosition = eventData.Position;
        }

        private void EnsurePool()
        {
            if (_rockPool != null)
            {
                return;
            }

            _poolRoot = new GameObject($"{nameof(KayaKalkaniEffect)}_PoolRoot").transform;
            _poolRoot.SetParent(transform, false);
            _rockPrefab = CreateRockPrefab();
            _rockPool = new ObjectPool<RockEntity>(_rockPrefab, 0, Mathf.Max(4, _spellDef != null ? _spellDef.MaxLevel : 5), PoolOverflowPolicy.Expand, _poolRoot);
        }

        private RockEntity CreateRockPrefab()
        {
            GameObject prefabObject = new($"{nameof(KayaKalkaniEffect)}_RockPrefab");
            prefabObject.transform.SetParent(transform, false);
            prefabObject.hideFlags = HideFlags.HideAndDontSave;
            RockEntity rockEntity = prefabObject.AddComponent<RockEntity>();
            prefabObject.SetActive(false);
            return rockEntity;
        }

        /// <summary>
        /// Determines rock count and orbit radius based on spell level.
        /// Kaya Kalkani gains additional rocks at levels 3 and 5.
        /// </summary>
        private static RockLayout ResolveRockLayout(int spellLevel, float baseRadius)
        {
            return spellLevel switch
            {
                1 => new RockLayout(2, baseRadius),
                2 => new RockLayout(2, baseRadius * 1.1f),
                3 => new RockLayout(3, baseRadius * 1.15f),
                4 => new RockLayout(3, baseRadius * 1.2f),
                _ => new RockLayout(4, baseRadius * 1.25f),
            };
        }

        private static bool TryResolveEnemy(Collider2D collider, out EnemyHealth enemy)
        {
            enemy = null;

            if (collider == null)
            {
                return false;
            }

            enemy = collider.GetComponentInParent<EnemyHealth>();
            return enemy != null && enemy.IsAlive;
        }

        private readonly struct RockLayout
        {
            public RockLayout(int count, float radius)
            {
                Count = count;
                Radius = radius;
            }

            public int Count { get; }
            public float Radius { get; }
        }

        private struct ActiveRock
        {
            public ActiveRock(RockEntity entity, float angle, float radius, float orbitSpeed)
            {
                Entity = entity;
                Angle = angle;
                Radius = radius;
                OrbitSpeed = orbitSpeed;
                WorldPosition = Vector2.zero;
            }

            public RockEntity Entity;
            public float Angle;
            public float Radius;
            public float OrbitSpeed;
            public Vector2 WorldPosition;
        }

        /// <summary>
        /// Pooled rock entity rendered as a filled circle using a LineRenderer.
        /// </summary>
        private sealed class RockEntity : MonoBehaviour, IPooledObject
        {
            private const int CircleSegments = 16;

            private LineRenderer _lineRenderer;
            private float _visualRadius;

            public bool IsActive => gameObject.activeSelf;

            /// <summary>
            /// Sets the visual radius and re-renders the rock circle.
            /// </summary>
            public void Configure(float visualRadius)
            {
                EnsureLineRenderer();
                _visualRadius = Mathf.Max(0.01f, visualRadius);
                float lineWidth = Mathf.Max(0.02f, _visualRadius * 0.8f);
                RenderRock(lineWidth);
            }

            /// <summary>
            /// Moves the rock to the specified world position.
            /// </summary>
            public void SetWorldPosition(Vector2 position)
            {
                transform.position = position;
            }

            public void OnPoolCreate()
            {
                EnsureLineRenderer();
            }

            public void OnPoolTake()
            {
                EnsureLineRenderer();
            }

            public void OnPoolReturn()
            {
                transform.position = Vector3.zero;
            }

            public void OnPoolDestroy()
            {
            }

            private void EnsureLineRenderer()
            {
                if (_lineRenderer != null)
                {
                    return;
                }

                _lineRenderer = gameObject.GetComponent<LineRenderer>();
                if (_lineRenderer == null)
                {
                    _lineRenderer = gameObject.AddComponent<LineRenderer>();
                }

                _lineRenderer.useWorldSpace = false;
                _lineRenderer.loop = true;
                _lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                _lineRenderer.receiveShadows = false;
            }

            private void RenderRock(float lineWidth)
            {
                if (_lineRenderer == null)
                {
                    return;
                }

                _lineRenderer.startWidth = lineWidth;
                _lineRenderer.endWidth = lineWidth;
                _lineRenderer.positionCount = CircleSegments;

                for (int i = 0; i < CircleSegments; i++)
                {
                    float t = i / (float)CircleSegments;
                    float angle = t * Mathf.PI * 2f;
                    Vector3 point = new(Mathf.Cos(angle) * _visualRadius, Mathf.Sin(angle) * _visualRadius, 0f);
                    _lineRenderer.SetPosition(i, point);
                }
            }
        }
    }
}
