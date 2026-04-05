using System;
using KokTengri.Core;
using UnityEngine;

namespace KokTengri.Gameplay
{
    [Serializable]
    public struct RunAfkStateChangedEvent
    {
        public RunAfkStateChangedEvent(bool isAfk, float runTime)
        {
            IsAfk = isAfk;
            RunTime = runTime;
        }

        public bool IsAfk;
        public float RunTime;
    }

    [DisallowMultipleComponent]
    public sealed class RunManager : MonoBehaviour
    {
        private static readonly System.Random SeedRandom = new();

        [SerializeField] private RunManagerConfigSO _runManagerConfig;
        [SerializeField] private EconomyConfigSO _economyConfig;

        private static int _nextRunId = 1;

        private RunLifecycleState _state = RunLifecycleState.Uninitialized;
        private float _elapsedSeconds;
        private float _tickAccumulator;
        private float _lastInputTime;
        private float _lastPauseResumeToggleTime = float.NegativeInfinity;
        private int _runId;
        private int _seed;
        private int _killCount;
        private int _bossesDefeated;
        private bool _heroModeActive;
        private bool _isAfk;
        private bool _hasPublishedRunEnd;
        private string _heroId = string.Empty;
        private string _classId = string.Empty;

        public RunLifecycleState State => _state;

        public float ElapsedSeconds => _elapsedSeconds;

        public float ElapsedMinutes => _elapsedSeconds / 60f;

        public int RunId => _runId;

        public int Seed => _seed;

        public int KillCount => _killCount;

        public int BossesDefeated => _bossesDefeated;

        public bool IsHeroModeActive => _heroModeActive;

        public bool IsAfk => _isAfk;

        public float FinalGoldReward { get; private set; }

        private void OnEnable()
        {
            EventBus.Subscribe<EnemyDeathEvent>(HandleEnemyDeath);
            EventBus.Subscribe<BossDefeatedEvent>(HandleBossDefeated);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<EnemyDeathEvent>(HandleEnemyDeath);
            EventBus.Unsubscribe<BossDefeatedEvent>(HandleBossDefeated);
        }

        private void Update()
        {
            if (_state != RunLifecycleState.Active)
            {
                return;
            }

            if (!HasRequiredConfig())
            {
                return;
            }

            var deltaTime = Time.unscaledDeltaTime;
            _elapsedSeconds += deltaTime;
            _tickAccumulator += deltaTime;

            PublishTimerTicksIfDue();
            UpdateAfkState();

            if (_runManagerConfig.VictoryAtTimerCapEnabled && !_heroModeActive && _elapsedSeconds >= _runManagerConfig.RunDurationSeconds)
            {
                EndRun(RunEndResultType.Victory);
            }
        }

        /// <summary>
        /// Starts a new run and publishes the run bootstrap event.
        /// </summary>
        /// <param name="heroId">The selected hero identifier.</param>
        /// <param name="classId">The selected class identifier.</param>
        /// <param name="seedOverride">Optional deterministic seed override.</param>
        public void StartRun(string heroId, string classId, int? seedOverride = null)
        {
            if (!HasRequiredConfig())
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(heroId))
            {
                Debug.LogWarning("RunManager rejected StartRun because heroId was null or empty.", this);
                return;
            }

            if (string.IsNullOrWhiteSpace(classId))
            {
                Debug.LogWarning("RunManager rejected StartRun because classId was null or empty.", this);
                return;
            }

            if (!TryTransitionTo(RunLifecycleState.Starting))
            {
                return;
            }

            ResetRunState();

            _heroId = heroId;
            _classId = classId;
            _runId = _nextRunId++;
            _seed = seedOverride ?? SeedRandom.Next(int.MinValue, int.MaxValue);
            _lastInputTime = Time.unscaledTime;

            if (!TryTransitionTo(RunLifecycleState.Active))
            {
                return;
            }

            EventBus.Publish(new RunStartEvent(_runId, _heroId, _classId, _seed));
        }

        /// <summary>
        /// Pauses the current run if the lifecycle state allows it.
        /// </summary>
        public void PauseRun()
        {
            if (!CanTogglePauseResume())
            {
                return;
            }

            if (!TryTransitionTo(RunLifecycleState.Paused))
            {
                return;
            }

            _lastPauseResumeToggleTime = Time.unscaledTime;
            EventBus.Publish(new RunPauseEvent(true, _elapsedSeconds));
        }

        /// <summary>
        /// Resumes the current run if the lifecycle state allows it.
        /// </summary>
        public void ResumeRun()
        {
            if (!CanTogglePauseResume())
            {
                return;
            }

            if (!TryTransitionTo(RunLifecycleState.Active))
            {
                return;
            }

            _lastPauseResumeToggleTime = Time.unscaledTime;
            _lastInputTime = Time.unscaledTime;
            EventBus.Publish(new RunPauseEvent(false, _elapsedSeconds));
        }

        /// <summary>
        /// Marks player input activity so AFK tracking stays in sync with live input.
        /// </summary>
        public void NotifyInputActivity()
        {
            _lastInputTime = Time.unscaledTime;

            if (!_isAfk)
            {
                return;
            }

            _isAfk = false;
            EventBus.Publish(new RunAfkStateChangedEvent(false, _elapsedSeconds));
        }

        /// <summary>
        /// Activates Hero Mode so the run can continue beyond the timer cap.
        /// </summary>
        public void ActivateHeroMode()
        {
            if (_state != RunLifecycleState.Active && _state != RunLifecycleState.Paused)
            {
                Debug.LogWarning($"RunManager ignored Hero Mode activation while in invalid state {_state}.", this);
                return;
            }

            if (_heroModeActive)
            {
                return;
            }

            _heroModeActive = true;
            EventBus.Publish(new HeroModeActivatedEvent(_elapsedSeconds));
        }

        /// <summary>
        /// Ends the current run, publishes the final run event once, and resets the EventBus.
        /// </summary>
        /// <param name="result">The terminal outcome for the run.</param>
        public void EndRun(RunEndResultType result)
        {
            if (_hasPublishedRunEnd)
            {
                return;
            }

            if (_state != RunLifecycleState.Active && _state != RunLifecycleState.Paused)
            {
                Debug.LogWarning($"RunManager ignored EndRun({result}) while in invalid state {_state}.", this);
                return;
            }

            if (!TryTransitionTo(RunLifecycleState.Ending))
            {
                return;
            }

            FinalGoldReward = CalculateGoldReward();
            _hasPublishedRunEnd = true;

            if (!TryTransitionTo(RunLifecycleState.Ended))
            {
                return;
            }

            EventBus.Publish(new RunEndEvent(_runId, result, _elapsedSeconds, _killCount, _bossesDefeated));
            EventBus.Reset();
        }

        private bool HasRequiredConfig()
        {
            if (_runManagerConfig == null)
            {
                Debug.LogWarning("RunManager requires a RunManagerConfigSO reference.", this);
                return false;
            }

            if (_economyConfig == null)
            {
                Debug.LogWarning("RunManager requires an EconomyConfigSO reference.", this);
                return false;
            }

            return true;
        }

        private void ResetRunState()
        {
            _elapsedSeconds = 0f;
            _tickAccumulator = 0f;
            _killCount = 0;
            _bossesDefeated = 0;
            _heroModeActive = false;
            _isAfk = false;
            _hasPublishedRunEnd = false;
            FinalGoldReward = 0f;
            _lastPauseResumeToggleTime = float.NegativeInfinity;
        }

        private bool TryTransitionTo(RunLifecycleState nextState)
        {
            if (!IsValidTransition(_state, nextState))
            {
                Debug.LogWarning($"RunManager rejected invalid transition from {_state} to {nextState}.", this);
                return false;
            }

            _state = nextState;
            return true;
        }

        private static bool IsValidTransition(RunLifecycleState currentState, RunLifecycleState nextState)
        {
            return currentState switch
            {
                RunLifecycleState.Uninitialized => nextState == RunLifecycleState.Starting,
                RunLifecycleState.Starting => nextState == RunLifecycleState.Active,
                RunLifecycleState.Active => nextState == RunLifecycleState.Paused || nextState == RunLifecycleState.Ending,
                RunLifecycleState.Paused => nextState == RunLifecycleState.Active || nextState == RunLifecycleState.Ending,
                RunLifecycleState.Ending => nextState == RunLifecycleState.Ended,
                RunLifecycleState.Ended => false,
                _ => false,
            };
        }

        private bool CanTogglePauseResume()
        {
            if (!HasRequiredConfig())
            {
                return false;
            }

            if (Time.unscaledTime - _lastPauseResumeToggleTime < _runManagerConfig.PauseResumeDebounceSeconds)
            {
                return false;
            }

            return true;
        }

        private void PublishTimerTicksIfDue()
        {
            while (_tickAccumulator >= _runManagerConfig.TimeBroadcastIntervalSeconds)
            {
                _tickAccumulator -= _runManagerConfig.TimeBroadcastIntervalSeconds;
                EventBus.Publish(new RunTimerTickEvent(_elapsedSeconds));
            }
        }

        private void UpdateAfkState()
        {
            var shouldBeAfk = Time.unscaledTime - _lastInputTime >= _runManagerConfig.AfkThresholdSeconds;

            if (shouldBeAfk == _isAfk)
            {
                return;
            }

            _isAfk = shouldBeAfk;
            EventBus.Publish(new RunAfkStateChangedEvent(_isAfk, _elapsedSeconds));
        }

        private float CalculateGoldReward()
        {
            var goldBase = (ElapsedMinutes * _economyConfig.GoldPerSurvivedMinute)
                + (_killCount * _economyConfig.GoldPerKill)
                + (_bossesDefeated * _economyConfig.GoldPerBoss);

            return _heroModeActive
                ? goldBase * _runManagerConfig.HeroModeGoldMultiplier
                : goldBase;
        }

        private void HandleEnemyDeath(EnemyDeathEvent eventData)
        {
            if (_state != RunLifecycleState.Active)
            {
                return;
            }

            _killCount++;
        }

        private void HandleBossDefeated(BossDefeatedEvent eventData)
        {
            if (_state != RunLifecycleState.Active)
            {
                return;
            }

            _bossesDefeated++;
        }
    }
}
