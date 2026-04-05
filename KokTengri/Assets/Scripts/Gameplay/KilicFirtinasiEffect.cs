using System.Collections.Generic;
using KokTengri.Core;
using UnityEngine;

namespace KokTengri.Gameplay
{
    /// <summary>
    /// Projectile volley spell effect for Kilic Firtinasi.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class KilicFirtinasiEffect : SpellEffectBase
    {
        private const int ContactBufferSize = 12;

        [SerializeField] private SpellDefinitionSO _spellDef;

        private readonly List<ActiveProjectile> _activeProjectiles = new();
        private readonly Collider2D[] _contactBuffer = new Collider2D[ContactBufferSize];

        private ObjectPool<SwordProjectileEntity> _projectilePool;
        private SwordProjectileEntity _projectilePrefab;
        private Transform _poolRoot;
        private int _spellLevel = 1;

        /// <summary>
        /// Activates the sword volley effect.
        /// </summary>
        public override SpellActivationResult Activate(SpellActivationContext context)
        {
            if (_spellDef == null)
            {
                Debug.LogWarning($"{nameof(KilicFirtinasiEffect)} requires a {nameof(SpellDefinitionSO)} reference.", this);
                return SpellActivationResult.Failed;
            }

            EnsurePool();
            _spellLevel = DamageCalculator.ClampSpellLevel(context.SpellLevel);

            Vector2 launchDirection = ResolveLaunchDirection(context);
            if (launchDirection.sqrMagnitude <= Mathf.Epsilon)
            {
                return SpellActivationResult.Failed;
            }

            launchDirection.Normalize();

            VolleyPattern pattern = ResolveVolleyPattern(_spellLevel, _spellDef.AreaRadius);
            float projectileSpeed = Mathf.Max(0.01f, _spellDef.Speed * (1f + (_spellDef.DamageScalingPerLevel * (_spellLevel - 1))));
            float maxDistance = Mathf.Max(0.01f, _spellDef.Range);
            float lifetime = maxDistance / projectileSpeed;
            float visualWidth = Mathf.Max(0.02f, _spellDef.AreaRadius * 0.35f);
            float visualLength = Mathf.Max(visualWidth, _spellDef.AreaRadius * 1.5f);

            for (int i = 0; i < pattern.ProjectileCount; i++)
            {
                if (!_projectilePool.TryTake(out SwordProjectileEntity projectileEntity))
                {
                    continue;
                }

                float angleOffset = GetAngleOffset(i, pattern.ProjectileCount, pattern.TotalSpreadAngleDegrees);
                Vector2 projectileDirection = Rotate(launchDirection, angleOffset).normalized;
                projectileEntity.Configure(context.PlayerPosition, projectileDirection, visualLength, visualWidth);

                _activeProjectiles.Add(new ActiveProjectile(
                    projectileEntity,
                    context.PlayerPosition,
                    projectileDirection,
                    context.PlayerPosition,
                    0f,
                    lifetime,
                    projectileSpeed,
                    maxDistance,
                    true));
            }

            EventBus.Publish(new SpellEffectActivatedEvent(_spellDef.SpellId, context.PlayerPosition));
            return _activeProjectiles.Count > 0 ? SpellActivationResult.Succeeded : SpellActivationResult.Failed;
        }

        /// <summary>
        /// Deactivates the projectile effect and returns active projectiles to the pool.
        /// </summary>
        public override void Deactivate()
        {
            for (int i = 0; i < _activeProjectiles.Count; i++)
            {
                if (_activeProjectiles[i].Active && _activeProjectiles[i].Entity != null && _projectilePool != null)
                {
                    _projectilePool.Return(_activeProjectiles[i].Entity);
                }
            }

            _activeProjectiles.Clear();
        }

        /// <summary>
        /// Performs deterministic cleanup when a run ends.
        /// </summary>
        public override void OnRunEnd()
        {
            Deactivate();
        }

        protected override void OnEffectDisabled()
        {
            Deactivate();
        }

        private void OnDestroy()
        {
            _projectilePool?.Dispose();

            if (_projectilePrefab != null)
            {
                Destroy(_projectilePrefab.gameObject);
            }
        }

        private void Update()
        {
            if (_spellDef == null || _activeProjectiles.Count == 0)
            {
                return;
            }

            float deltaTime = Time.unscaledDeltaTime;

            for (int i = _activeProjectiles.Count - 1; i >= 0; i--)
            {
                ActiveProjectile projectile = _activeProjectiles[i];
                if (!projectile.Active)
                {
                    _activeProjectiles.RemoveAt(i);
                    continue;
                }

                projectile.ElapsedLife += deltaTime;
                projectile.Position += projectile.Direction * projectile.Speed * deltaTime;

                if (projectile.Entity != null)
                {
                    projectile.Entity.SetPose(projectile.Position, projectile.Direction);
                }

                if (TryHitEnemy(projectile))
                {
                    ReturnProjectile(projectile.Entity);
                    _activeProjectiles.RemoveAt(i);
                    continue;
                }

                bool hasExpired = projectile.ElapsedLife >= projectile.Lifetime;
                bool outOfBounds = Vector2.Distance(projectile.Origin, projectile.Position) >= projectile.MaxDistance;
                if (hasExpired || outOfBounds)
                {
                    ReturnProjectile(projectile.Entity);
                    _activeProjectiles.RemoveAt(i);
                    continue;
                }

                _activeProjectiles[i] = projectile;
            }
        }

        private bool TryHitEnemy(ActiveProjectile projectile)
        {
            int hitCount = Physics2D.OverlapCircleNonAlloc(projectile.Position, Mathf.Max(0.01f, _spellDef.AreaRadius), _contactBuffer);
            for (int i = 0; i < hitCount; i++)
            {
                Collider2D collider = _contactBuffer[i];
                _contactBuffer[i] = null;

                if (!TryResolveEnemy(collider, out EnemyHealth enemy))
                {
                    continue;
                }

                int damage = RequestDamage(_spellDef.BaseDamage, _spellLevel, _spellDef.ElementA, _spellDef.ElementB, enemy.EnemyType, CurrentHeroClass);
                enemy.TakeDamage(damage, _spellDef.ElementA);
                EventBus.Publish(new SpellEffectHitEvent(_spellDef.SpellId, enemy.GetInstanceID(), damage));
                return true;
            }

            return false;
        }

        private void EnsurePool()
        {
            if (_projectilePool != null)
            {
                return;
            }

            _poolRoot = new GameObject($"{nameof(KilicFirtinasiEffect)}_PoolRoot").transform;
            _poolRoot.SetParent(transform, false);
            _projectilePrefab = CreateProjectilePrefab();
            _projectilePool = new ObjectPool<SwordProjectileEntity>(_projectilePrefab, 0, Mathf.Max(4, _spellDef != null ? _spellDef.MaxLevel : 5), PoolOverflowPolicy.Expand, _poolRoot);
        }

        private SwordProjectileEntity CreateProjectilePrefab()
        {
            GameObject prefabObject = new($"{nameof(KilicFirtinasiEffect)}_ProjectilePrefab");
            prefabObject.transform.SetParent(transform, false);
            prefabObject.hideFlags = HideFlags.HideAndDontSave;
            SwordProjectileEntity projectileEntity = prefabObject.AddComponent<SwordProjectileEntity>();
            prefabObject.SetActive(false);
            return projectileEntity;
        }

        private Vector2 ResolveLaunchDirection(SpellActivationContext context)
        {
            if (context.LastMovementDirection.HasValue && context.LastMovementDirection.Value.sqrMagnitude > Mathf.Epsilon)
            {
                return context.LastMovementDirection.Value.normalized;
            }

            if (context.PlayerFacingDirection.sqrMagnitude > Mathf.Epsilon)
            {
                return context.PlayerFacingDirection.normalized;
            }

            EnemyHealth nearestEnemy = FindNearestEnemy(context.PlayerPosition);
            if (nearestEnemy != null)
            {
                Vector2 toEnemy = (Vector2)nearestEnemy.transform.position - context.PlayerPosition;
                if (toEnemy.sqrMagnitude > Mathf.Epsilon)
                {
                    return toEnemy.normalized;
                }
            }

            return Vector2.zero;
        }

        private static VolleyPattern ResolveVolleyPattern(int spellLevel, float spreadSource)
        {
            float baseSpreadDegrees = Mathf.Rad2Deg * Mathf.Max(0f, spreadSource);

            return spellLevel switch
            {
                1 => new VolleyPattern(1, 0f),
                2 => new VolleyPattern(1, 0f),
                3 => new VolleyPattern(2, baseSpreadDegrees * 0.35f),
                4 => new VolleyPattern(3, baseSpreadDegrees * 0.75f),
                _ => new VolleyPattern(4, baseSpreadDegrees),
            };
        }

        private static float GetAngleOffset(int index, int projectileCount, float totalSpreadAngleDegrees)
        {
            if (projectileCount <= 1 || totalSpreadAngleDegrees <= 0f)
            {
                return 0f;
            }

            float normalizedIndex = index / (float)(projectileCount - 1);
            return Mathf.Lerp(-totalSpreadAngleDegrees * 0.5f, totalSpreadAngleDegrees * 0.5f, normalizedIndex);
        }

        private static Vector2 Rotate(Vector2 direction, float angleDegrees)
        {
            float radians = angleDegrees * Mathf.Deg2Rad;
            float cos = Mathf.Cos(radians);
            float sin = Mathf.Sin(radians);
            return new Vector2(
                (direction.x * cos) - (direction.y * sin),
                (direction.x * sin) + (direction.y * cos));
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

        private static EnemyHealth FindNearestEnemy(Vector2 origin)
        {
            EnemyHealth[] enemies = Object.FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None);
            EnemyHealth nearestEnemy = null;
            float nearestDistanceSqr = float.PositiveInfinity;

            for (int i = 0; i < enemies.Length; i++)
            {
                EnemyHealth enemy = enemies[i];
                if (enemy == null || !enemy.IsAlive)
                {
                    continue;
                }

                float distanceSqr = ((Vector2)enemy.transform.position - origin).sqrMagnitude;
                if (distanceSqr >= nearestDistanceSqr)
                {
                    continue;
                }

                nearestEnemy = enemy;
                nearestDistanceSqr = distanceSqr;
            }

            return nearestEnemy;
        }

        private void ReturnProjectile(SwordProjectileEntity entity)
        {
            if (entity == null || _projectilePool == null)
            {
                return;
            }

            _projectilePool.Return(entity);
        }

        private readonly struct VolleyPattern
        {
            public VolleyPattern(int projectileCount, float totalSpreadAngleDegrees)
            {
                ProjectileCount = projectileCount;
                TotalSpreadAngleDegrees = totalSpreadAngleDegrees;
            }

            public int ProjectileCount { get; }
            public float TotalSpreadAngleDegrees { get; }
        }

        private struct ActiveProjectile
        {
            public ActiveProjectile(
                SwordProjectileEntity entity,
                Vector2 position,
                Vector2 direction,
                Vector2 origin,
                float elapsedLife,
                float lifetime,
                float speed,
                float maxDistance,
                bool active)
            {
                Entity = entity;
                Position = position;
                Direction = direction;
                Origin = origin;
                ElapsedLife = elapsedLife;
                Lifetime = lifetime;
                Speed = speed;
                MaxDistance = maxDistance;
                Active = active;
            }

            public SwordProjectileEntity Entity;
            public Vector2 Position;
            public Vector2 Direction;
            public Vector2 Origin;
            public float ElapsedLife;
            public float Lifetime;
            public float Speed;
            public float MaxDistance;
            public bool Active;
        }

        private sealed class SwordProjectileEntity : MonoBehaviour, IPooledObject
        {
            private LineRenderer _lineRenderer;
            private float _length;

            public bool IsActive => gameObject.activeSelf;

            public void Configure(Vector2 position, Vector2 direction, float length, float width)
            {
                EnsureLineRenderer();
                _length = Mathf.Max(width, length);
                _lineRenderer.startWidth = width;
                _lineRenderer.endWidth = width;
                SetPose(position, direction);
            }

            public void SetPose(Vector2 position, Vector2 direction)
            {
                transform.position = position;
                if (_lineRenderer == null)
                {
                    return;
                }

                Vector3 halfExtent = (Vector3)(direction.normalized * (_length * 0.5f));
                _lineRenderer.SetPosition(0, -halfExtent);
                _lineRenderer.SetPosition(1, halfExtent);
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

                _lineRenderer.positionCount = 2;
                _lineRenderer.useWorldSpace = false;
                _lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                _lineRenderer.receiveShadows = false;
            }
        }
    }
}
