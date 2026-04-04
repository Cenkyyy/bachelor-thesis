using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class WordShopPanelController : MonoBehaviour, IMajorPanel
{
    [Header("Root")]
    [SerializeField] private GameObject _root;

    [Header("References")]
    [SerializeField] private Player _player;
    [SerializeField] private WordShopCatalogData _catalog;

    [Header("Controls")]
    [SerializeField] private Button _leaveButton;
    [SerializeField] private Button _buyButton;
    [SerializeField] private TMP_Text _buyButtonLabel;

    [Header("Word List")]
    [SerializeField] private Transform _wordButtonContainer;
    [SerializeField] private WordShopWordButton _wordButtonPrefab;

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

    private readonly List<WordShopWordButton> _spawnedButtons = new();
    private readonly List<WordShopWordEntry> _availableEntries = new();
    private WordShopWordEntry _selectedEntry;
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
        RefreshBuyButtonState();
    }

    private void HandleWordsChanged()
    {
        RefreshShop();
    }

    private void RefreshShop()
    {
        BuildAvailableEntries();
        RebuildWordButtons();
        SelectFirstEntryIfNeeded();
        RefreshStateVisibility();
        RefreshBuyButtonState();
    }

    private void BuildAvailableEntries()
    {
        _availableEntries.Clear();

        if (_catalog == null || _player?.SpellWords == null)
            return;

        _catalog.GetAvailableEntries(_player.SpellWords, _availableEntries);

        if (_selectedEntry != null && !_availableEntries.Contains(_selectedEntry))
            _selectedEntry = null;
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

        for (int i = 0; i < _availableEntries.Count; i++)
        {
            var button = Instantiate(_wordButtonPrefab, _wordButtonContainer);
            button.Bind(_availableEntries[i]);
            button.Clicked += HandleWordClicked;
            _spawnedButtons.Add(button);
        }

        RefreshWordHighlights();
    }

    private void HandleWordClicked(WordShopWordEntry entry)
    {
        if (entry == null)
            return;

        _selectedEntry = entry;
        RefreshWordHighlights();
        RefreshBuyButtonState();
    }

    private void SelectFirstEntryIfNeeded()
    {
        if (_selectedEntry == null && _availableEntries.Count > 0)
            _selectedEntry = _availableEntries[0];

        RefreshWordHighlights();
    }

    private void RefreshWordHighlights()
    {
        for (int i = 0; i < _spawnedButtons.Count; i++)
        {
            var button = _spawnedButtons[i];
            if (button == null)
                continue;

            button.SetSelected(_selectedEntry == _availableEntries[i]);
        }
    }

    private void RefreshStateVisibility()
    {
        bool hasAnyWordsToBuy = _availableEntries.Count > 0;

        if (_wordButtonContainer != null)
            _wordButtonContainer.gameObject.SetActive(hasAnyWordsToBuy);

        if (_buyButton != null)
            _buyButton.gameObject.SetActive(hasAnyWordsToBuy);

        if (_emptyStateRoot != null)
            _emptyStateRoot.SetActive(!hasAnyWordsToBuy);

        if (!hasAnyWordsToBuy)
            _selectedEntry = null;
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
        if (_selectedEntry == null)
            return "Buy";

        return $"Buy ({_selectedEntry.MemoryLevelCost} memory levels)";
    }

    private bool CanBuySelectedWord()
    {
        if (_selectedEntry == null || _player?.Data == null)
            return false;

        return _player.Data.CurrentMemoryLevel >= _selectedEntry.MemoryLevelCost;
    }

    private void HandleBuyClicked()
    {
        if (_selectedEntry == null || _player?.Data == null || _player.SpellWords == null)
            return;

        if (!_selectedEntry.IsValid() || _selectedEntry.IsUnlocked(_player.SpellWords))
            return;

        if (!_player.Data.TrySpendMemoryLevels(_selectedEntry.MemoryLevelCost))
            return;

        if (!_selectedEntry.TryUnlock(_player.SpellWords))
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
