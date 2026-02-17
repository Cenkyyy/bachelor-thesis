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
    [SerializeField] private Player player;
    [SerializeField] private WordPanelView modifierPanel;
    [SerializeField] private WordPanelView elementPanel;
    [SerializeField] private WordPanelView formPanel;

    [Header("HUD")]
    [SerializeField] private TMP_Text currentPhraseText;

    [Header("Input")]
    [SerializeField] private WordSelectionKeys selectionKeys = default;
    [SerializeField] private KeyCode cancelKey = KeyCode.Escape;

    [Header("Casting")]
    [SerializeField, Min(0f)] private float castLockDurationSeconds = 0.2f;

    private SpellWordInventory _wordInventory;
    private SpellPhrase _currentPhrase;
    private CastingStage _stage;
    private bool _isCastLocked;

    public event Action<SpellPhrase> OnPhraseCompleted;

    private void Awake()
    {
        _wordInventory = player.GetComponent<SpellWordInventory>();
        _wordInventory.OnWordsChanged += RefreshPanels;

        RefreshPanels();
        SetPhraseTextVisible(false);
        ResetCastingState();
    }

    private void OnDestroy()
    {
        _wordInventory.OnWordsChanged -= RefreshPanels;
    }

    private void Update()
    {
        if (_isCastLocked || GameStateManager.IsGamePaused)
            return;

        if (PanelManager.Instance != null && PanelManager.Instance.BlocksGameplayInput)
            return;

        if (Input.GetKeyDown(cancelKey))
        {
            CancelCasting();
            return;
        }

        var pressedIndex = selectionKeys.TryGetPressedIndex();
        if (!pressedIndex.HasValue)
            return;

        TrySelectWord(pressedIndex.Value);
    }

    private void RefreshPanels()
    {
        modifierPanel.Bind(_wordInventory.UnlockedModifiers, CombatWordDefinitions.GetLabel);
        elementPanel.Bind(_wordInventory.UnlockedElements, CombatWordDefinitions.GetLabel);
        formPanel.Bind(_wordInventory.UnlockedForms, CombatWordDefinitions.GetLabel);
        ApplyStageVisuals();
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
        modifierPanel.SetPanelInteractable(false);
        elementPanel.SetPanelInteractable(false);
        formPanel.SetPanelInteractable(false);

        if (castLockDurationSeconds > 0f)
            yield return new WaitForSeconds(castLockDurationSeconds);

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
        var showModifierPanel = _stage == CastingStage.Modifier && !_isCastLocked;
        var showElementPanel = _stage == CastingStage.Element && !_isCastLocked;
        var showFormPanel = _stage == CastingStage.Form && !_isCastLocked;

        modifierPanel.gameObject.SetActive(showModifierPanel);
        elementPanel.gameObject.SetActive(showElementPanel);
        formPanel.gameObject.SetActive(showFormPanel);

        modifierPanel.SetPanelInteractable(showModifierPanel);
        elementPanel.SetPanelInteractable(showElementPanel);
        formPanel.SetPanelInteractable(showFormPanel);
    }

    private void UpdateText()
    {
        currentPhraseText.text = _currentPhrase.ToString();
    }

    private void SetPhraseTextVisible(bool visible)
    {
        currentPhraseText.gameObject.SetActive(visible);
    }
}