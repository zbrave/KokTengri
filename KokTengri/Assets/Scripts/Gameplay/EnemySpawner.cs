using System;
using System.Collections.Generic;
using KokTengri.Core;
using UnityEngine;

namespace KokTengri.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class EnemySpawner : MonoBehaviour
    {
        [Serializable]
        private struct PoolBinding
        {
            [SerializeField] private EnemyType _enemyType;
            [SerializeField] private string _bossId;
            [SerializeField] private EnemyHealth _prefab;
            [SerializeField, Min(0)] private int _initialSize;
            [SerializeField, Min(1)] private int _maxSize;
            [SerializeField] private PoolOverflowPolicy _overflowPolicy;

            public EnemyType EnemyType => _enemyType;
            public string BossId => _bossId;
            public EnemyHealth Prefab => _prefab;
            public int InitialSize => _initialSize;
            public int MaxSize => _maxSize;
            public PoolOverflowPolicy OverflowPolicy => _overflowPolicy;
        }

        private readonly struct SpawnTicket
        {
            public SpawnTicket(EnemyDefinitionSO definition, float hpMultiplier, float damageMultiplier, bool isElite, float runTime, bool isBoss)
            {
                Definition = definition;
                HpMultiplier = hpMultiplier;
                DamageMultiplier = damageMultiplier;
                IsElite = isElite;
                RunTime = runTime;
                IsBoss = isBoss;
            }

            public EnemyDefinitionSO Definition { get; }
            public float HpMultiplier { get; }
            public float DamageMultiplier { get; }
            public bool IsElite { get; }
            public float RunTime { get; }
            public bool IsBoss { get; }
        }

        private readonly struct ActiveEnemyRecord
        {
            public ActiveEnemyRecord(EnemyHealth enemy, EnemyDefinitionSO definition, ObjectPool<EnemyHealth> pool, bool isBoss)
            {
                Enemy = enemy;
                Definition = definition;
                Pool = pool;
                IsBoss = isBoss;
            }

            public EnemyHealth Enemy { get; }
            public EnemyDefinitionSO Definition { get; }
            public ObjectPool<EnemyHealth> Pool { get; }
            public bool IsBoss { get; }
        }

        [Header("References")]
        [SerializeField] private WaveManager _waveManager;
        [SerializeField] private RunManager _runManager;
        [SerializeField] private DifficultyConfigSO _difficultyConfig;
        [SerializeField] private EnemyDefinitionSO[] _enemyDefinitions;
        [SerializeField] private Camera _spawnCamera;
        [SerializeField] private Transform _poolRoot;

        [Header("Pools")]
        [SerializeField] private PoolBinding[] _poolBindings = Array.Empty<PoolBinding>();

        [Header("Spawn Bounds")]
        [SerializeField] private Rect _arenaBounds = new(-50f, -50f, 100f, 100f);
        [SerializeField, Min(0.1f)] private float _spawnDistanceFromPlayer = 12f;
        [SerializeField, Min(0f)] private float _spawnDistanceVariance = 2f;
        [SerializeField, Min(0.1f)] private float _minSpawnDistanceFromPlayer = 6f;
        [SerializeField, Min(0.1f)] private float _maxSpawnDistanceFromPlayer = 18f;
        [SerializeField, Min(0f)] private float _screenEdgePadding = 1.5f;
        [SerializeField, Min(1)] private int _spawnPositionRetryCount = 4;

        [Header("Runtime Limits")]
        [SerializeField, Min(1)] private int _maxActiveEnemies = 300;
        [SerializeField, Min(1)] private int _queueDrainPerFrameLimit = 12;
        [SerializeField] private bool _spawnBossAtArenaCenter = true;

        private readonly Dictionary<int, ActiveEnemyRecord> _activeEnemiesById = new();
        private readonly Dictionary<EnemyType, ObjectPool<EnemyHealth>> _poolByEnemyType = new();
        private readonly Dictionary<string, ObjectPool<EnemyHealth>> _poolByBossId = new(StringComparer.OrdinalIgnoreCase);
        private readonly Queue<SpawnTicket> _pendingSpawnQueue = new();
        private readonly List<SpawnTicket> _batchTickets = new();
        private readonly List<SpawnTicket> _failedBatchTickets = new();

        private System.Random _random = new();
        private Vector2 _lastKnownPlayerPosition;
        private bool _hasPlayerPosition;
        private bool _isRunActive;
        private bool _poolsInitialized;

        private void Awake()
        {
            if (_spawnCamera == null)
            {
                _spawnCamera = Camera.main;
            }
        }

        private void OnEnable()
        {
            EventBus.Subscribe<EnemyPoolReturnRequestedEvent>(HandleEnemyPoolReturnRequested);
            EventBus.Subscribe<CorSplitRequestedEvent>(HandleCorSplitRequested);
            EventBus.Subscribe<RunStartEvent>(HandleRunStart);
            EventBus.Subscribe<RunEndEvent>(HandleRunEnd);
            EventBus.Subscribe<PlayerPositionEvent>(HandlePlayerPosition);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<EnemyPoolReturnRequestedEvent>(HandleEnemyPoolReturnRequested);
            EventBus.Unsubscribe<CorSplitRequestedEvent>(HandleCorSplitRequested);
            EventBus.Unsubscribe<RunStartEvent>(HandleRunStart);
            EventBus.Unsubscribe<RunEndEvent>(HandleRunEnd);
            EventBus.Unsubscribe<PlayerPositionEvent>(HandlePlayerPosition);
        }

        private void OnDestroy()
        {
            DisposePools();
        }

        private void Update()
        {
            if (!_isRunActive || _waveManager == null || _runManager == null)
            {
                return;
            }

            if (_runManager.State != RunLifecycleState.Active)
            {
                return;
            }

            if (!_poolsInitialized)
            {
                InitializePools();
            }

            DrainQueuedSpawnTickets(_queueDrainPerFrameLimit);

            WaveManager.SpawnPlan spawnPlan = _waveManager.GetCurrentSpawnPlan();
            int spawnBudget = _waveManager.ConsumeSpawnBudget();
            EnqueueSpawnBudget(spawnPlan, spawnBudget, _runManager.ElapsedSeconds);

            DrainQueuedSpawnTickets(_queueDrainPerFrameLimit);
            SpawnPendingBossIfNeeded();
            SyncWaveManagerActiveCount();
        }

        private void HandleRunStart(RunStartEvent eventData)
        {
            _random = new System.Random(eventData.Seed);
            _isRunActive = true;
            _hasPlayerPosition = false;
            _pendingSpawnQueue.Clear();
            _activeEnemiesById.Clear();
            InitializePools();
            SyncWaveManagerActiveCount();
        }

        private void HandleRunEnd(RunEndEvent eventData)
        {
            _isRunActive = false;
            _hasPlayerPosition = false;
            _pendingSpawnQueue.Clear();
            _activeEnemiesById.Clear();
            DisposePools();
            SyncWaveManagerActiveCount();
        }

        private void HandlePlayerPosition(PlayerPositionEvent eventData)
        {
            _lastKnownPlayerPosition = eventData.Position;
            _hasPlayerPosition = true;
        }

        private void HandleEnemyPoolReturnRequested(EnemyPoolReturnRequestedEvent eventData)
        {
            if (!_activeEnemiesById.TryGetValue(eventData.EnemyId, out ActiveEnemyRecord activeEnemy))
            {
                SyncWaveManagerActiveCount();
                return;
            }

            _activeEnemiesById.Remove(eventData.EnemyId);

            if (activeEnemy.Pool != null)
            {
                activeEnemy.Pool.Return(activeEnemy.Enemy);
            }

            SyncWaveManagerActiveCount();
        }

        private void HandleCorSplitRequested(CorSplitRequestedEvent eventData)
        {
            EnemyDefinitionSO splitDefinition = ResolveEnemyDefinition(EnemyType.Cor);
            if (splitDefinition == null)
            {
                return;
            }

            float elapsedMinutes = eventData.RunTime / 60f;
            float baseHpMultiplier = DifficultyScaling.GetFinalHpMultiplier(
                elapsedMinutes,
                _runManager != null && _runManager.IsHeroModeActive,
                false,
                _difficultyConfig);
            float splitHpMultiplier = baseHpMultiplier * Mathf.Max(0.01f, eventData.SplitHpRatio);
            float damageMultiplier = DifficultyScaling.GetDamageMultiplier(elapsedMinutes, _difficultyConfig);

            Vector2 splitDirection = GetRandomDirection();
            float offsetDistance = Mathf.Max(0f, eventData.SplitOffsetDistance);
            Vector3 leftPosition = ClampToArena(eventData.Position + (Vector3)(splitDirection * offsetDistance));
            Vector3 rightPosition = ClampToArena(eventData.Position - (Vector3)(splitDirection * offsetDistance));

            TrySpawnTicket(new SpawnTicket(splitDefinition, splitHpMultiplier, damageMultiplier, eventData.IsElite, eventData.RunTime, false), leftPosition, true);
            TrySpawnTicket(new SpawnTicket(splitDefinition, splitHpMultiplier, damageMultiplier, eventData.IsElite, eventData.RunTime, false), rightPosition, true);
            SyncWaveManagerActiveCount();
        }

        private void BuildPoolLookups()
        {
            _poolByEnemyType.Clear();
            _poolByBossId.Clear();

            for (int i = 0; i < _poolBindings.Length; i++)
            {
                PoolBinding binding = _poolBindings[i];
                if (binding.Prefab == null)
                {
                    continue;
                }

                ObjectPool<EnemyHealth> pool = new(
                    binding.Prefab,
                    binding.InitialSize,
                    Mathf.Max(1, binding.MaxSize),
                    binding.OverflowPolicy,
                    _poolRoot);

                if (!string.IsNullOrWhiteSpace(binding.BossId))
                {
                    _poolByBossId[binding.BossId] = pool;
                    continue;
                }

                _poolByEnemyType[binding.EnemyType] = pool;
            }

        }

        private void InitializePools()
        {
            if (_poolsInitialized)
            {
                return;
            }

            BuildPoolLookups();

            foreach (KeyValuePair<EnemyType, ObjectPool<EnemyHealth>> poolEntry in _poolByEnemyType)
            {
                EnemyDefinitionSO definition = ResolveEnemyDefinition(poolEntry.Key);
                if (definition == null || definition.IsBoss)
                {
                    continue;
                }

                poolEntry.Value.Prewarm(GetSuggestedPrewarmCount(definition));
            }

            foreach (KeyValuePair<string, ObjectPool<EnemyHealth>> poolEntry in _poolByBossId)
            {
                EnemyDefinitionSO definition = ResolveBossDefinition(poolEntry.Key);
                if (definition == null)
                {
                    continue;
                }

                poolEntry.Value.Prewarm(1);
            }

            _poolsInitialized = true;
        }

        private void DisposePools()
        {
            foreach (KeyValuePair<EnemyType, ObjectPool<EnemyHealth>> poolEntry in _poolByEnemyType)
            {
                poolEntry.Value?.Dispose();
            }

            foreach (KeyValuePair<string, ObjectPool<EnemyHealth>> poolEntry in _poolByBossId)
            {
                poolEntry.Value?.Dispose();
            }

            _poolByEnemyType.Clear();
            _poolByBossId.Clear();
            _poolsInitialized = false;
        }

        private void EnqueueSpawnBudget(WaveManager.SpawnPlan spawnPlan, int spawnBudget, float runTime)
        {
            if (spawnBudget <= 0 || !spawnPlan.IsSpawnAllowed)
            {
                return;
            }

            float elapsedMinutes = runTime / 60f;
            float hpMultiplier = DifficultyScaling.GetFinalHpMultiplier(
                elapsedMinutes,
                _runManager != null && _runManager.IsHeroModeActive,
                false,
                _difficultyConfig);
            float damageMultiplier = DifficultyScaling.GetDamageMultiplier(elapsedMinutes, _difficultyConfig);

            for (int i = 0; i < spawnBudget; i++)
            {
                EnemyDefinitionSO definition = PickRandomAllowedEnemyDefinition(spawnPlan.AllowedEnemyTypes, elapsedMinutes);
                if (definition == null)
                {
                    continue;
                }

                bool isElite = definition.CanHaveEliteVariant && NextFloat01() <= spawnPlan.EliteChance;
                _pendingSpawnQueue.Enqueue(new SpawnTicket(definition, hpMultiplier, damageMultiplier, isElite, runTime, false));
            }
        }

        private EnemyDefinitionSO PickRandomAllowedEnemyDefinition(List<EnemyType> allowedEnemyTypes, float elapsedMinutes)
        {
            if (allowedEnemyTypes == null || allowedEnemyTypes.Count == 0)
            {
                return null;
            }

            int startIndex = _random.Next(allowedEnemyTypes.Count);
            for (int i = 0; i < allowedEnemyTypes.Count; i++)
            {
                EnemyType enemyType = allowedEnemyTypes[(startIndex + i) % allowedEnemyTypes.Count];
                if (!DifficultyScaling.IsEnemyUnlocked(enemyType, elapsedMinutes, _difficultyConfig))
                {
                    continue;
                }

                EnemyDefinitionSO definition = ResolveEnemyDefinition(enemyType);
                if (definition == null || definition.IsBoss)
                {
                    continue;
                }

                return definition;
            }

            return null;
        }

        private void DrainQueuedSpawnTickets(int maxSpawnsThisFrame)
        {
            if (maxSpawnsThisFrame <= 0 || _pendingSpawnQueue.Count == 0)
            {
                return;
            }

            int availableSlots = Mathf.Max(0, _maxActiveEnemies - GetNonBossActiveEnemyCount());
            if (availableSlots <= 0)
            {
                return;
            }

            int batchCount = Mathf.Min(Mathf.Min(maxSpawnsThisFrame, availableSlots), _pendingSpawnQueue.Count);
            if (batchCount <= 0)
            {
                return;
            }

            _batchTickets.Clear();
            _failedBatchTickets.Clear();

            for (int i = 0; i < batchCount; i++)
            {
                _batchTickets.Add(_pendingSpawnQueue.Dequeue());
            }

            float baseAngle = NextAngleRadians();
            for (int i = 0; i < _batchTickets.Count; i++)
            {
                Vector3 spawnPosition = ResolveSpawnPosition(i, _batchTickets.Count, baseAngle);
                if (!TrySpawnTicket(_batchTickets[i], spawnPosition, false))
                {
                    _failedBatchTickets.Add(_batchTickets[i]);
                }
            }

            for (int i = 0; i < _failedBatchTickets.Count; i++)
            {
                _pendingSpawnQueue.Enqueue(_failedBatchTickets[i]);
            }
        }

        private void SpawnPendingBossIfNeeded()
        {
            if (_waveManager == null || !_waveManager.IsBossSpawnPending())
            {
                return;
            }

            string pendingBossId = _waveManager.GetPendingBossId();
            EnemyDefinitionSO bossDefinition = ResolveBossDefinition(pendingBossId);
            if (bossDefinition == null)
            {
                return;
            }

            float runTime = _runManager != null ? _runManager.ElapsedSeconds : 0f;
            float elapsedMinutes = runTime / 60f;
            float hpMultiplier = DifficultyScaling.GetFinalHpMultiplier(
                elapsedMinutes,
                _runManager != null && _runManager.IsHeroModeActive,
                false,
                _difficultyConfig);
            float damageMultiplier = DifficultyScaling.GetDamageMultiplier(elapsedMinutes, _difficultyConfig);

            Vector3 bossPosition = ResolveBossSpawnPosition();
            if (!TrySpawnTicket(new SpawnTicket(bossDefinition, hpMultiplier, damageMultiplier, false, runTime, true), bossPosition, true))
            {
                return;
            }

            _waveManager.ConfirmBossSpawn(bossPosition);
        }

        private bool TrySpawnTicket(SpawnTicket ticket, Vector3 spawnPosition, bool bypassCap)
        {
            if (ticket.Definition == null)
            {
                return false;
            }

            if (!ticket.IsBoss && !bypassCap && GetNonBossActiveEnemyCount() >= _maxActiveEnemies)
            {
                return false;
            }

            if (!TryTakeFromPool(ticket.Definition, out EnemyHealth enemy, out ObjectPool<EnemyHealth> pool))
            {
                return false;
            }

            int enemyId = enemy.GetInstanceID();
            if (_activeEnemiesById.ContainsKey(enemyId))
            {
                _activeEnemiesById.Remove(enemyId);
                Debug.LogWarning($"EnemySpawner recycled active pooled enemy instance {enemyId}.", this);
            }

            Transform enemyTransform = enemy.transform;
            enemyTransform.SetParent(_poolRoot, true);
            enemyTransform.position = ClampToArena(spawnPosition);

            enemy.Initialize(ticket.Definition, ticket.HpMultiplier, ticket.DamageMultiplier, ticket.IsElite, ticket.RunTime);
            _activeEnemiesById[enemyId] = new ActiveEnemyRecord(enemy, ticket.Definition, pool, ticket.IsBoss);
            _waveManager.RegisterSpawnedEnemy(enemyId);
            SyncWaveManagerActiveCount();
            return true;
        }

        private bool TryTakeFromPool(EnemyDefinitionSO definition, out EnemyHealth enemy, out ObjectPool<EnemyHealth> pool)
        {
            enemy = null;
            pool = null;

            if (definition == null)
            {
                return false;
            }

            if (definition.IsBoss && !string.IsNullOrWhiteSpace(definition.BossId) && _poolByBossId.TryGetValue(definition.BossId, out pool))
            {
                return pool.TryTake(out enemy);
            }

            if (_poolByEnemyType.TryGetValue(definition.Type, out pool))
            {
                return pool.TryTake(out enemy);
            }

            Debug.LogWarning($"EnemySpawner could not resolve a pool for enemy '{definition.EnemyId}'.", this);
            return false;
        }

        private EnemyDefinitionSO ResolveEnemyDefinition(EnemyType enemyType)
        {
            int index = (int)enemyType;
            if (_enemyDefinitions == null || index < 0 || index >= _enemyDefinitions.Length)
            {
                return null;
            }

            EnemyDefinitionSO definition = _enemyDefinitions[index];
            return definition != null && definition.Type == enemyType ? definition : null;
        }

        private EnemyDefinitionSO ResolveBossDefinition(string bossId)
        {
            if (string.IsNullOrWhiteSpace(bossId) || _enemyDefinitions == null)
            {
                return null;
            }

            for (int i = 0; i < _enemyDefinitions.Length; i++)
            {
                EnemyDefinitionSO definition = _enemyDefinitions[i];
                if (definition == null || !definition.IsBoss)
                {
                    continue;
                }

                if (string.Equals(definition.BossId, bossId, StringComparison.OrdinalIgnoreCase))
                {
                    return definition;
                }
            }

            return null;
        }

        private int GetSuggestedPrewarmCount(EnemyDefinitionSO definition)
        {
            if (definition == null)
            {
                return 0;
            }

            int enemyTypeCount = Mathf.Max(1, _enemyDefinitions != null ? _enemyDefinitions.Length : 1);
            return Mathf.Max(1, Mathf.CeilToInt((float)_maxActiveEnemies / enemyTypeCount));
        }

        private int GetNonBossActiveEnemyCount()
        {
            int activeCount = 0;

            foreach (KeyValuePair<int, ActiveEnemyRecord> activeEnemy in _activeEnemiesById)
            {
                if (!activeEnemy.Value.IsBoss)
                {
                    activeCount++;
                }
            }

            return activeCount;
        }

        private void SyncWaveManagerActiveCount()
        {
            _waveManager?.SetActiveEnemyCount(_activeEnemiesById.Count);
        }

        private Vector3 ResolveBossSpawnPosition()
        {
            if (_spawnBossAtArenaCenter)
            {
                return ClampToArena(_arenaBounds.center);
            }

            return ResolveSpawnPosition(0, 1, NextAngleRadians());
        }

        private Vector3 ResolveSpawnPosition(int batchIndex, int batchCount, float baseAngle)
        {
            Vector2 playerPosition = GetPlayerPosition();
            int retryCount = Mathf.Max(1, _spawnPositionRetryCount);
            Vector2 bestPosition = playerPosition;

            for (int i = 0; i < retryCount; i++)
            {
                float angleStep = batchCount > 0 ? (Mathf.PI * 2f) / batchCount : 0f;
                float angle = baseAngle + (angleStep * batchIndex) + (NextFloatSigned() * 0.35f);
                Vector2 direction = new(Mathf.Cos(angle), Mathf.Sin(angle));
                float distance = Mathf.Clamp(
                    _spawnDistanceFromPlayer + (NextFloatSigned() * _spawnDistanceVariance),
                    _minSpawnDistanceFromPlayer,
                    Mathf.Max(_minSpawnDistanceFromPlayer, _maxSpawnDistanceFromPlayer));

                bestPosition = playerPosition + (direction * distance);
                bestPosition = PushOutsideCameraBand(playerPosition, direction, bestPosition);
                bestPosition = EnforceMinDistanceFromPlayer(playerPosition, bestPosition);
                bestPosition = ClampToArena(bestPosition);

                if ((bestPosition - playerPosition).sqrMagnitude >= _minSpawnDistanceFromPlayer * _minSpawnDistanceFromPlayer)
                {
                    break;
                }
            }

            return bestPosition;
        }

        private Vector2 PushOutsideCameraBand(Vector2 playerPosition, Vector2 direction, Vector2 candidatePosition)
        {
            if (_spawnCamera == null || !_spawnCamera.orthographic)
            {
                return candidatePosition;
            }

            Rect cameraRect = GetCameraWorldRect();
            if (!cameraRect.Contains(candidatePosition))
            {
                return candidatePosition;
            }

            if (!TryGetDistanceToExitRect(playerPosition, direction, cameraRect, out float exitDistance))
            {
                return candidatePosition;
            }

            float desiredDistance = Mathf.Max(_minSpawnDistanceFromPlayer, exitDistance + _screenEdgePadding);
            desiredDistance = Mathf.Min(desiredDistance, Mathf.Max(_minSpawnDistanceFromPlayer, _maxSpawnDistanceFromPlayer));
            return playerPosition + (direction * desiredDistance);
        }

        private Rect GetCameraWorldRect()
        {
            float halfHeight = _spawnCamera.orthographicSize;
            float halfWidth = halfHeight * _spawnCamera.aspect;
            Vector3 cameraPosition = _spawnCamera.transform.position;
            return Rect.MinMaxRect(
                cameraPosition.x - halfWidth,
                cameraPosition.y - halfHeight,
                cameraPosition.x + halfWidth,
                cameraPosition.y + halfHeight);
        }

        private static bool TryGetDistanceToExitRect(Vector2 origin, Vector2 direction, Rect rect, out float distance)
        {
            distance = float.PositiveInfinity;

            TryAccumulateExitDistance(origin, direction, rect.xMin, true, rect, ref distance);
            TryAccumulateExitDistance(origin, direction, rect.xMax, true, rect, ref distance);
            TryAccumulateExitDistance(origin, direction, rect.yMin, false, rect, ref distance);
            TryAccumulateExitDistance(origin, direction, rect.yMax, false, rect, ref distance);

            return !float.IsPositiveInfinity(distance);
        }

        private static void TryAccumulateExitDistance(Vector2 origin, Vector2 direction, float boundary, bool isVerticalBoundary, Rect rect, ref float bestDistance)
        {
            float axisDelta = isVerticalBoundary ? direction.x : direction.y;
            if (Mathf.Abs(axisDelta) <= Mathf.Epsilon)
            {
                return;
            }

            float originAxis = isVerticalBoundary ? origin.x : origin.y;
            float travelDistance = (boundary - originAxis) / axisDelta;
            if (travelDistance <= 0f || travelDistance >= bestDistance)
            {
                return;
            }

            float otherAxis = isVerticalBoundary
                ? origin.y + (direction.y * travelDistance)
                : origin.x + (direction.x * travelDistance);
            bool intersects = isVerticalBoundary
                ? otherAxis >= rect.yMin && otherAxis <= rect.yMax
                : otherAxis >= rect.xMin && otherAxis <= rect.xMax;

            if (intersects)
            {
                bestDistance = travelDistance;
            }
        }

        private Vector2 EnforceMinDistanceFromPlayer(Vector2 playerPosition, Vector2 candidatePosition)
        {
            Vector2 offset = candidatePosition - playerPosition;
            float minDistance = Mathf.Max(0.01f, _minSpawnDistanceFromPlayer);
            if (offset.sqrMagnitude >= minDistance * minDistance)
            {
                return candidatePosition;
            }

            Vector2 direction = offset.sqrMagnitude > Mathf.Epsilon ? offset.normalized : GetRandomDirection();
            return playerPosition + (direction * minDistance);
        }

        private Vector2 GetPlayerPosition()
        {
            if (_hasPlayerPosition)
            {
                return _lastKnownPlayerPosition;
            }

            return _runManager != null ? (Vector2)_runManager.transform.position : Vector2.zero;
        }

        private Vector3 ClampToArena(Vector3 position)
        {
            Vector2 clamped = ClampToArena((Vector2)position);
            return new Vector3(clamped.x, clamped.y, position.z);
        }

        private Vector3 ClampToArena(Vector2 position)
        {
            if (_arenaBounds.width <= 0f || _arenaBounds.height <= 0f)
            {
                return position;
            }

            return new Vector2(
                Mathf.Clamp(position.x, _arenaBounds.xMin, _arenaBounds.xMax),
                Mathf.Clamp(position.y, _arenaBounds.yMin, _arenaBounds.yMax));
        }

        private float NextFloat01()
        {
            return (float)_random.NextDouble();
        }

        private float NextFloatSigned()
        {
            return (NextFloat01() * 2f) - 1f;
        }

        private float NextAngleRadians()
        {
            return NextFloat01() * Mathf.PI * 2f;
        }

        private Vector2 GetRandomDirection()
        {
            float angle = NextAngleRadians();
            return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        }
    }
}
