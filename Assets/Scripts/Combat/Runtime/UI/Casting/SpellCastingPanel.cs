using System;
using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// Handles spell word selection UI, builds the current spell phrase, and emits completed phrases.
/// </summary>
public sealed class SpellCastingPanel : MonoBehaviour
{
    /// <summary>
    /// Represents the next spell word category the player must choose.
    /// </summary>
    private enum CastingStage
    {
        Modifier,
        Element,
        Form
    }

    [Header("References")]
    [SerializeField] private Player _player;
    [SerializeField] private SpellWordListView _modifierPanel;
    [SerializeField] private SpellWordListView _elementPanel;
    [SerializeField] private SpellWordListView _formPanel;

    [Header("HUD")]
    [SerializeField] private TMP_Text _currentPhraseText;
    [SerializeField] private WorldTextPopupController _feedbackPopup;
    [SerializeField] private string _weaponRequiredMessage = "Arm a weapon to cast spells";
    [SerializeField] private Color _weaponRequiredMessageColor = Color.white;

    [Header("Input")]
    [SerializeField] private SpellCastingInputBindingsData _spellCastingKeys;

    [Header("Casting")]
    [SerializeField, Min(0f)] private float _castLockDurationSeconds = 0.2f;
    [SerializeField, Min(0f)] private float _autoCancelCastingAfterSeconds = 4f;

    private PlayerSpellWordInventory _wordInventory;
    private SpellPhrase _currentPhrase;
    private CastingStage _stage;
    private bool _isCastLocked;
    private bool _canInteractWithSpellcasting;
    private float _lastCastingProgressTimestamp;
    private bool _isWordInventoryInitialized;
    private bool _isPlayerInventoryInitialized;

    public event Action<SpellPhrase> OnPhraseCompleted;

    private void Awake()
    {
        TryResolveDependencies();
        SetPhraseTextVisible(false);
    }

    private void OnEnable()
    {
        TryResolveDependencies();

        if (_player != null && _player.Inventory != null)
        {
            _player.Inventory.OnHotbarSelectionChanged += HandleSelectedHotbarChanged;
            _player.Inventory.OnItemChanged += HandleInventoryItemChanged;
        }

        if (_wordInventory == null)
            return;

        _wordInventory.OnWordsInitialized += HandleWordsInitialized;
        _wordInventory.OnWordsChanged += RefreshPanels;

        if (_wordInventory.HasInitializedWords)
            HandleWordsInitialized();
    }

    private void OnDisable()
    {
        if (_wordInventory != null)
        {
            _wordInventory.OnWordsInitialized -= HandleWordsInitialized;
            _wordInventory.OnWordsChanged -= RefreshPanels;
        }

        if (_player != null && _player.Inventory != null)
        {
            _player.Inventory.OnHotbarSelectionChanged -= HandleSelectedHotbarChanged;
            _player.Inventory.OnItemChanged -= HandleInventoryItemChanged;
        }
    }

    private void Update()
    {
        if (!_isWordInventoryInitialized)
            return;

        if (!_isPlayerInventoryInitialized)
        {
            HandleInventoryInitialized();
            return;
        }

        if (_isCastLocked || GameStateManager.IsGamePaused)
            return;

        if (PanelManager.Instance != null && PanelManager.Instance.BlocksGameplayInput)
            return;

        TryHandleAutoCancel();

        var pressedIndex = _spellCastingKeys.TryGetPressedIndex();
        if (!pressedIndex.HasValue)
            return;

        if (!_canInteractWithSpellcasting)
        {
            _feedbackPopup?.ShowMessage(_weaponRequiredMessage, _weaponRequiredMessageColor, null);
            return;
        }

        TrySelectWord(pressedIndex.Value);
    }

    public bool TryCancelActiveCasting()
    {
        if (!IsCastingInProgress())
            return false;

        CancelCasting();
        return true;
    }

    private void HandleWordsInitialized()
    {
        if (_wordInventory == null || _isWordInventoryInitialized)
            return;

        _wordInventory.OnWordsInitialized -= HandleWordsInitialized;

        RefreshPanels();
        ResetCastingState();
        RefreshInteractionAvailability();
        _isWordInventoryInitialized = true;
    }

    private void HandleInventoryInitialized()
    {
        if (_isPlayerInventoryInitialized || _player == null || _player.Inventory == null)
            return;

        _player.Inventory.OnHotbarSelectionChanged -= HandleSelectedHotbarChanged;
        _player.Inventory.OnItemChanged -= HandleInventoryItemChanged;
        _player.Inventory.OnHotbarSelectionChanged += HandleSelectedHotbarChanged;
        _player.Inventory.OnItemChanged += HandleInventoryItemChanged;
        RefreshInteractionAvailability();
        _isPlayerInventoryInitialized = true;
    }

    private bool TryResolveDependencies()
    {
        if (_player != null && _wordInventory == null)
            _wordInventory = _player.SpellWords;

        if (_player != null && _feedbackPopup == null)
            _feedbackPopup = _player.GetComponent<WorldTextPopupController>();

        return _player != null && _wordInventory != null;
    }

    private void RefreshPanels()
    {
        _modifierPanel.Bind(_wordInventory.UnlockedModifiers, word => word.DisplayName);
        _elementPanel.Bind(_wordInventory.UnlockedElements, word => word.DisplayName);
        _formPanel.Bind(_wordInventory.UnlockedForms, word => word.DisplayName);
        RefreshInteractionAvailability();
        ApplyStageVisuals();
    }

    private void HandleSelectedHotbarChanged(int _)
    {
        RefreshInteractionAvailability();
    }

    private void HandleInventoryItemChanged(int changedIndex)
    {
        if (_player == null || _player.Inventory == null)
            return;

        if (changedIndex != _player.Inventory.SelectedHotbarIndex)
            return;

        RefreshInteractionAvailability();
    }

    private void RefreshInteractionAvailability()
    {
        var canInteractNow = IsHoldingWeaponItem();
        if (_canInteractWithSpellcasting == canInteractNow)
            return;

        _canInteractWithSpellcasting = canInteractNow;

        if (!_canInteractWithSpellcasting)
            CancelCasting();

        ApplyStageVisuals();
    }

    private bool IsHoldingWeaponItem()
    {
        if (_player == null || _player.Inventory == null)
            return false;

        var selectedItem = _player.Inventory.GetItemAt(_player.Inventory.SelectedHotbarIndex);
        return selectedItem.Item is WeaponItemData;
    }

    private void TrySelectWord(int index)
    {
        if (index < 0)
            return;

        switch (_stage)
        {
            case CastingStage.Modifier:
                if (index >= _wordInventory.UnlockedModifiers.Count)
                    return;

                _currentPhrase.SetModifier(_wordInventory.UnlockedModifiers[index]);
                _stage = CastingStage.Element;
                SetPhraseTextVisible(true);
                break;

            case CastingStage.Element:
                if (index >= _wordInventory.UnlockedElements.Count)
                    return;

                _currentPhrase.SetElement(_wordInventory.UnlockedElements[index]);
                _stage = CastingStage.Form;
                break;

            case CastingStage.Form:
                if (index >= _wordInventory.UnlockedForms.Count)
                    return;

                _currentPhrase.SetForm(_wordInventory.UnlockedForms[index]);
                CompletePhrase();
                return;
        }

        RegisterCastingProgress();
        ApplyStageVisuals();
        UpdateText();
    }

    private void CompletePhrase()
    {
        UpdateText();

        if (_currentPhrase.IsComplete)
        {
            OnPhraseCompleted?.Invoke(_currentPhrase);
            StartCoroutine(CastLockRoutine());
            return;
        }

        ResetCastingState();
    }

    private IEnumerator CastLockRoutine()
    {
        _isCastLocked = true;

        _modifierPanel.SetPanelInteractable(false);
        _elementPanel.SetPanelInteractable(false);
        _formPanel.SetPanelInteractable(false);

        if (_castLockDurationSeconds > 0f)
            yield return new WaitForSeconds(_castLockDurationSeconds);

        _isCastLocked = false;
        ResetCastingState();
    }

    private void CancelCasting()
    {
        ResetCastingState();
    }

    private void TryHandleAutoCancel()
    {
        if (!IsCastingInProgress())
            return;

        if (Time.unscaledTime - _lastCastingProgressTimestamp < _autoCancelCastingAfterSeconds)
            return;

        CancelCasting();
    }

    private bool IsCastingInProgress()
    {
        return _currentPhrase.Modifier != null ||
               _currentPhrase.Element != null ||
               _currentPhrase.Form != null;
    }

    private void RegisterCastingProgress()
    {
        _lastCastingProgressTimestamp = Time.unscaledTime;
    }

    private void ResetCastingState()
    {
        _currentPhrase.Clear();
        _stage = CastingStage.Modifier;

        SetPhraseTextVisible(false);
        ApplyStageVisuals();
        UpdateText();
    }

    private void ApplyStageVisuals()
    {
        var showModifierPanel = _stage == CastingStage.Modifier;
        var showElementPanel = _stage == CastingStage.Element;
        var showFormPanel = _stage == CastingStage.Form;

        _modifierPanel.gameObject.SetActive(showModifierPanel);
        _elementPanel.gameObject.SetActive(showElementPanel);
        _formPanel.gameObject.SetActive(showFormPanel);

        _modifierPanel.SetPanelInteractable(showModifierPanel && !_isCastLocked && _canInteractWithSpellcasting);
        _elementPanel.SetPanelInteractable(showElementPanel && !_isCastLocked && _canInteractWithSpellcasting);
        _formPanel.SetPanelInteractable(showFormPanel && !_isCastLocked && _canInteractWithSpellcasting);
    }

    private void UpdateText()
    {
        _currentPhraseText.text = _currentPhrase.ToString();
    }

    private void SetPhraseTextVisible(bool visible)
    {
        _currentPhraseText.gameObject.SetActive(visible);
    }
}
