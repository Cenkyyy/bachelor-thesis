using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class StarterWordSelectionController : MonoBehaviour
{
    [Serializable]
    public sealed class StarterWordSelectionResult
    {
        public List<ModifierWordType> Modifiers = new();
        public List<ElementWordType> Elements = new();
        public List<FormWordType> Forms = new();
    }

    [Header("Settings")]
    [SerializeField] private bool _enableWordSelection = true;

    [Header("Data")]
    [SerializeField] private StarterWordSelectionData _starterWordSelectionData;

    [Header("Root")]
    [SerializeField] private GameObject _root;
    [SerializeField] private Image _backgroundImage;

    [Header("Panels")]
    [SerializeField] private StarterWordCategoryPanel _modifierPanel;
    [SerializeField] private StarterWordCategoryPanel _elementPanel;
    [SerializeField] private StarterWordCategoryPanel _formPanel;

    [Header("Controls")]
    [SerializeField] private Button _continueButton;

    public bool WasConfirmed { get; private set; }
    public bool IsEnabled => _enableWordSelection;
    public StarterWordSelectionResult Result { get; private set; }

    private void Awake()
    {
        SetVisible(false);

        if (_continueButton != null)
            _continueButton.onClick.AddListener(HandleContinueClicked);

        if (_modifierPanel != null)
            _modifierPanel.SelectionChanged += RefreshContinueInteractable;

        if (_elementPanel != null)
            _elementPanel.SelectionChanged += RefreshContinueInteractable;

        if (_formPanel != null)
            _formPanel.SelectionChanged += RefreshContinueInteractable;
    }

    private void OnDestroy()
    {
        if (_continueButton != null)
            _continueButton.onClick.RemoveListener(HandleContinueClicked);

        if (_modifierPanel != null)
            _modifierPanel.SelectionChanged -= RefreshContinueInteractable;

        if (_elementPanel != null)
            _elementPanel.SelectionChanged -= RefreshContinueInteractable;

        if (_formPanel != null)
            _formPanel.SelectionChanged -= RefreshContinueInteractable;
    }

    public IEnumerator RunSelectionCoroutine(Sprite backgroundSprite, Color backgroundColor)
    {
        if (!IsEnabled || _starterWordSelectionData == null)
            yield break;

        var player = FindFirstObjectByType<Player>();
        if (player == null || player.SpellWords == null)
            yield break;

        Show(backgroundSprite, backgroundColor);

        while (!WasConfirmed)
            yield return null;

        var result = Result;
        player.SpellWords.SetUnlockedWords(result.Modifiers, result.Elements, result.Forms);
        Hide();
    }

    public void Show(Sprite backgroundSprite, Color backgroundColor)
    {
        WasConfirmed = false;
        Result = null;

        ApplyBackground(backgroundSprite, backgroundColor);

        _modifierPanel.BuildButtonOptions("modifier", _starterWordSelectionData.ModifierRule.RequiredCount, _starterWordSelectionData.ModifierRule.FixedWords, _starterWordSelectionData.ModifierRule.ChoiceWords, word => word.ToString());
        _elementPanel.BuildButtonOptions("element", _starterWordSelectionData.ElementRule.RequiredCount, _starterWordSelectionData.ElementRule.FixedWords, _starterWordSelectionData.ElementRule.ChoiceWords, word => word.ToString());
        _formPanel.BuildButtonOptions("form", _starterWordSelectionData.FormRule.RequiredCount, _starterWordSelectionData.FormRule.FixedWords, _starterWordSelectionData.FormRule.ChoiceWords, word => word.ToString());

        RefreshContinueInteractable();
        SetVisible(true);
    }

    public void Hide()
    {
        SetVisible(false);
    }

    private void HandleContinueClicked()
    {
        if (!CanContinue())
            return;

        Result = new StarterWordSelectionResult
        {
            Modifiers = _modifierPanel.GetSelectedWords<ModifierWordType>(),
            Elements = _elementPanel.GetSelectedWords<ElementWordType>(),
            Forms = _formPanel.GetSelectedWords<FormWordType>(),
        };

        WasConfirmed = true;
    }

    private bool CanContinue()
    {
        return _modifierPanel.IsRequirementSatisfied && _elementPanel.IsRequirementSatisfied && _formPanel.IsRequirementSatisfied;
    }

    private void RefreshContinueInteractable()
    {
        if (_continueButton != null)
            _continueButton.interactable = CanContinue();
    }

    private void ApplyBackground(Sprite backgroundSprite, Color fallbackColor)
    {
        if (_backgroundImage == null)
            return;

        _backgroundImage.sprite = backgroundSprite;
        _backgroundImage.color = backgroundSprite != null ? Color.white : fallbackColor;
    }

    private void SetVisible(bool visible)
    {
        if (_root != null)
            _root.SetActive(visible);
    }
}
