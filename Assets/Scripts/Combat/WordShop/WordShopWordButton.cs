using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class WordShopWordButton : MonoBehaviour
{
    [SerializeField] private Button _button;
    [SerializeField] private TMP_Text _label;
    [SerializeField] private Image _background;
    [SerializeField] private Sprite _defaultSprite;
    [SerializeField] private Sprite _selectedSprite;

    private WordShopWordEntry _entry;

    public event Action<WordShopWordEntry> Clicked;

    private void Awake()
    {
        if (_button != null)
            _button.onClick.AddListener(HandleClicked);
    }

    private void OnDestroy()
    {
        if (_button != null)
            _button.onClick.RemoveListener(HandleClicked);
    }

    public void Bind(WordShopWordEntry entry)
    {
        _entry = entry;

        if (_label != null)
            _label.text = entry != null ? entry.GetLabel() : string.Empty;
    }

    public void SetSelected(bool selected)
    {
        _background.sprite = selected ? _selectedSprite : _defaultSprite;
    }

    private void HandleClicked()
    {
        Clicked?.Invoke(_entry);
    }
}
