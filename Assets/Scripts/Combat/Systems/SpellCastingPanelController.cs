using System;
using System.Collections;
using TMPro;
using UnityEngine;

public class SpellCastingPanelController : MonoBehaviour
{
    private enum CastingStage
    {
        Modifier,
        Element,
        Form
    }

    [Header("References")]
    [SerializeField] private Player _player;
    [SerializeField] private WordPanelView _modifierPanel;
    [SerializeField] private WordPanelView _elementPanel;
    [SerializeField] private WordPanelView _formPanel;

    [Header("HUD")]
    [SerializeField] private TMP_Text _currentPhraseText;

    [Header("Input")]
    [SerializeField] private SpellCastingKeybindsData _spellCastingKeys;

    [Header("Casting")]
    [SerializeField, Min(0f)] private float _castLockDurationSeconds = 0.2f;
    [SerializeField, Min(0f)] private float _autoCancelCastingAfterSeconds = 4f;

    private SpellWordInventory _wordInventory;
    private SpellPhrase _currentPhrase;
    private CastingStage _stage;
    private bool _isCastLocked;
    private bool _canInteractWithSpellcasting;
    private float _lastCastingProgressTimestamp;
    private bool _isInitialized;

    public event Action<SpellPhrase> OnPhraseCompleted;

    private void Start()
    {
        StartCoroutine(InitializeSpellCastingCoroutine());
    }

    private IEnumerator InitializeSpellCastingCoroutine()
    {
        while (!TryResolveDependencies())
            yield return null;

        _wordInventory.OnWordsChanged += RefreshPanels;

        if (_player != null && _player.Inventory != null)
        {
            _player.Inventory.OnHotbarSelectionChanged += HandleSelectedHotbarChanged;
            _player.Inventory.OnItemChanged += HandleInventoryItemChanged;
        }

        RefreshPanels();
        SetPhraseTextVisible(false);
        ResetCastingState();
        RefreshInteractionAvailability();
        _isInitialized = true;
    }

    private bool TryResolveDependencies()
    {
        if (_player != null && _wordInventory == null)
            _wordInventory = _player.SpellWords;

        return _player != null && _wordInventory != null;
    }

    private void OnDestroy()
    {
        if (_wordInventory != null)
            _wordInventory.OnWordsChanged -= RefreshPanels;

        if (_player != null && _player.Inventory != null)
        {
            _player.Inventory.OnHotbarSelectionChanged -= HandleSelectedHotbarChanged;
            _player.Inventory.OnItemChanged -= HandleInventoryItemChanged;
        }
    }

    private void Update()
    {
        if (!_isInitialized)
            return;

        if (_isCastLocked || GameStateManager.IsGamePaused || !_canInteractWithSpellcasting)
            return;

        if (PanelManager.Instance != null && PanelManager.Instance.BlocksGameplayInput)
            return;

        TryHandleAutoCancel();

        var pressedIndex = _spellCastingKeys.TryGetPressedIndex();
        if (!pressedIndex.HasValue)
            return;

        TrySelectWord(pressedIndex.Value);
    }

    private void RefreshPanels()
    {
        _modifierPanel.Bind(_wordInventory.UnlockedModifiers, CombatWordDefinitions.GetLabel);
        _elementPanel.Bind(_wordInventory.UnlockedElements, CombatWordDefinitions.GetLabel);
        _formPanel.Bind(_wordInventory.UnlockedForms, CombatWordDefinitions.GetLabel);
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

        // all slots disabled while the spell commit is active
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

    public bool TryCancelActiveCasting()
    {
        if (!IsCastingInProgress())
            return false;

        CancelCasting();
        return true;
    }

    private bool IsCastingInProgress()
    {
        return _currentPhrase.Modifier.HasValue ||
               _currentPhrase.Element.HasValue ||
               _currentPhrase.Form.HasValue;
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
