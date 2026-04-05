using KokTengri.Core;
using UnityEngine;

namespace KokTengri.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class XPLeveling : MonoBehaviour
    {
        [SerializeField] private XPConfigSO _xpConfig;
        [SerializeField] private EnemyDefinitionSO[] _enemyDefinitions;
        [SerializeField] private DifficultyConfigSO _difficultyConfig;

        private float _currentXp;
        private float _totalXpAccumulated;
        private float _runTimeSeconds;
        private int _currentLevel = 1;
        private int _levelUpsAvailable;
        private bool _isRunActive;

        public int CurrentLevel => _currentLevel;

        public float CurrentXp => _currentXp;

        public float XpToNextLevel => GetXpToNextLevel();

        public float TotalXpAccumulated => _totalXpAccumulated;

        public int LevelUpsAvailable => _levelUpsAvailable;

        private void OnEnable()
        {
            EventBus.Subscribe<EnemyDeathEvent>(HandleEnemyDeath);
            EventBus.Subscribe<RunStartEvent>(HandleRunStart);
            EventBus.Subscribe<RunEndEvent>(HandleRunEnd);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<EnemyDeathEvent>(HandleEnemyDeath);
            EventBus.Unsubscribe<RunStartEvent>(HandleRunStart);
            EventBus.Unsubscribe<RunEndEvent>(HandleRunEnd);
        }

        private void Update()
        {
            if (!_isRunActive)
            {
                return;
            }

            _runTimeSeconds += Time.unscaledDeltaTime;
        }

        private void HandleRunStart(RunStartEvent runStartEvent)
        {
            ResetProgressionState();
            _isRunActive = true;
        }

        private void HandleRunEnd(RunEndEvent runEndEvent)
        {
            _isRunActive = false;
        }

        private void HandleEnemyDeath(EnemyDeathEvent enemyDeathEvent)
        {
            if (!_isRunActive || _xpConfig == null)
            {
                return;
            }

            int baseXpReward = GetBaseXpReward(enemyDeathEvent.EnemyType);
            if (baseXpReward <= 0)
            {
                return;
            }

            float xpMultiplier = ResolveXpMultiplier(enemyDeathEvent.IsElite);
            float xpReward = baseXpReward * xpMultiplier;
            if (xpReward <= 0f)
            {
                return;
            }

            _runTimeSeconds = Mathf.Max(_runTimeSeconds, enemyDeathEvent.RunTime);
            AddXp(xpReward, enemyDeathEvent.RunTime);
        }

        private void AddXp(float amount, float runTime)
        {
            if (amount <= 0f)
            {
                return;
            }

            _currentXp += amount;
            _totalXpAccumulated += amount;

            ProcessLevelUps(runTime);
        }

        private void ProcessLevelUps(float runTime)
        {
            while (CanLevelUp())
            {
                float xpRequired = GetXpToNextLevel();
                if (xpRequired <= 0f)
                {
                    break;
                }

                _currentXp -= xpRequired;
                _currentLevel++;
                _levelUpsAvailable++;

                float levelUpRunTime = Mathf.Max(runTime, _runTimeSeconds);
                EventBus.Publish(new LevelUpEvent(_currentLevel, _currentXp, levelUpRunTime));
            }
        }

        private bool CanLevelUp()
        {
            if (_xpConfig == null)
            {
                return false;
            }

            if (_currentLevel >= _xpConfig.MaxLevel)
            {
                return false;
            }

            return _currentXp >= GetXpToNextLevel();
        }

        private float GetXpToNextLevel()
        {
            if (_xpConfig == null || _currentLevel >= _xpConfig.MaxLevel)
            {
                return 0f;
            }

            return Mathf.Max(0f, _xpConfig.GetXpForLevel(_currentLevel + 1));
        }

        private int GetBaseXpReward(EnemyType enemyType)
        {
            if (_enemyDefinitions == null)
            {
                return 0;
            }

            for (int i = 0; i < _enemyDefinitions.Length; i++)
            {
                EnemyDefinitionSO definition = _enemyDefinitions[i];
                if (definition == null || definition.Type != enemyType)
                {
                    continue;
                }

                if (definition.IsClone)
                {
                    return 0;
                }

                return Mathf.Max(0, definition.XpReward);
            }

            return 0;
        }

        private float ResolveXpMultiplier(bool isElite)
        {
            if (!isElite)
            {
                return 1f;
            }

            if (_difficultyConfig != null)
            {
                return DifficultyScaling.GetFinalXpMultiplier(true, _difficultyConfig);
            }

            if (_xpConfig == null)
            {
                return 1f;
            }

            return Mathf.Max(1f, _xpConfig.EliteXpMultiplier);
        }

        private void ResetProgressionState()
        {
            _currentLevel = 1;
            _currentXp = 0f;
            _totalXpAccumulated = 0f;
            _levelUpsAvailable = 0;
            _runTimeSeconds = 0f;
        }
    }
}
