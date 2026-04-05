using System;
using KokTengri.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KokTengri.UI
{
    public enum HUDState
    {
        Hidden = 0,
        Active = 1,
        DimmedForLevelUp = 2,
        BossOverlayActive = 3,
        Paused = 4,
        PostRun = 5,
    }

    /// <summary>
    /// Display-only HUD presenter that listens to gameplay events and mirrors their state to UI widgets.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HUDController : MonoBehaviour
    {
        private const int ElementSlotCount = 3;
        private const int SpellSlotCount = 6;

        [SerializeField] private HUDConfigSO _config;
        [SerializeField] private CanvasGroup _rootCanvasGroup;
        [SerializeField] private Image _hpBarFill;
        [SerializeField] private Image _xpBarFill;
        [SerializeField] private TextMeshProUGUI _levelLabel;
        [SerializeField] private TextMeshProUGUI _timerLabel;
        [SerializeField] private Image[] _elementSlots = new Image[ElementSlotCount];
        [SerializeField] private SpellSlotWidget[] _spellSlots = new SpellSlotWidget[SpellSlotCount];
        [SerializeField] private GameObject _bossHpBar;
        [SerializeField] private Image _bossHpBarFill;
        [SerializeField] private Image _damageFlashOverlay;

        private readonly ElementType?[] _elements = new ElementType?[ElementSlotCount];
        private readonly SpellSlotData[] _spells = new SpellSlotData[SpellSlotCount];

        private HUDState _state = HUDState.Hidden;
        private int _currentHp;
        private int _maxHp;
        private float _currentXp;
        private float _xpToNextLevel;
        private int _currentLevel = 1;
        private float _elapsedSeconds;
        private float _displayHp;
        private float _targetHp;
        private float _displayXp;
        private float _targetXp;

        private void Awake()
        {
            if (_rootCanvasGroup == null)
            {
                TryGetComponent(out _rootCanvasGroup);
            }

            ResetStoredState();
            ApplyStatePresentation();
            RefreshAllWidgetsImmediate();
        }

        private void OnEnable()
        {
            EventBus.Subscribe<RunStartEvent>(HandleRunStart);
            EventBus.Subscribe<RunEndEvent>(HandleRunEnd);
            EventBus.Subscribe<RunPauseEvent>(HandleRunPause);
            EventBus.Subscribe<RunTimerTickEvent>(HandleRunTimerTick);
            EventBus.Subscribe<PlayerDamagedEvent>(HandlePlayerDamaged);
            EventBus.Subscribe<LevelUpEvent>(HandleLevelUp);
            EventBus.Subscribe<XPCollectedEvent>(HandleXpCollected);
            EventBus.Subscribe<ElementAddedEvent>(HandleElementAdded);
            EventBus.Subscribe<ElementRemovedEvent>(HandleElementRemoved);
            EventBus.Subscribe<SpellCraftedEvent>(HandleSpellCrafted);
            EventBus.Subscribe<SpellUpgradedEvent>(HandleSpellUpgraded);
            EventBus.Subscribe<BossSpawnedEvent>(HandleBossSpawned);
            EventBus.Subscribe<BossDefeatedEvent>(HandleBossDefeated);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<RunStartEvent>(HandleRunStart);
            EventBus.Unsubscribe<RunEndEvent>(HandleRunEnd);
            EventBus.Unsubscribe<RunPauseEvent>(HandleRunPause);
            EventBus.Unsubscribe<RunTimerTickEvent>(HandleRunTimerTick);
            EventBus.Unsubscribe<PlayerDamagedEvent>(HandlePlayerDamaged);
            EventBus.Unsubscribe<LevelUpEvent>(HandleLevelUp);
            EventBus.Unsubscribe<XPCollectedEvent>(HandleXpCollected);
            EventBus.Unsubscribe<ElementAddedEvent>(HandleElementAdded);
            EventBus.Unsubscribe<ElementRemovedEvent>(HandleElementRemoved);
            EventBus.Unsubscribe<SpellCraftedEvent>(HandleSpellCrafted);
            EventBus.Unsubscribe<SpellUpgradedEvent>(HandleSpellUpgraded);
            EventBus.Unsubscribe<BossSpawnedEvent>(HandleBossSpawned);
            EventBus.Unsubscribe<BossDefeatedEvent>(HandleBossDefeated);
        }

        private void Update()
        {
            if (_state != HUDState.Active && _state != HUDState.BossOverlayActive)
            {
                return;
            }

            float deltaTime = Time.unscaledDeltaTime;
            float hpLerpSpeed = _config != null ? Mathf.Max(0f, _config.HpLerpSpeed) : 0f;
            _displayHp = Mathf.Lerp(_displayHp, _targetHp, hpLerpSpeed * deltaTime);

            float xpLerpSpeed = _config != null ? Mathf.Max(0f, _config.HpLerpSpeed) : 0f;
            _displayXp = Mathf.Lerp(_displayXp, _targetXp, xpLerpSpeed * deltaTime);

            ApplyBarFill(_hpBarFill, _displayHp);
            ApplyBarFill(_xpBarFill, _displayXp);
            FadeDamageFlash(deltaTime);
        }

        /// <summary>
        /// Returns the HUD from level-up dimming to the active presentation state.
        /// </summary>
        public void ResolveLevelUpDisplay()
        {
            if (_state != HUDState.DimmedForLevelUp)
            {
                return;
            }

            TransitionToState(_bossHpBar != null && _bossHpBar.activeSelf ? HUDState.BossOverlayActive : HUDState.Active);
        }

        private void HandleRunStart(RunStartEvent runStartEvent)
        {
            ResetStoredState();
            TransitionToState(HUDState.Active);
            RefreshAllWidgetsImmediate();
        }

        private void HandleRunEnd(RunEndEvent runEndEvent)
        {
            _elapsedSeconds = Mathf.Max(0f, runEndEvent.SurvivedSeconds);
            UpdateTimerLabel();
            TransitionToState(HUDState.PostRun);
        }

        private void HandleRunPause(RunPauseEvent runPauseEvent)
        {
            if (_state == HUDState.PostRun || _state == HUDState.Hidden)
            {
                return;
            }

            if (runPauseEvent.IsPaused)
            {
                TransitionToState(HUDState.Paused);
                return;
            }

            TransitionToState(_bossHpBar != null && _bossHpBar.activeSelf ? HUDState.BossOverlayActive : HUDState.Active);
        }

        private void HandleRunTimerTick(RunTimerTickEvent runTimerTickEvent)
        {
            _elapsedSeconds = Mathf.Max(0f, runTimerTickEvent.ElapsedSeconds);
            UpdateTimerLabel();
        }

        private void HandlePlayerDamaged(PlayerDamagedEvent playerDamagedEvent)
        {
            int previousMaxHp = _maxHp;
            _currentHp = Mathf.Max(0, playerDamagedEvent.CurrentHp);
            _maxHp = Mathf.Max(0, playerDamagedEvent.MaxHp);
            _targetHp = CalculateNormalizedHp(_currentHp, _maxHp);

            if (previousMaxHp <= 0)
            {
                _displayHp = _targetHp;
                ApplyBarFill(_hpBarFill, _displayHp);
            }

            SetDamageFlashAlpha(_config != null ? _config.DamageFlashAlpha : 0f);
        }

        private void HandleLevelUp(LevelUpEvent levelUpEvent)
        {
            _currentLevel = Mathf.Max(1, levelUpEvent.NewLevel);
            _currentXp = Mathf.Max(0f, levelUpEvent.OverflowXp);
            _xpToNextLevel = GetConfiguredXpToNextLevel(_currentLevel);
            _targetXp = CalculateNormalizedXp(_currentXp, _xpToNextLevel);
            UpdateLevelLabel();
            TransitionToState(HUDState.DimmedForLevelUp);
        }

        private void HandleXpCollected(XPCollectedEvent xpCollectedEvent)
        {
            _currentXp = Mathf.Max(0f, _currentXp + Mathf.Max(0, xpCollectedEvent.Amount));
            _xpToNextLevel = GetConfiguredXpToNextLevel(_currentLevel);
            _targetXp = CalculateNormalizedXp(_currentXp, _xpToNextLevel);
        }

        private void HandleElementAdded(ElementAddedEvent elementAddedEvent)
        {
            if (!IsValidElementSlotIndex(elementAddedEvent.SlotIndex))
            {
                return;
            }

            _elements[elementAddedEvent.SlotIndex] = elementAddedEvent.ElementType;
            UpdateElementSlot(elementAddedEvent.SlotIndex);
        }

        private void HandleElementRemoved(ElementRemovedEvent elementRemovedEvent)
        {
            if (!IsValidElementSlotIndex(elementRemovedEvent.SlotIndex))
            {
                return;
            }

            _elements[elementRemovedEvent.SlotIndex] = null;
            UpdateElementSlot(elementRemovedEvent.SlotIndex);
        }

        private void HandleSpellCrafted(SpellCraftedEvent spellCraftedEvent)
        {
            int slotIndex = FindFirstEmptySpellSlot();
            if (slotIndex < 0)
            {
                return;
            }

            _spells[slotIndex] = new SpellSlotData(
                spellCraftedEvent.SpellId,
                spellCraftedEvent.Level,
                spellCraftedEvent.Kind,
                0f,
                true,
                _config != null ? _config.GetSpellIcon(spellCraftedEvent.SpellId) : null);

            UpdateSpellSlot(slotIndex);
        }

        private void HandleSpellUpgraded(SpellUpgradedEvent spellUpgradedEvent)
        {
            int slotIndex = FindSpellSlotIndex(spellUpgradedEvent.SpellId);
            if (slotIndex < 0)
            {
                return;
            }

            SpellSlotData spellSlotData = _spells[slotIndex];
            spellSlotData.Level = Mathf.Max(1, spellUpgradedEvent.NewLevel);
            _spells[slotIndex] = spellSlotData;
            UpdateSpellSlot(slotIndex);
        }

        private void HandleBossSpawned(BossSpawnedEvent bossSpawnedEvent)
        {
            ApplyBossBarVisibility(true);
            ApplyBarFill(_bossHpBarFill, 1f);
            TransitionToState(HUDState.BossOverlayActive);
        }

        private void HandleBossDefeated(BossDefeatedEvent bossDefeatedEvent)
        {
            ApplyBarFill(_bossHpBarFill, 0f);
            ApplyBossBarVisibility(false);

            if (_state != HUDState.PostRun)
            {
                TransitionToState(HUDState.Active);
            }
        }

        private void ResetStoredState()
        {
            _currentHp = 0;
            _maxHp = 0;
            _currentXp = 0f;
            _xpToNextLevel = GetConfiguredXpToNextLevel(GetInitialLevel());
            _currentLevel = GetInitialLevel();
            _elapsedSeconds = 0f;
            _displayHp = 0f;
            _targetHp = 0f;
            _displayXp = 0f;
            _targetXp = 0f;

            for (int i = 0; i < _elements.Length; i++)
            {
                _elements[i] = null;
            }

            for (int i = 0; i < _spells.Length; i++)
            {
                _spells[i] = default;
            }

            ApplyBossBarVisibility(false);
            ApplyBarFill(_bossHpBarFill, 0f);
            SetDamageFlashAlpha(0f);
        }

        private void RefreshAllWidgetsImmediate()
        {
            ApplyBarFill(_hpBarFill, _targetHp);
            ApplyBarFill(_xpBarFill, _targetXp);
            UpdateLevelLabel();
            UpdateTimerLabel();

            for (int i = 0; i < _elementSlots.Length; i++)
            {
                UpdateElementSlot(i);
            }

            for (int i = 0; i < _spellSlots.Length; i++)
            {
                UpdateSpellSlot(i);
            }
        }

        private void UpdateLevelLabel()
        {
            if (_levelLabel == null)
            {
                return;
            }

            _levelLabel.SetText("Lv. {0}", _currentLevel);
        }

        private void UpdateTimerLabel()
        {
            if (_timerLabel == null)
            {
                return;
            }

            int totalSeconds = Mathf.Max(0, Mathf.FloorToInt(_elapsedSeconds));
            int minutes = totalSeconds / 60;
            int seconds = totalSeconds % 60;
            _timerLabel.SetText("{0:00}:{1:00}", minutes, seconds);
        }

        private void UpdateElementSlot(int slotIndex)
        {
            if (!IsValidElementSlotIndex(slotIndex))
            {
                return;
            }

            Image slotImage = _elementSlots[slotIndex];
            if (slotImage == null)
            {
                return;
            }

            if (!_elements[slotIndex].HasValue)
            {
                slotImage.sprite = null;
                slotImage.enabled = false;
                return;
            }

            slotImage.sprite = _config != null ? _config.GetElementIcon(_elements[slotIndex].Value) : null;
            slotImage.enabled = slotImage.sprite != null;
        }

        private void UpdateSpellSlot(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= _spellSlots.Length)
            {
                return;
            }

            SpellSlotWidget spellSlotWidget = _spellSlots[slotIndex];
            if (spellSlotWidget == null)
            {
                return;
            }

            SpellSlotData spellSlotData = _spells[slotIndex];
            if (!spellSlotData.IsOccupied)
            {
                spellSlotWidget.Clear();
                return;
            }

            spellSlotWidget.SetSpell(spellSlotData);
        }

        private void TransitionToState(HUDState nextState)
        {
            _state = nextState;
            ApplyStatePresentation();
        }

        private void ApplyStatePresentation()
        {
            if (_rootCanvasGroup != null && _config != null)
            {
                _rootCanvasGroup.alpha = _config.GetCanvasAlpha(_state);
                _rootCanvasGroup.interactable = false;
                _rootCanvasGroup.blocksRaycasts = false;
            }
        }

        private void ApplyBossBarVisibility(bool isVisible)
        {
            if (_bossHpBar != null)
            {
                _bossHpBar.SetActive(isVisible);
            }
        }

        private void FadeDamageFlash(float deltaTime)
        {
            if (_damageFlashOverlay == null)
            {
                return;
            }

            Color overlayColor = _damageFlashOverlay.color;
            float flashDuration = _config != null ? _config.DamageFlashDuration : 0f;
            float fadeRate = flashDuration > 0f && overlayColor.a > 0f
                ? (_config.DamageFlashAlpha / flashDuration)
                : float.MaxValue;

            overlayColor.a = Mathf.MoveTowards(overlayColor.a, 0f, fadeRate * deltaTime);
            _damageFlashOverlay.color = overlayColor;
        }

        private void SetDamageFlashAlpha(float alpha)
        {
            if (_damageFlashOverlay == null)
            {
                return;
            }

            Color overlayColor = _damageFlashOverlay.color;
            overlayColor.a = Mathf.Clamp01(alpha);
            _damageFlashOverlay.color = overlayColor;
        }

        private static void ApplyBarFill(Image image, float normalizedValue)
        {
            if (image == null)
            {
                return;
            }

            image.fillAmount = Mathf.Clamp01(normalizedValue);
        }

        private float GetConfiguredXpToNextLevel(int currentLevel)
        {
            return _config != null ? Mathf.Max(0f, _config.GetXpToNextLevel(currentLevel)) : 0f;
        }

        private int GetInitialLevel()
        {
            return _config != null ? Mathf.Max(1, _config.InitialLevel) : 1;
        }

        private static float CalculateNormalizedHp(int currentHp, int maxHp)
        {
            if (maxHp <= 0)
            {
                return 0f;
            }

            return Mathf.Clamp01((float)currentHp / maxHp);
        }

        private static float CalculateNormalizedXp(float currentXp, float xpToNextLevel)
        {
            if (xpToNextLevel <= 0f)
            {
                return 0f;
            }

            return Mathf.Clamp01(currentXp / xpToNextLevel);
        }

        private int FindFirstEmptySpellSlot()
        {
            for (int i = 0; i < _spells.Length; i++)
            {
                if (!_spells[i].IsOccupied)
                {
                    return i;
                }
            }

            return -1;
        }

        private int FindSpellSlotIndex(string spellId)
        {
            if (string.IsNullOrEmpty(spellId))
            {
                return -1;
            }

            for (int i = 0; i < _spells.Length; i++)
            {
                if (_spells[i].IsOccupied && string.Equals(_spells[i].SpellId, spellId, StringComparison.Ordinal))
                {
                    return i;
                }
            }

            return -1;
        }

        private bool IsValidElementSlotIndex(int slotIndex)
        {
            return slotIndex >= 0 && slotIndex < _elementSlots.Length && slotIndex < _elements.Length;
        }
    }

    /// <summary>
    /// Serializable display data for a single spell slot widget.
    /// </summary>
    [Serializable]
    public struct SpellSlotData
    {
        /// <summary>
        /// Initializes a spell slot display payload.
        /// </summary>
        public SpellSlotData(string spellId, int level, SpellKind kind, float cooldownNormalized, bool isOccupied, Sprite icon)
        {
            SpellId = spellId;
            Level = level;
            Kind = kind;
            CooldownNormalized = cooldownNormalized;
            IsOccupied = isOccupied;
            Icon = icon;
        }

        public string SpellId;
        public int Level;
        public SpellKind Kind;
        public float CooldownNormalized;
        public bool IsOccupied;
        public Sprite Icon;
    }

    /// <summary>
    /// Simple spell slot presenter for icon, level text, and cooldown overlay.
    /// </summary>
    [Serializable]
    public sealed class SpellSlotWidget
    {
        [SerializeField] private Image _iconImage;
        [SerializeField] private TextMeshProUGUI _levelLabel;
        [SerializeField] private Image _cooldownOverlay;

        /// <summary>
        /// Applies display data to the widget without mutating gameplay state.
        /// </summary>
        public void SetSpell(SpellSlotData spellSlotData)
        {
            if (_iconImage != null)
            {
                _iconImage.sprite = spellSlotData.Icon;
                _iconImage.enabled = spellSlotData.Icon != null;
            }

            if (_levelLabel != null)
            {
                _levelLabel.SetText("Lv. {0}", Mathf.Max(1, spellSlotData.Level));
            }

            if (_cooldownOverlay != null)
            {
                _cooldownOverlay.fillAmount = Mathf.Clamp01(spellSlotData.CooldownNormalized);
                _cooldownOverlay.enabled = _cooldownOverlay.fillAmount > 0f;
            }
        }

        /// <summary>
        /// Clears all visual content from the widget.
        /// </summary>
        public void Clear()
        {
            if (_iconImage != null)
            {
                _iconImage.sprite = null;
                _iconImage.enabled = false;
            }

            if (_levelLabel != null)
            {
                _levelLabel.SetText(string.Empty);
            }

            if (_cooldownOverlay != null)
            {
                _cooldownOverlay.fillAmount = 0f;
                _cooldownOverlay.enabled = false;
            }
        }
    }

    /// <summary>
    /// Configuration source for HUD interpolation, flash behavior, and icon lookup data.
    /// </summary>
    [CreateAssetMenu(fileName = "HUDConfig", menuName = "KokTengri/UI/HUD Config")]
    public sealed class HUDConfigSO : ScriptableObject
    {
        [SerializeField, Min(1)] private int _initialLevel = 1;
        [SerializeField, Min(0f)] private float _hpLerpSpeed = 8f;
        [SerializeField, Min(0f)] private float _damageFlashDuration = 0.2f;
        [SerializeField, Range(0f, 1f)] private float _damageFlashAlpha = 0.65f;
        [SerializeField, Range(0f, 1f)] private float _hiddenAlpha = 0f;
        [SerializeField, Range(0f, 1f)] private float _activeAlpha = 1f;
        [SerializeField, Range(0f, 1f)] private float _dimmedAlpha = 0.45f;
        [SerializeField, Range(0f, 1f)] private float _pausedAlpha = 0.8f;
        [SerializeField, Range(0f, 1f)] private float _postRunAlpha = 1f;
        [SerializeField] private float[] _xpToNextLevelByCurrentLevel = Array.Empty<float>();
        [SerializeField] private ElementIconEntry[] _elementIcons = Array.Empty<ElementIconEntry>();
        [SerializeField] private SpellIconEntry[] _spellIcons = Array.Empty<SpellIconEntry>();

        public int InitialLevel => _initialLevel;

        public float HpLerpSpeed => _hpLerpSpeed;

        public float DamageFlashDuration => _damageFlashDuration;

        public float DamageFlashAlpha => _damageFlashAlpha;

        /// <summary>
        /// Returns the configured XP required to advance from the provided level.
        /// </summary>
        public float GetXpToNextLevel(int currentLevel)
        {
            int index = currentLevel - 1;
            if (_xpToNextLevelByCurrentLevel == null || index < 0 || index >= _xpToNextLevelByCurrentLevel.Length)
            {
                return 0f;
            }

            return Mathf.Max(0f, _xpToNextLevelByCurrentLevel[index]);
        }

        /// <summary>
        /// Returns the sprite configured for the provided element type.
        /// </summary>
        public Sprite GetElementIcon(ElementType elementType)
        {
            if (_elementIcons == null)
            {
                return null;
            }

            for (int i = 0; i < _elementIcons.Length; i++)
            {
                if (_elementIcons[i].ElementType == elementType)
                {
                    return _elementIcons[i].Icon;
                }
            }

            return null;
        }

        /// <summary>
        /// Returns the sprite configured for the provided spell identifier.
        /// </summary>
        public Sprite GetSpellIcon(string spellId)
        {
            if (_spellIcons == null || string.IsNullOrEmpty(spellId))
            {
                return null;
            }

            for (int i = 0; i < _spellIcons.Length; i++)
            {
                if (string.Equals(_spellIcons[i].SpellId, spellId, StringComparison.Ordinal))
                {
                    return _spellIcons[i].Icon;
                }
            }

            return null;
        }

        /// <summary>
        /// Returns the configured root HUD alpha for a visual state.
        /// </summary>
        public float GetCanvasAlpha(HUDState hudState)
        {
            return hudState switch
            {
                HUDState.Hidden => _hiddenAlpha,
                HUDState.DimmedForLevelUp => _dimmedAlpha,
                HUDState.Paused => _pausedAlpha,
                HUDState.PostRun => _postRunAlpha,
                _ => _activeAlpha,
            };
        }

        [Serializable]
        private struct ElementIconEntry
        {
            public ElementType ElementType;
            public Sprite Icon;
        }

        [Serializable]
        private struct SpellIconEntry
        {
            public string SpellId;
            public Sprite Icon;
        }
    }
}
