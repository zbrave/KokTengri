using System;
using System.Collections.Generic;
using KokTengri.Core;
using KokTengri.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KokTengri.UI
{
    /// <summary>
    /// Presents level-up element choices, pauses the run, and routes the selected outcome through SpellCrafting.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LevelUpSelectionController : MonoBehaviour
    {
        private static readonly ElementType[] ElementPool =
        {
            ElementType.Od,
            ElementType.Sub,
            ElementType.Yer,
            ElementType.Yel,
            ElementType.Temur,
        };

        private const float BaseWeight = 1f;
        private const float HealthFallbackPercent = 0.03f;
        private const float SpeedFallbackPercent = 0.02f;
        private const float DamageFallbackPercent = 0.02f;

        private enum SelectionState
        {
            Hidden,
            Showing,
            Evaluating,
            Resolving,
        }

        private enum FallbackRewardType
        {
            None = 0,
            Health = 1,
            Speed = 2,
            Damage = 3,
        }

        private readonly Queue<LevelUpEvent> _pendingLevelUps = new();
        private readonly CardOption[] _activeCards = new CardOption[3];
        private readonly ElementInventory _defaultInventory = new();
        private readonly SpellSlotManager _defaultSpellSlots = new();

        [SerializeField] private GameObject _overlayRoot;
        [SerializeField] private LevelUpCardWidget[] _cardWidgets = new LevelUpCardWidget[3];
        [SerializeField] private Button _rerollButton;
        [SerializeField] private TextMeshProUGUI _rerollLabel;
        [SerializeField] private ElementInventoryWidget _inventoryDisplay;
        [SerializeField] private SpellSlotsWidget _spellSlotDisplay;
        [SerializeField] private LevelUpSelectionConfigSO _config;

        private SelectionState _state = SelectionState.Hidden;
        private ElementInventory _inventory;
        private SpellSlotManager _spellSlots;
        private SpellCrafting _spellCrafting;
        private System.Random _random = new();
        private LevelUpEvent _activeLevelUp;
        private ElementType _heroStartingElement = ElementType.Od;
        private int _rerollsRemaining;
        private bool _hasActiveLevelUp;
        private bool _discardEnabled = true;
        private bool _isRunPausedByOverlay;

        /// <summary>
        /// Gets whether the overlay is currently visible and evaluating a level-up choice.
        /// </summary>
        public bool IsOpen => _state != SelectionState.Hidden;

        private void Awake()
        {
            _inventory = _defaultInventory;
            _spellSlots = _defaultSpellSlots;
            _spellCrafting = new SpellCrafting(_inventory);

            EnsureWidgetArray();
            ApplyOverlayVisibility(false);
            RefreshSupportingWidgets();
            RefreshRerollState();
        }

        private void OnEnable()
        {
            EventBus.Subscribe<LevelUpEvent>(HandleLevelUpEvent);
            EventBus.Subscribe<RunStartEvent>(HandleRunStartEvent);
            EventBus.Subscribe<RunEndEvent>(HandleRunEndEvent);

            if (_rerollButton != null)
            {
                _rerollButton.onClick.AddListener(HandleRerollPressed);
            }

            for (int i = 0; i < _cardWidgets.Length; i++)
            {
                LevelUpCardWidget widget = _cardWidgets[i];
                if (widget != null)
                {
                    widget.SetSelectionCallback(HandleCardSelected);
                }
            }
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<LevelUpEvent>(HandleLevelUpEvent);
            EventBus.Unsubscribe<RunStartEvent>(HandleRunStartEvent);
            EventBus.Unsubscribe<RunEndEvent>(HandleRunEndEvent);

            if (_rerollButton != null)
            {
                _rerollButton.onClick.RemoveListener(HandleRerollPressed);
            }

            for (int i = 0; i < _cardWidgets.Length; i++)
            {
                LevelUpCardWidget widget = _cardWidgets[i];
                if (widget != null)
                {
                    widget.SetSelectionCallback(null);
                }
            }
        }

        /// <summary>
        /// Injects the live runtime inventory and spell slots used for level-up evaluation.
        /// </summary>
        /// <param name="inventory">The current run element inventory.</param>
        /// <param name="spellSlots">The current run spell slots.</param>
        public void BindRuntime(ElementInventory inventory, SpellSlotManager spellSlots)
        {
            _inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
            _spellSlots = spellSlots ?? throw new ArgumentNullException(nameof(spellSlots));
            _spellCrafting = new SpellCrafting(_inventory);

            RefreshSupportingWidgets();
        }

        /// <summary>
        /// Enables or disables discard resolution when an inventory-full selection has no valid crafting path.
        /// </summary>
        /// <param name="discardEnabled">True to allow discarding dead-end element picks.</param>
        public void SetDiscardEnabled(bool discardEnabled)
        {
            _discardEnabled = discardEnabled;
        }

        private void HandleRunStartEvent(RunStartEvent runStartEvent)
        {
            _random = new System.Random(runStartEvent.Seed);
            _heroStartingElement = ResolveHeroStartingElement(runStartEvent);
            ResetOverlayState(false);
        }

        private void HandleRunEndEvent(RunEndEvent runEndEvent)
        {
            ResetOverlayState(false);
        }

        private void HandleLevelUpEvent(LevelUpEvent levelUpEvent)
        {
            _pendingLevelUps.Enqueue(levelUpEvent);

            if (_hasActiveLevelUp || _state != SelectionState.Hidden)
            {
                return;
            }

            ShowNextPendingLevelUp();
        }

        private void HandleRerollPressed()
        {
            if (_state != SelectionState.Evaluating || _rerollsRemaining <= 0)
            {
                return;
            }

            _rerollsRemaining--;
            TransitionTo(SelectionState.Showing);
            GenerateCards();
            TransitionTo(SelectionState.Evaluating);
            RefreshRerollState();
        }

        private void HandleCardSelected(LevelUpCardWidget selectedWidget)
        {
            if (selectedWidget == null || _state != SelectionState.Evaluating)
            {
                return;
            }

            int selectedIndex = selectedWidget.CardIndex;
            if (selectedIndex < 0 || selectedIndex >= _activeCards.Length)
            {
                return;
            }

            ResolveSelection(_activeCards[selectedIndex]);
        }

        private void ShowNextPendingLevelUp()
        {
            if (_pendingLevelUps.Count == 0)
            {
                HideOverlayAndResume();
                return;
            }

            _activeLevelUp = _pendingLevelUps.Peek();
            _hasActiveLevelUp = true;
            _rerollsRemaining = Mathf.Max(0, GetFreeRerollsPerScreen());

            TransitionTo(SelectionState.Showing);
            ApplyOverlayVisibility(true);
            PublishPauseIfNeeded(true);
            RefreshSupportingWidgets();
            GenerateCards();
            TransitionTo(SelectionState.Evaluating);
            RefreshRerollState();
        }

        private void GenerateCards()
        {
            EnsureWidgetArray();

            IReadOnlyList<SpellSlotEntry> ownedSpells = _spellSlots.GetAllSpells();
            List<ElementType> availableElements = new List<ElementType>(ElementPool);
            int visibleCardCount = Mathf.Min(GetOptionsPerLevelUp(), _cardWidgets.Length, _activeCards.Length, availableElements.Count);

            for (int cardIndex = 0; cardIndex < _activeCards.Length; cardIndex++)
            {
                _activeCards[cardIndex] = default;
            }

            for (int cardIndex = 0; cardIndex < _cardWidgets.Length; cardIndex++)
            {
                LevelUpCardWidget widget = _cardWidgets[cardIndex];

                if (widget == null)
                {
                    continue;
                }

                widget.SetCardIndex(cardIndex);

                if (cardIndex >= visibleCardCount)
                {
                    widget.SetVisible(false);
                    continue;
                }

                ElementType element = DrawWeightedElement(availableElements);
                SpellCrafting.CraftingResult evaluation = _spellCrafting.EvaluateSelection(element, ownedSpells, _spellSlots.MaxSlots);
                bool hasFallback = HasOnlyMaxedSpellMatches(element, ownedSpells);
                FallbackRewardType fallbackReward = hasFallback ? GetFallbackReward(cardIndex) : FallbackRewardType.None;

                CardOption option = new CardOption(element, evaluation, fallbackReward, evaluation.Type == CraftingResultType.InventoryFullNoMatch && _discardEnabled);
                _activeCards[cardIndex] = option;

                widget.Bind(new LevelUpCardWidget.CardViewModel(
                    element.ToString(),
                    BuildTooltip(option),
                    option.IsDiscardOption,
                    fallbackReward != FallbackRewardType.None));
                widget.SetVisible(true);
            }
        }

        private ElementType DrawWeightedElement(List<ElementType> availableElements)
        {
            double totalWeight = 0d;

            for (int i = 0; i < availableElements.Count; i++)
            {
                totalWeight += GetElementWeight(availableElements[i]);
            }

            double threshold = _random.NextDouble() * totalWeight;
            double cumulative = 0d;

            for (int i = 0; i < availableElements.Count; i++)
            {
                ElementType element = availableElements[i];
                cumulative += GetElementWeight(element);

                if (threshold > cumulative && i < availableElements.Count - 1)
                {
                    continue;
                }

                availableElements.RemoveAt(i);
                return element;
            }

            ElementType fallbackElement = availableElements[availableElements.Count - 1];
            availableElements.RemoveAt(availableElements.Count - 1);
            return fallbackElement;
        }

        private double GetElementWeight(ElementType element)
        {
            float heroBias = element == _heroStartingElement ? GetStartingElementBias() : 0f;
            return BaseWeight * (1f + heroBias);
        }

        private string BuildTooltip(CardOption option)
        {
            if (option.FallbackReward != FallbackRewardType.None)
            {
                return option.FallbackReward switch
                {
                    FallbackRewardType.Health => $"Stat boost fallback: +{HealthFallbackPercent * 100f:0}% HP",
                    FallbackRewardType.Speed => $"Stat boost fallback: +{SpeedFallbackPercent * 100f:0}% Speed",
                    FallbackRewardType.Damage => $"Stat boost fallback: +{DamageFallbackPercent * 100f:0}% Damage",
                    _ => string.Empty,
                };
            }

            return option.CraftingResult.Type switch
            {
                CraftingResultType.NewSpell => $"Creates {option.CraftingResult.SpellId}",
                CraftingResultType.UpgradeSpell => $"Upgrades {option.CraftingResult.SpellId} to Lv.{option.CraftingResult.NewLevel}",
                CraftingResultType.AddToInventory => "Added to inventory",
                CraftingResultType.BlockedByFullSlots => "Spell slots full",
                CraftingResultType.InventoryFullNoMatch when option.IsDiscardOption => "No valid path (discard option)",
                CraftingResultType.InventoryFullNoMatch => "No valid path",
                _ => string.Empty,
            };
        }

        private bool HasOnlyMaxedSpellMatches(ElementType selectedElement, IReadOnlyList<SpellSlotEntry> ownedSpells)
        {
            SpellCrafting.CraftingResult currentEvaluation = _spellCrafting.EvaluateSelection(selectedElement, ownedSpells, _spellSlots.MaxSlots);
            if (currentEvaluation.Type == CraftingResultType.NewSpell || currentEvaluation.Type == CraftingResultType.UpgradeSpell)
            {
                return false;
            }

            List<SpellSlotEntry> withoutMaxedSpells = new List<SpellSlotEntry>(ownedSpells.Count);
            bool removedMaxedSpell = false;

            for (int i = 0; i < ownedSpells.Count; i++)
            {
                SpellSlotEntry ownedSpell = ownedSpells[i];
                if (ownedSpell.Level >= 5)
                {
                    removedMaxedSpell = true;
                    continue;
                }

                withoutMaxedSpells.Add(ownedSpell);
            }

            if (!removedMaxedSpell)
            {
                return false;
            }

            SpellCrafting.CraftingResult simulatedEvaluation = _spellCrafting.EvaluateSelection(selectedElement, withoutMaxedSpells, _spellSlots.MaxSlots);
            return simulatedEvaluation.Type == CraftingResultType.NewSpell ||
                   (simulatedEvaluation.Type == CraftingResultType.BlockedByFullSlots && currentEvaluation.Type != CraftingResultType.BlockedByFullSlots);
        }

        private void ResolveSelection(CardOption selectedOption)
        {
            TransitionTo(SelectionState.Resolving);

            if (selectedOption.FallbackReward != FallbackRewardType.None)
            {
                CompleteResolution();
                return;
            }

            IReadOnlyList<SpellSlotEntry> ownedSpells = _spellSlots.GetAllSpells();
            SpellCrafting.CraftingResult resolution = _spellCrafting.ProcessSelection(selectedOption.Element, ownedSpells, _spellSlots.MaxSlots);

            if (resolution.Type == CraftingResultType.InventoryFullNoMatch && !_discardEnabled)
            {
                TransitionTo(SelectionState.Evaluating);
                return;
            }

            RefreshSupportingWidgets();
            CompleteResolution();
        }

        private void CompleteResolution()
        {
            if (_pendingLevelUps.Count > 0)
            {
                _pendingLevelUps.Dequeue();
            }

            _hasActiveLevelUp = false;

            if (_pendingLevelUps.Count == 0)
            {
                HideOverlayAndResume();
                return;
            }

            ShowNextPendingLevelUp();
        }

        private void HideOverlayAndResume()
        {
            ApplyOverlayVisibility(false);
            TransitionTo(SelectionState.Hidden);
            RefreshRerollState();
            PublishPauseIfNeeded(false);
        }

        private void ResetOverlayState(bool resumeRun)
        {
            _pendingLevelUps.Clear();
            _hasActiveLevelUp = false;
            _activeLevelUp = default;
            _rerollsRemaining = 0;

            for (int i = 0; i < _activeCards.Length; i++)
            {
                _activeCards[i] = default;
            }

            for (int i = 0; i < _cardWidgets.Length; i++)
            {
                if (_cardWidgets[i] != null)
                {
                    _cardWidgets[i].SetVisible(false);
                }
            }

            ApplyOverlayVisibility(false);
            TransitionTo(SelectionState.Hidden);
            RefreshRerollState();

            if (resumeRun)
            {
                PublishPauseIfNeeded(false);
            }
            else
            {
                _isRunPausedByOverlay = false;
            }
        }

        private void PublishPauseIfNeeded(bool isPaused)
        {
            if (isPaused)
            {
                if (_isRunPausedByOverlay)
                {
                    return;
                }

                EventBus.Publish(new RunPauseEvent(true, _activeLevelUp.RunTime));
                _isRunPausedByOverlay = true;
                return;
            }

            if (!_isRunPausedByOverlay)
            {
                return;
            }

            EventBus.Publish(new RunPauseEvent(false, _activeLevelUp.RunTime));
            _isRunPausedByOverlay = false;
        }

        private void ApplyOverlayVisibility(bool isVisible)
        {
            if (_overlayRoot == null)
            {
                return;
            }

            if (_overlayRoot.TryGetComponent(out CanvasGroup canvasGroup))
            {
                canvasGroup.alpha = isVisible ? Mathf.Clamp01(GetOverlayDimAlpha()) : 0f;
                canvasGroup.interactable = isVisible;
                canvasGroup.blocksRaycasts = isVisible;
            }

            _overlayRoot.SetActive(isVisible);
        }

        private void RefreshSupportingWidgets()
        {
            if (_inventoryDisplay != null)
            {
                _inventoryDisplay.Refresh(_inventory);
            }

            if (_spellSlotDisplay != null)
            {
                _spellSlotDisplay.Refresh(_spellSlots.GetAllSpells(), _spellSlots.MaxSlots);
            }
        }

        private void RefreshRerollState()
        {
            if (_rerollLabel != null)
            {
                _rerollLabel.text = $"Re-roll ({_rerollsRemaining})";
            }

            if (_rerollButton != null)
            {
                _rerollButton.interactable = _state == SelectionState.Evaluating && _rerollsRemaining > 0;
            }
        }

        private void EnsureWidgetArray()
        {
            if (_cardWidgets == null || _cardWidgets.Length != 3)
            {
                _cardWidgets = new LevelUpCardWidget[3];
            }
        }

        private void TransitionTo(SelectionState nextState)
        {
            _state = nextState;
        }

        private int GetOptionsPerLevelUp()
        {
            return _config != null ? Mathf.Max(1, _config.OptionsPerLevelUp) : 3;
        }

        private int GetFreeRerollsPerScreen()
        {
            return _config != null ? Mathf.Max(0, _config.FreeRerollsPerScreen) : 1;
        }

        private float GetStartingElementBias()
        {
            return _config != null ? Mathf.Max(0f, _config.StartingElementBias) : 0.15f;
        }

        private float GetOverlayDimAlpha()
        {
            return _config != null ? _config.OverlayDimAlpha : 1f;
        }

        private static FallbackRewardType GetFallbackReward(int cardIndex)
        {
            return cardIndex % 3 switch
            {
                0 => FallbackRewardType.Health,
                1 => FallbackRewardType.Speed,
                _ => FallbackRewardType.Damage,
            };
        }

        private static ElementType ResolveHeroStartingElement(RunStartEvent runStartEvent)
        {
            string combinedId = string.Concat(runStartEvent.HeroId, " ", runStartEvent.ClassId).ToLowerInvariant();

            if (combinedId.Contains("od") || combinedId.Contains("fire"))
            {
                return ElementType.Od;
            }

            if (combinedId.Contains("sub") || combinedId.Contains("water"))
            {
                return ElementType.Sub;
            }

            if (combinedId.Contains("yer") || combinedId.Contains("earth"))
            {
                return ElementType.Yer;
            }

            if (combinedId.Contains("yel") || combinedId.Contains("wind"))
            {
                return ElementType.Yel;
            }

            if (combinedId.Contains("temur") || combinedId.Contains("metal") || combinedId.Contains("iron") || combinedId.Contains("mergen"))
            {
                return ElementType.Temur;
            }

            if (combinedId.Contains("batur"))
            {
                return ElementType.Yer;
            }

            if (combinedId.Contains("otaci"))
            {
                return ElementType.Sub;
            }

            if (combinedId.Contains("kam"))
            {
                return ElementType.Od;
            }

            return ElementType.Od;
        }

        private readonly struct CardOption
        {
            public CardOption(ElementType element, SpellCrafting.CraftingResult craftingResult, FallbackRewardType fallbackReward, bool isDiscardOption)
            {
                Element = element;
                CraftingResult = craftingResult;
                FallbackReward = fallbackReward;
                IsDiscardOption = isDiscardOption;
            }

            public ElementType Element { get; }

            public SpellCrafting.CraftingResult CraftingResult { get; }

            public FallbackRewardType FallbackReward { get; }

            public bool IsDiscardOption { get; }
        }
    }

    /// <summary>
    /// ScriptableObject config for level-up selection tuning values.
    /// </summary>
    [CreateAssetMenu(fileName = "LevelUpSelectionConfig", menuName = "KokTengri/UI/Level Up Selection Config")]
    public sealed class LevelUpSelectionConfigSO : ScriptableObject
    {
        [field: SerializeField, Min(1)] public int OptionsPerLevelUp { get; private set; } = 3;

        [field: SerializeField, Min(0)] public int FreeRerollsPerScreen { get; private set; } = 1;

        [field: SerializeField, Min(0f)] public float StartingElementBias { get; private set; } = 0.15f;

        [field: SerializeField, Range(0f, 1f)] public float OverlayDimAlpha { get; private set; } = 1f;
    }

    /// <summary>
    /// Minimal card widget binding surface used by the level-up selection overlay.
    /// </summary>
    public sealed class LevelUpCardWidget : MonoBehaviour
    {
        [Serializable]
        public readonly struct CardViewModel
        {
            public CardViewModel(string title, string description, bool showDiscardBadge, bool showFallbackBadge)
            {
                Title = title;
                Description = description;
                ShowDiscardBadge = showDiscardBadge;
                ShowFallbackBadge = showFallbackBadge;
            }

            public string Title { get; }

            public string Description { get; }

            public bool ShowDiscardBadge { get; }

            public bool ShowFallbackBadge { get; }
        }

        [SerializeField] private GameObject _root;
        [SerializeField] private Button _selectButton;
        [SerializeField] private TextMeshProUGUI _titleLabel;
        [SerializeField] private TextMeshProUGUI _descriptionLabel;
        [SerializeField] private GameObject _discardBadge;
        [SerializeField] private GameObject _fallbackBadge;

        private Action<LevelUpCardWidget> _selectionCallback;

        public int CardIndex { get; private set; }

        private void Awake()
        {
            if (_selectButton != null)
            {
                _selectButton.onClick.AddListener(NotifySelected);
            }
        }

        private void OnDestroy()
        {
            if (_selectButton != null)
            {
                _selectButton.onClick.RemoveListener(NotifySelected);
            }
        }

        /// <summary>
        /// Sets the stable slot index used by the controller to resolve selections.
        /// </summary>
        /// <param name="cardIndex">The card index inside the three-card overlay.</param>
        public void SetCardIndex(int cardIndex)
        {
            CardIndex = cardIndex;
        }

        /// <summary>
        /// Binds display content for this card.
        /// </summary>
        /// <param name="viewModel">The title, description, and badges to display.</param>
        public void Bind(CardViewModel viewModel)
        {
            if (_titleLabel != null)
            {
                _titleLabel.text = viewModel.Title;
            }

            if (_descriptionLabel != null)
            {
                _descriptionLabel.text = viewModel.Description;
            }

            if (_discardBadge != null)
            {
                _discardBadge.SetActive(viewModel.ShowDiscardBadge);
            }

            if (_fallbackBadge != null)
            {
                _fallbackBadge.SetActive(viewModel.ShowFallbackBadge);
            }
        }

        /// <summary>
        /// Sets the callback invoked when the card button is pressed.
        /// </summary>
        /// <param name="selectionCallback">The action called for a confirmed card click.</param>
        public void SetSelectionCallback(Action<LevelUpCardWidget> selectionCallback)
        {
            _selectionCallback = selectionCallback;
        }

        /// <summary>
        /// Shows or hides the full card root.
        /// </summary>
        /// <param name="isVisible">True to show the card; otherwise false.</param>
        public void SetVisible(bool isVisible)
        {
            GameObject target = _root != null ? _root : gameObject;
            target.SetActive(isVisible);
        }

        private void NotifySelected()
        {
            _selectionCallback?.Invoke(this);
        }
    }

    /// <summary>
    /// Minimal inventory presenter used by the overlay to display current element slots.
    /// </summary>
    public sealed class ElementInventoryWidget : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI[] _slotLabels = new TextMeshProUGUI[ElementInventory.MaxSlots];

        /// <summary>
        /// Refreshes the three-slot inventory display from the supplied inventory state.
        /// </summary>
        /// <param name="inventory">The inventory to visualize.</param>
        public void Refresh(ElementInventory inventory)
        {
            if (inventory == null)
            {
                return;
            }

            for (int i = 0; i < _slotLabels.Length; i++)
            {
                if (_slotLabels[i] == null)
                {
                    continue;
                }

                ElementType? element = inventory.GetElementAt(i);
                _slotLabels[i].text = element.HasValue ? element.Value.ToString() : "-";
            }
        }
    }

    /// <summary>
    /// Minimal spell slot presenter used by the overlay to display owned spells.
    /// </summary>
    public sealed class SpellSlotsWidget : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI[] _slotLabels = new TextMeshProUGUI[6];

        /// <summary>
        /// Refreshes the spell slot display from the supplied spell list.
        /// </summary>
        /// <param name="spells">The owned spell snapshot.</param>
        /// <param name="maxSlots">The maximum number of spell slots to render.</param>
        public void Refresh(IReadOnlyList<SpellSlotEntry> spells, int maxSlots)
        {
            int labelCount = Mathf.Min(_slotLabels.Length, maxSlots);

            for (int i = 0; i < labelCount; i++)
            {
                if (_slotLabels[i] == null)
                {
                    continue;
                }

                if (spells != null && i < spells.Count && !string.IsNullOrWhiteSpace(spells[i].SpellId))
                {
                    _slotLabels[i].text = $"{spells[i].SpellId} Lv.{spells[i].Level}";
                }
                else
                {
                    _slotLabels[i].text = "-";
                }
            }
        }
    }
}
