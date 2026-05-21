using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class SpellWordShopPanelController : MonoBehaviour, IMajorPanel
{
    [Header("Root")]
    [SerializeField] private GameObject _root;

    [Header("References")]
    [SerializeField] private Player _player;
    [SerializeField] private CombatWordsData _combatWordsData;

    [Header("Controls")]
    [SerializeField] private Button _leaveButton;
    [SerializeField] private Button _buyButton;
    [SerializeField] private TMP_Text _buyButtonLabel;

    [Header("Memory Display")]
    [SerializeField] private TMP_Text _currentMemoryLevelLabel;
    [SerializeField] private string _currentMemoryLevelLabelFormat = "Current Memory Level: {0}";

    [Header("Word List")]
    [SerializeField] private Transform _wordButtonContainer;
    [SerializeField] private WordShopButtonView _wordButtonPrefab;

    [Header("Empty State")]
    [SerializeField] private GameObject _emptyStateRoot;
    [SerializeField] private TMP_Text _emptyStateLabel;
    [SerializeField] private string _emptyStateMessage = "You have regained your spellcasting, now use it wisely.";

    [Header("Visual States")]
    [SerializeField, Range(0.1f, 1f)] private float _buyDisabledAlpha = 0.6f;

    public PanelId Id => PanelId.WordShop;
    public bool IsOpen => _root != null && _root.activeSelf;
    public bool PausesGame => true;
    public bool BlocksGameplayInput => true;

    private readonly List<WordShopButtonView> _spawnedButtons = new();
    private readonly List<WordData> _availableWords = new();
    private WordData _selectedWord;
    private CanvasGroup _buyButtonCanvasGroup;

    private void Awake()
    {
        if (_root != null)
            _root.SetActive(false);

        if (_leaveButton != null)
            _leaveButton.onClick.AddListener(HandleLeaveClicked);

        if (_buyButton != null)
            _buyButton.onClick.AddListener(HandleBuyClicked);

        if (_emptyStateLabel != null)
            _emptyStateLabel.text = _emptyStateMessage;

        if (_buyButton != null)
        {
            _buyButtonCanvasGroup = _buyButton.GetComponent<CanvasGroup>();
            if (_buyButtonCanvasGroup == null)
                _buyButtonCanvasGroup = _buyButton.gameObject.AddComponent<CanvasGroup>();
        }
    }

    private void OnDestroy()
    {
        if (_leaveButton != null)
            _leaveButton.onClick.RemoveListener(HandleLeaveClicked);

        if (_buyButton != null)
            _buyButton.onClick.RemoveListener(HandleBuyClicked);

        UnsubscribeRuntimeEvents();
    }

    public void Open()
    {
        if (_root != null)
            _root.SetActive(true);

        SubscribeRuntimeEvents();
        RefreshShop();
    }

    public void Close()
    {
        if (_root != null)
            _root.SetActive(false);

        UnsubscribeRuntimeEvents();
    }

    private void SubscribeRuntimeEvents()
    {
        UnsubscribeRuntimeEvents();

        if (_player?.Data != null)
            _player.Data.OnMemoryXPChanged += HandleMemoryChanged;

        if (_player?.SpellWords != null)
            _player.SpellWords.OnWordsChanged += HandleWordsChanged;
    }

    private void UnsubscribeRuntimeEvents()
    {
        if (_player?.Data != null)
            _player.Data.OnMemoryXPChanged -= HandleMemoryChanged;

        if (_player?.SpellWords != null)
            _player.SpellWords.OnWordsChanged -= HandleWordsChanged;
    }

    private void HandleMemoryChanged(int _, int __, int ___)
    {
        RefreshCurrentMemoryLevelLabel();
        RefreshBuyButtonState();
    }

    private void HandleWordsChanged()
    {
        RefreshShop();
    }

    private void RefreshShop()
    {
        BuildAvailableWords();
        RebuildWordButtons();
        SelectFirstWordIfNeeded();
        RefreshStateVisibility();
        RefreshCurrentMemoryLevelLabel();
        RefreshBuyButtonState();
    }

    private void BuildAvailableWords()
    {
        _availableWords.Clear();

        if (_combatWordsData == null || _player?.SpellWords == null)
            return;

        _combatWordsData.GetAvailableWords(_player.SpellWords, _availableWords);

        if (_selectedWord != null && !_availableWords.Contains(_selectedWord))
            _selectedWord = null;
    }

    private void RebuildWordButtons()
    {
        for (int i = 0; i < _spawnedButtons.Count; i++)
        {
            var button = _spawnedButtons[i];
            if (button == null)
                continue;

            button.Clicked -= HandleWordClicked;
            Destroy(button.gameObject);
        }

        _spawnedButtons.Clear();

        if (_wordButtonContainer == null || _wordButtonPrefab == null)
            return;

        for (int i = 0; i < _availableWords.Count; i++)
        {
            var button = Instantiate(_wordButtonPrefab, _wordButtonContainer);
            button.Bind(_availableWords[i]);
            button.Clicked += HandleWordClicked;
            _spawnedButtons.Add(button);
        }

        RefreshWordHighlights();
    }

    private void HandleWordClicked(WordData word)
    {
        if (word == null)
            return;

        _selectedWord = word;
        RefreshWordHighlights();
        RefreshBuyButtonState();
    }

    private void SelectFirstWordIfNeeded()
    {
        if (_selectedWord == null && _availableWords.Count > 0)
            _selectedWord = _availableWords[0];

        RefreshWordHighlights();
    }

    private void RefreshWordHighlights()
    {
        for (int i = 0; i < _spawnedButtons.Count; i++)
        {
            var button = _spawnedButtons[i];
            if (button == null)
                continue;

            button.SetSelected(_selectedWord == _availableWords[i]);
        }
    }

    private void RefreshStateVisibility()
    {
        bool hasAnyWordsToBuy = _availableWords.Count > 0;

        if (_wordButtonContainer != null)
            _wordButtonContainer.gameObject.SetActive(hasAnyWordsToBuy);

        if (_buyButton != null)
            _buyButton.gameObject.SetActive(hasAnyWordsToBuy);

        if (_emptyStateRoot != null)
            _emptyStateRoot.SetActive(!hasAnyWordsToBuy);

        if (!hasAnyWordsToBuy)
            _selectedWord = null;
    }

    private void RefreshCurrentMemoryLevelLabel()
    {
        if (_currentMemoryLevelLabel == null)
            return;

        int currentMemoryLevel = _player?.Data != null ? _player.Data.CurrentMemoryLevel : 0;
        _currentMemoryLevelLabel.text = string.Format(_currentMemoryLevelLabelFormat, currentMemoryLevel);
    }

    private void RefreshBuyButtonState()
    {
        if (_buyButtonLabel != null)
            _buyButtonLabel.text = GetBuyButtonLabel();

        bool canBuy = CanBuySelectedWord();

        if (_buyButton != null)
            _buyButton.interactable = canBuy;

        if (_buyButtonCanvasGroup != null)
            _buyButtonCanvasGroup.alpha = canBuy ? 1f : _buyDisabledAlpha;
    }

    private string GetBuyButtonLabel()
    {
        if (_selectedWord == null)
            return "Buy";

        return $"Buy ({_selectedWord.MemoryCost} memory levels)";
    }

    private bool CanBuySelectedWord()
    {
        if (_selectedWord == null || _player?.Data == null)
            return false;

        return _player.Data.CurrentMemoryLevel >= _selectedWord.MemoryCost;
    }

    private void HandleBuyClicked()
    {
        if (_selectedWord == null || _player?.Data == null || _player.SpellWords == null)
            return;

        if (!_selectedWord.IsValid || _player.SpellWords.IsUnlocked(_selectedWord))
            return;

        if (!_player.Data.TrySpendMemoryLevels(_selectedWord.MemoryCost))
            return;

        if (!_player.SpellWords.TryUnlock(_selectedWord))
            return;

        RefreshShop();
    }

    private void HandleLeaveClicked()
    {
        if (PanelManager.Instance != null)
            PanelManager.Instance.CloseCurrentMajorPanel();
        else
            Close();
    }
}
