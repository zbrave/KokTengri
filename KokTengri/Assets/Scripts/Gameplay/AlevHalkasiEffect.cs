using System;
using System.Collections.Generic;
using KokTengri.Core;
using UnityEngine;

namespace KokTengri.Gameplay
{
    /// <summary>
    /// Persistent orbit spell effect for Alev Halkasi.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class AlevHalkasiEffect : SpellEffectBase
    {
        private const int ContactBufferSize = 16;
        private const int CircleSegments = 40;

        [SerializeField] private SpellDefinitionSO _spellDef;

        private readonly Dictionary<int, float> _enemyLastTickTimeById = new();
        private readonly List<ActiveRing> _activeRings = new();
        private readonly Collider2D[] _contactBuffer = new Collider2D[ContactBufferSize];

        private ObjectPool<OrbitRingEntity> _ringPool;
        private OrbitRingEntity _ringPrefab;
        private Transform _poolRoot;
        private Vector2 _playerPosition;
        private int _spellLevel = 1;
        private bool _isActive;

        /// <summary>
        /// Activates the orbiting fire ring effect.
        /// </summary>
        public override SpellActivationResult Activate(SpellActivationContext context)
        {
            if (_spellDef == null)
            {
                Debug.LogWarning($"{nameof(AlevHalkasiEffect)} requires a {nameof(SpellDefinitionSO)} reference.", this);
                return SpellActivationResult.Failed;
            }

            Deactivate();
            EnsurePool();

            _playerPosition = context.PlayerPosition;
            _spellLevel = DamageCalculator.ClampSpellLevel(context.SpellLevel);
            _isActive = true;

            float baseRadius = _spellDef.Range * (1f + (0.15f * (_spellLevel - 1)));
            float collisionRadius = Mathf.Max(0.01f, _spellDef.AreaRadius);
            float orbitSpeed = _spellDef.Speed;
            RingLayout layout = ResolveRingLayout(_spellLevel, baseRadius);

            for (int i = 0; i < layout.Radii.Length; i++)
            {
                if (!_ringPool.TryTake(out OrbitRingEntity ringEntity))
                {
                    continue;
                }

                float angle = (Mathf.PI * 2f * i) / Mathf.Max(1, layout.Radii.Length);
                ringEntity.Configure(collisionRadius, collisionRadius * 0.2f, CircleSegments);

                ActiveRing activeRing = new(ringEntity, angle, layout.Radii[i], orbitSpeed);
                UpdateRingPose(ref activeRing, 0f);
                _activeRings.Add(activeRing);
            }

            EventBus.Publish(new SpellEffectActivatedEvent(_spellDef.SpellId, _playerPosition));
            return _activeRings.Count > 0 ? SpellActivationResult.Succeeded : SpellActivationResult.Failed;
        }

        /// <summary>
        /// Deactivates the orbit effect and returns spawned rings to the pool.
        /// </summary>
        public override void Deactivate()
        {
            for (int i = 0; i < _activeRings.Count; i++)
            {
                if (_activeRings[i].Entity != null && _ringPool != null)
                {
                    _ringPool.Return(_activeRings[i].Entity);
                }
            }

            _activeRings.Clear();
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
            _ringPool?.Dispose();

            if (_ringPrefab != null)
            {
                Destroy(_ringPrefab.gameObject);
            }
        }

        private void Update()
        {
            if (!_isActive || _spellDef == null)
            {
                return;
            }

            float deltaTime = Time.unscaledDeltaTime;
            for (int i = 0; i < _activeRings.Count; i++)
            {
                ActiveRing activeRing = _activeRings[i];
                UpdateRingPose(ref activeRing, deltaTime);
                ProcessRingContacts(activeRing);
                _activeRings[i] = activeRing;
            }
        }

        private void ProcessRingContacts(ActiveRing activeRing)
        {
            int hitCount = Physics2D.OverlapCircleNonAlloc(activeRing.WorldPosition, Mathf.Max(0.01f, _spellDef.AreaRadius), _contactBuffer);
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

        private void UpdateRingPose(ref ActiveRing activeRing, float deltaTime)
        {
            activeRing.Angle += activeRing.OrbitSpeed * deltaTime;
            Vector2 offset = new(Mathf.Cos(activeRing.Angle), Mathf.Sin(activeRing.Angle));
            Vector2 ringPosition = _playerPosition + (offset * activeRing.Radius);
            activeRing.WorldPosition = ringPosition;

            if (activeRing.Entity != null)
            {
                activeRing.Entity.SetWorldPosition(ringPosition);
            }
        }

        private void HandlePlayerPosition(PlayerPositionEvent eventData)
        {
            _playerPosition = eventData.Position;
        }

        private void EnsurePool()
        {
            if (_ringPool != null)
            {
                return;
            }

            _poolRoot = new GameObject($"{nameof(AlevHalkasiEffect)}_PoolRoot").transform;
            _poolRoot.SetParent(transform, false);
            _ringPrefab = CreateRingPrefab();
            _ringPool = new ObjectPool<OrbitRingEntity>(_ringPrefab, 0, Mathf.Max(3, _spellDef != null ? _spellDef.MaxLevel : 5), PoolOverflowPolicy.Expand, _poolRoot);
        }

        private OrbitRingEntity CreateRingPrefab()
        {
            GameObject prefabObject = new($"{nameof(AlevHalkasiEffect)}_RingPrefab");
            prefabObject.transform.SetParent(transform, false);
            prefabObject.hideFlags = HideFlags.HideAndDontSave;
            OrbitRingEntity ringEntity = prefabObject.AddComponent<OrbitRingEntity>();
            prefabObject.SetActive(false);
            return ringEntity;
        }

        private static RingLayout ResolveRingLayout(int spellLevel, float baseRadius)
        {
            return spellLevel switch
            {
                1 => new RingLayout(new[] { baseRadius }),
                2 => new RingLayout(new[] { baseRadius }),
                3 => new RingLayout(new[] { baseRadius * 0.8f, baseRadius * 1.2f }),
                4 => new RingLayout(new[] { baseRadius * 0.85f, baseRadius * 1.25f }),
                _ => new RingLayout(new[] { baseRadius * 0.75f, baseRadius, baseRadius * 1.3f }),
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

        private readonly struct RingLayout
        {
            public RingLayout(float[] radii)
            {
                Radii = radii;
            }

            public float[] Radii { get; }
        }

        private struct ActiveRing
        {
            public ActiveRing(OrbitRingEntity entity, float angle, float radius, float orbitSpeed)
            {
                Entity = entity;
                Angle = angle;
                Radius = radius;
                OrbitSpeed = orbitSpeed;
                WorldPosition = Vector2.zero;
            }

            public OrbitRingEntity Entity;
            public float Angle;
            public float Radius;
            public float OrbitSpeed;
            public Vector2 WorldPosition;
        }

        private sealed class OrbitRingEntity : MonoBehaviour, IPooledObject
        {
            private LineRenderer _lineRenderer;
            private float _visualRadius;
            private float _lineWidth;
            private int _segments;

            public bool IsActive => gameObject.activeSelf;

            public void Configure(float visualRadius, float lineWidth, int segments)
            {
                EnsureLineRenderer();
                _visualRadius = Mathf.Max(0.01f, visualRadius);
                _lineWidth = Mathf.Max(0.01f, lineWidth);
                _segments = Mathf.Max(8, segments);
                RenderCircle();
            }

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
                _lineRenderer.loop = false;
                _lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                _lineRenderer.receiveShadows = false;
            }

            private void RenderCircle()
            {
                if (_lineRenderer == null)
                {
                    return;
                }

                _lineRenderer.startWidth = _lineWidth;
                _lineRenderer.endWidth = _lineWidth;
                _lineRenderer.positionCount = _segments + 1;

                for (int i = 0; i <= _segments; i++)
                {
                    float t = i / (float)_segments;
                    float angle = t * Mathf.PI * 2f;
                    Vector3 point = new(Mathf.Cos(angle) * _visualRadius, Mathf.Sin(angle) * _visualRadius, 0f);
                    _lineRenderer.SetPosition(i, point);
                }
            }
        }
    }
}
