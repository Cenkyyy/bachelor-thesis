using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Selectable UI button for one starter spell word option.
/// </summary>
public sealed class StarterWordOptionButton : MonoBehaviour
{
    [SerializeField] private Button _button;
    [SerializeField] private Image _backgroundImage;
    [SerializeField] private TMP_Text _label;
    [SerializeField] private Sprite _normalSprite;
    [SerializeField] private Sprite _selectedSprite;

    public bool IsFixed { get; private set; }
    public bool IsSelected { get; private set; }
    public WordData Word { get; private set; }

    public event Action<StarterWordOptionButton> Clicked;

    private void Awake()
    {
        if (_button != null)
            _button.onClick.AddListener(HandleClick);
    }

    private void OnDestroy()
    {
        if (_button != null)
            _button.onClick.RemoveListener(HandleClick);
    }

    public void Bind(WordData word, string label, bool isFixed, bool isSelected)
    {
        Word = word;
        IsFixed = isFixed;
        IsSelected = isSelected;
        _label.text = label;

        if (_button != null)
            _button.interactable = !IsFixed;

        RefreshVisual();
    }

    public void SetSelected(bool selected)
    {
        IsSelected = selected;
        RefreshVisual();
    }

    private void HandleClick()
    {
        Clicked?.Invoke(this);
    }

    private void RefreshVisual()
    {
        if (_backgroundImage != null)
            _backgroundImage.sprite = IsSelected ? _selectedSprite : _normalSprite;
    }
}
