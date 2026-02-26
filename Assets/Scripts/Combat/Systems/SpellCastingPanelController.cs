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

    [Serializable]
    private struct WordSelectionKeys
    {
        public KeyCode Slot1;
        public KeyCode Slot2;
        public KeyCode Slot3;
        public KeyCode Slot4;
        public KeyCode Slot5;

        public int? TryGetPressedIndex()
        {
            if (Input.GetKeyDown(Slot1)) return 0;
            if (Input.GetKeyDown(Slot2)) return 1;
            if (Input.GetKeyDown(Slot3)) return 2;
            if (Input.GetKeyDown(Slot4)) return 3;
            if (Input.GetKeyDown(Slot5)) return 4;
            return null;
        }
    }

    [Header("References")]
    [SerializeField] private Player _player;
    [SerializeField] private WordPanelView _modifierPanel;
    [SerializeField] private WordPanelView _elementPanel;
    [SerializeField] private WordPanelView _formPanel;

    [Header("HUD")]
    [SerializeField] private TMP_Text _currentPhraseText;

    [Header("Input")]
    [SerializeField] private WordSelectionKeys _selectionKeys = default;
    [SerializeField] private KeyCode _cancelKey = KeyCode.Escape;

    [Header("Casting")]
    [SerializeField, Min(0f)] private float _castLockDurationSeconds = 0.2f;

    private SpellWordInventory _wordInventory;
    private SpellPhrase _currentPhrase;
    private CastingStage _stage;
    private bool _isCastLocked;
    private bool _canInteractWithSpellcasting;

    public event Action<SpellPhrase> OnPhraseCompleted;

    private void Awake()
    {
        _wordInventory = _player.GetComponent<SpellWordInventory>();
        _wordInventory.OnWordsChanged += RefreshPanels;

        RefreshPanels();
        SetPhraseTextVisible(false);
        ResetCastingState();
        RefreshInteractionAvailability();
    }

    private void Start()
    {
        if (_player != null && _player.Inventory != null)
        {
            _player.Inventory.OnHotbarSelectionChanged += HandleSelectedHotbarChanged;
            _player.Inventory.OnItemChanged += HandleInventoryItemChanged;
        }
    }

    private void OnDestroy()
    {
        _wordInventory.OnWordsChanged -= RefreshPanels;

        if (_player != null && _player.Inventory != null)
        {
            _player.Inventory.OnHotbarSelectionChanged -= HandleSelectedHotbarChanged;
            _player.Inventory.OnItemChanged -= HandleInventoryItemChanged;
        }
    }

    private void Update()
    {
        if (_isCastLocked || GameStateManager.IsGamePaused || !_canInteractWithSpellcasting)
            return;

        if (PanelManager.Instance != null && PanelManager.Instance.BlocksGameplayInput)
            return;

        if (Input.GetKeyDown(_cancelKey))
        {
            CancelCasting();
            return;
        }

        var pressedIndex = _selectionKeys.TryGetPressedIndex();
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
        return selectedItem.Item is WeaponItem;
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
