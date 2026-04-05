using System;
using System.Collections.Generic;
using KokTengri.Core;
using UnityEngine;

namespace KokTengri.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class WaveManager : MonoBehaviour
    {
        public struct SpawnPlan
        {
            public SpawnPlan(float currentSpawnRate, List<EnemyType> allowedEnemyTypes, float eliteChance, bool isSpawnAllowed, bool isBossActive)
            {
                CurrentSpawnRate = currentSpawnRate;
                AllowedEnemyTypes = allowedEnemyTypes;
                EliteChance = eliteChance;
                IsSpawnAllowed = isSpawnAllowed;
                IsBossActive = isBossActive;
            }

            public float CurrentSpawnRate;
            public List<EnemyType> AllowedEnemyTypes;
            public float EliteChance;
            public bool IsSpawnAllowed;
            public bool IsBossActive;
        }

        [SerializeField] private WaveManagerConfigSO _config;
        [SerializeField] private RunManager _runManager;

        private readonly List<EnemyType> _allowedEnemyTypes = new();
        private readonly Dictionary<int, int> _enemySegmentById = new();
        private readonly Dictionary<int, int> _activeEnemyCountBySegment = new();
        private readonly Queue<int> _pendingWaveCompletions = new();
        private readonly HashSet<int> _processedBossScheduleIndices = new();

        private bool[] _unlockProcessedFlags = Array.Empty<bool>();
        private WaveState _state = WaveState.Inactive;
        private float _elapsedSeconds;
        private float _difficultyMultiplier = 1f;
        private float _spawnCreditAccumulator;
        private float _enemyHpMultiplier = 1f;
        private bool _isPaused;
        private bool _heroModeActive;
        private bool _bossSpawnPending;
        private bool _bossActive;
        private int _activeEnemyCount;
        private int _nextSegmentToQueue;
        private int _pendingBossScheduleIndex = -1;
        private string _pendingBossId = string.Empty;

        public WaveState State => _state;

        public float ElapsedSeconds => _elapsedSeconds;

        public float ElapsedMinutes => _elapsedSeconds / 60f;

        public int ActiveEnemyCount => _activeEnemyCount;

        public float CurrentEnemyHpMultiplier => _enemyHpMultiplier;

        public int CurrentWaveIndex => Mathf.FloorToInt(_elapsedSeconds / Mathf.Max(1f, _config != null ? _config.WaveSegmentDurationSeconds : 1f));

        private void OnEnable()
        {
            EventBus.Subscribe<RunStartEvent>(HandleRunStart);
            EventBus.Subscribe<RunEndEvent>(HandleRunEnd);
            EventBus.Subscribe<RunPauseEvent>(HandleRunPause);
            EventBus.Subscribe<HeroModeActivatedEvent>(HandleHeroModeActivated);
            EventBus.Subscribe<EnemyDeathEvent>(HandleEnemyDeath);
            EventBus.Subscribe<BossDefeatedEvent>(HandleBossDefeated);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<RunStartEvent>(HandleRunStart);
            EventBus.Unsubscribe<RunEndEvent>(HandleRunEnd);
            EventBus.Unsubscribe<RunPauseEvent>(HandleRunPause);
            EventBus.Unsubscribe<HeroModeActivatedEvent>(HandleHeroModeActivated);
            EventBus.Unsubscribe<EnemyDeathEvent>(HandleEnemyDeath);
            EventBus.Unsubscribe<BossDefeatedEvent>(HandleBossDefeated);
        }

        private void Update()
        {
            if (_state == WaveState.Inactive || _isPaused)
            {
                return;
            }

            if (!HasRequiredReferences())
            {
                return;
            }

            var deltaTime = Time.unscaledDeltaTime;
            _elapsedSeconds += deltaTime;

            UpdateUnlockSchedule();
            UpdateSpawnAccumulator(deltaTime);
            QueueElapsedWaveSegments();
            UpdateBossSchedule();
            ProcessPendingWaveCompletions();
        }

        /// <summary>
        /// Returns the current pacing plan consumed by Enemy Spawner.
        /// </summary>
        public SpawnPlan GetCurrentSpawnPlan()
        {
            return new SpawnPlan(
                GetCurrentSpawnRate(),
                _allowedEnemyTypes,
                GetCurrentEliteChance(),
                IsRegularSpawnAllowed(),
                _bossActive);
        }

        /// <summary>
        /// Applies the latest difficulty multiplier from the difficulty scaling system.
        /// </summary>
        public void SetDifficultyMultiplier(float difficultyMultiplier)
        {
            _difficultyMultiplier = Mathf.Max(0f, difficultyMultiplier);
        }

        /// <summary>
        /// Registers a spawned enemy to active-count and wave-segment tracking.
        /// </summary>
        public void RegisterSpawnedEnemy(int enemyId)
        {
            if (_state == WaveState.Inactive)
            {
                return;
            }

            if (_enemySegmentById.ContainsKey(enemyId))
            {
                return;
            }

            var segmentIndex = CurrentWaveIndex;
            _enemySegmentById.Add(enemyId, segmentIndex);

            if (_activeEnemyCountBySegment.TryGetValue(segmentIndex, out var activeSegmentCount))
            {
                _activeEnemyCountBySegment[segmentIndex] = activeSegmentCount + 1;
            }
            else
            {
                _activeEnemyCountBySegment.Add(segmentIndex, 1);
            }

            _activeEnemyCount++;
        }

        /// <summary>
        /// Updates the active enemy count from an external spawner snapshot.
        /// </summary>
        public void SetActiveEnemyCount(int activeEnemyCount)
        {
            _activeEnemyCount = Mathf.Max(0, activeEnemyCount);
        }

        /// <summary>
        /// Returns and consumes accumulated regular spawn credits.
        /// </summary>
        public int ConsumeSpawnBudget()
        {
            if (!IsRegularSpawnAllowed())
            {
                return 0;
            }

            var spawnBudget = Mathf.FloorToInt(_spawnCreditAccumulator);
            _spawnCreditAccumulator = Mathf.Max(0f, _spawnCreditAccumulator - spawnBudget);
            return spawnBudget;
        }

        /// <summary>
        /// Returns true while a configured boss spawn slot is pending confirmation.
        /// </summary>
        public bool IsBossSpawnPending()
        {
            return _bossSpawnPending;
        }

        /// <summary>
        /// Returns the current pending boss identifier, or empty when no boss is due.
        /// </summary>
        public string GetPendingBossId()
        {
            return _pendingBossId;
        }

        /// <summary>
        /// Returns seconds until the next configured boss schedule entry.
        /// </summary>
        public float GetSecondsUntilNextBoss()
        {
            if (_config == null || _config.BossSchedule == null)
            {
                return -1f;
            }

            var elapsedMinutes = Mathf.Max(0f, ElapsedMinutes);

            for (var i = 0; i < _config.BossSchedule.Length; i++)
            {
                if (_processedBossScheduleIndices.Contains(i))
                {
                    continue;
                }

                var entry = _config.BossSchedule[i];

                if (entry.TriggerTimeMinutes < elapsedMinutes)
                {
                    continue;
                }

                return Mathf.Max(0f, (entry.TriggerTimeMinutes - elapsedMinutes) * 60f);
            }

            return -1f;
        }

        /// <summary>
        /// Confirms that the pending boss was successfully spawned and publishes the spawn event.
        /// </summary>
        public void ConfirmBossSpawn(Vector3 spawnPosition)
        {
            if (!_bossSpawnPending || _pendingBossScheduleIndex < 0 || string.IsNullOrWhiteSpace(_pendingBossId))
            {
                return;
            }

            _bossSpawnPending = false;
            _bossActive = true;
            _state = WaveState.BossEncounter;
            _processedBossScheduleIndices.Add(_pendingBossScheduleIndex);
            EventBus.Publish(new BossSpawnedEvent(_pendingBossId, spawnPosition, _elapsedSeconds));
            _pendingBossScheduleIndex = -1;
            _pendingBossId = string.Empty;
        }

        /// <summary>
        /// Clears all runtime pacing state.
        /// </summary>
        public void ResetState()
        {
            _state = WaveState.Inactive;
            _elapsedSeconds = 0f;
            _difficultyMultiplier = 1f;
            _spawnCreditAccumulator = 0f;
            _enemyHpMultiplier = 1f;
            _isPaused = false;
            _heroModeActive = false;
            _bossSpawnPending = false;
            _bossActive = false;
            _activeEnemyCount = 0;
            _nextSegmentToQueue = 0;
            _pendingBossScheduleIndex = -1;
            _pendingBossId = string.Empty;
            _allowedEnemyTypes.Clear();
            _enemySegmentById.Clear();
            _activeEnemyCountBySegment.Clear();
            _pendingWaveCompletions.Clear();
            _processedBossScheduleIndices.Clear();
            _unlockProcessedFlags = _config != null && _config.EnemyUnlockSchedule != null
                ? new bool[_config.EnemyUnlockSchedule.Length]
                : Array.Empty<bool>();
        }

        private bool HasRequiredReferences()
        {
            if (_config == null)
            {
                Debug.LogWarning("WaveManager requires a WaveManagerConfigSO reference.", this);
                return false;
            }

            if (_runManager == null)
            {
                Debug.LogWarning("WaveManager requires a RunManager reference.", this);
                return false;
            }

            return true;
        }

        private float GetCurrentSpawnRate()
        {
            if (_config == null)
            {
                return 0f;
            }

            var elapsedMinutes = Mathf.Max(0f, ElapsedMinutes);
            var heroModeSpawnMultiplier = _heroModeActive ? _config.HeroModeSpawnMultiplier : 1f;
            return _config.BaseSpawnRateEnemiesPerSecond
                * (1f + (_config.SpawnRateIncreasePerMinute * elapsedMinutes))
                * _difficultyMultiplier
                * heroModeSpawnMultiplier;
        }

        private float GetCurrentEliteChance()
        {
            if (_config == null)
            {
                return 0f;
            }

            return ElapsedMinutes >= _config.EliteStartMinute
                ? _config.EliteSpawnProbability
                : 0f;
        }

        private bool IsRegularSpawnAllowed()
        {
            if (_config == null)
            {
                return false;
            }

            return _state != WaveState.Inactive && _activeEnemyCount < _config.MaxActiveEnemies;
        }

        private void UpdateUnlockSchedule()
        {
            if (_config.EnemyUnlockSchedule == null)
            {
                return;
            }

            for (var i = 0; i < _config.EnemyUnlockSchedule.Length; i++)
            {
                if (_unlockProcessedFlags.Length <= i || _unlockProcessedFlags[i])
                {
                    continue;
                }

                var unlockEntry = _config.EnemyUnlockSchedule[i];

                if (ElapsedMinutes < unlockEntry.UnlockTimeMinutes)
                {
                    continue;
                }

                _unlockProcessedFlags[i] = true;

                if (!_allowedEnemyTypes.Contains(unlockEntry.Type))
                {
                    _allowedEnemyTypes.Add(unlockEntry.Type);
                }
            }
        }

        private void UpdateSpawnAccumulator(float deltaTime)
        {
            if (!IsRegularSpawnAllowed())
            {
                return;
            }

            _spawnCreditAccumulator += GetCurrentSpawnRate() * deltaTime;
        }

        private void QueueElapsedWaveSegments()
        {
            var segmentDurationSeconds = Mathf.Max(0.01f, _config.WaveSegmentDurationSeconds);

            while (_elapsedSeconds >= (_nextSegmentToQueue + 1) * segmentDurationSeconds)
            {
                _pendingWaveCompletions.Enqueue(_nextSegmentToQueue);
                _nextSegmentToQueue++;
            }
        }

        private void ProcessPendingWaveCompletions()
        {
            while (_pendingWaveCompletions.Count > 0)
            {
                var waveIndex = _pendingWaveCompletions.Peek();

                if (_activeEnemyCountBySegment.TryGetValue(waveIndex, out var remainingEnemies) && remainingEnemies > 0)
                {
                    return;
                }

                _pendingWaveCompletions.Dequeue();
                _activeEnemyCountBySegment.Remove(waveIndex);
                EventBus.Publish(new WaveCompletedEvent(waveIndex, _activeEnemyCount, _elapsedSeconds));
            }
        }

        private void UpdateBossSchedule()
        {
            if (_config.BossSchedule == null || _bossActive)
            {
                return;
            }

            if (_bossSpawnPending)
            {
                var pendingEntry = _config.BossSchedule[_pendingBossScheduleIndex];
                var deadlineSeconds = pendingEntry.TriggerTimeMinutes * 60f + _config.BossSpawnRetryWindowSeconds;

                if (_elapsedSeconds <= deadlineSeconds)
                {
                    return;
                }

                Debug.LogWarning($"WaveManager boss spawn window expired for '{_pendingBossId}' at {pendingEntry.TriggerTimeMinutes:0.##} minutes.", this);
                _processedBossScheduleIndices.Add(_pendingBossScheduleIndex);
                _bossSpawnPending = false;
                _pendingBossScheduleIndex = -1;
                _pendingBossId = string.Empty;
            }

            for (var i = 0; i < _config.BossSchedule.Length; i++)
            {
                if (_processedBossScheduleIndices.Contains(i))
                {
                    continue;
                }

                var entry = _config.BossSchedule[i];
                var triggerTimeSeconds = entry.TriggerTimeMinutes * 60f;
                var triggerMinuteRatio = _config.BossIntervalMinutes > 0 ? entry.TriggerTimeMinutes / _config.BossIntervalMinutes : 0f;

                if (!Mathf.Approximately(triggerMinuteRatio, Mathf.Round(triggerMinuteRatio)))
                {
                    continue;
                }

                if (_elapsedSeconds < triggerTimeSeconds)
                {
                    return;
                }

                if (_elapsedSeconds > triggerTimeSeconds + _config.BossSpawnRetryWindowSeconds)
                {
                    Debug.LogWarning($"WaveManager missed configured boss spawn window for '{entry.BossId}' at {entry.TriggerTimeMinutes:0.##} minutes.", this);
                    _processedBossScheduleIndices.Add(i);
                    continue;
                }

                _bossSpawnPending = true;
                _pendingBossScheduleIndex = i;
                _pendingBossId = entry.BossId;
                return;
            }
        }

        private void HandleRunStart(RunStartEvent eventData)
        {
            if (!HasRequiredReferences())
            {
                return;
            }

            ResetState();
            _state = WaveState.Spawning;
            _heroModeActive = _runManager.IsHeroModeActive;
            _enemyHpMultiplier = _heroModeActive ? _config.HeroModeEnemyHpMultiplier : 1f;
            UpdateUnlockSchedule();
        }

        private void HandleRunEnd(RunEndEvent eventData)
        {
            ResetState();
        }

        private void HandleRunPause(RunPauseEvent eventData)
        {
            _isPaused = eventData.IsPaused;
        }

        private void HandleHeroModeActivated(HeroModeActivatedEvent eventData)
        {
            if (_state == WaveState.Inactive || _config == null)
            {
                return;
            }

            _heroModeActive = true;
            _enemyHpMultiplier = _config.HeroModeEnemyHpMultiplier;
        }

        private void HandleEnemyDeath(EnemyDeathEvent eventData)
        {
            if (!_enemySegmentById.TryGetValue(eventData.EnemyId, out var segmentIndex))
            {
                _activeEnemyCount = Mathf.Max(0, _activeEnemyCount - 1);
                ProcessPendingWaveCompletions();
                return;
            }

            _enemySegmentById.Remove(eventData.EnemyId);
            _activeEnemyCount = Mathf.Max(0, _activeEnemyCount - 1);

            if (_activeEnemyCountBySegment.TryGetValue(segmentIndex, out var activeSegmentCount))
            {
                activeSegmentCount = Mathf.Max(0, activeSegmentCount - 1);

                if (activeSegmentCount == 0)
                {
                    _activeEnemyCountBySegment.Remove(segmentIndex);
                }
                else
                {
                    _activeEnemyCountBySegment[segmentIndex] = activeSegmentCount;
                }
            }

            ProcessPendingWaveCompletions();
        }

        private void HandleBossDefeated(BossDefeatedEvent eventData)
        {
            if (_state == WaveState.Inactive)
            {
                return;
            }

            _bossActive = false;

            if (_state != WaveState.Inactive)
            {
                _state = WaveState.Spawning;
            }
        }
    }
}
