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
        public List<ModifierWord> Modifiers = new();
        public List<ElementWord> Elements = new();
        public List<FormWord> Forms = new();
    }

    [Header("Settings")]
    [SerializeField] private bool _enableWordSelection = true;

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

    public IEnumerator RunSelectionCoroutine(StarterWordSelectionData data, Player player, Sprite backgroundSprite, Color backgroundColor)
    {
        if (!IsEnabled || data == null)
            yield break;

        if (player == null)
            player = FindFirstObjectByType<Player>();

        if (player == null || player.SpellWords == null)
            yield break;

        Show(data, backgroundSprite, backgroundColor);

        while (!WasConfirmed)
            yield return null;

        var result = Result;
        player.SpellWords.SetUnlockedWords(result.Modifiers, result.Elements, result.Forms);
        Hide();
    }

    public void Show(StarterWordSelectionData data, Sprite backgroundSprite, Color backgroundColor)
    {
        WasConfirmed = false;
        Result = null;

        ApplyBackground(backgroundSprite, backgroundColor);

        _modifierPanel.BuildButtonOptions("modifier", data.ModifierRule.RequiredCount, data.ModifierRule.FixedWords, data.ModifierRule.ChoiceWords, word => word.ToString());
        _elementPanel.BuildButtonOptions("element", data.ElementRule.RequiredCount, data.ElementRule.FixedWords, data.ElementRule.ChoiceWords, word => word.ToString());
        _formPanel.BuildButtonOptions("form", data.FormRule.RequiredCount, data.FormRule.FixedWords, data.FormRule.ChoiceWords, word => word.ToString());

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
            Modifiers = _modifierPanel.GetSelectedWords<ModifierWord>(),
            Elements = _elementPanel.GetSelectedWords<ElementWord>(),
            Forms = _formPanel.GetSelectedWords<FormWord>(),
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
