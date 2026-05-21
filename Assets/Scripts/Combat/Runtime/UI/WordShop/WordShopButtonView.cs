using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class WordShopButtonView : MonoBehaviour
{
    [SerializeField] private Button _button;
    [SerializeField] private TMP_Text _label;
    [SerializeField] private Image _background;
    [SerializeField] private Sprite _defaultSprite;
    [SerializeField] private Sprite _selectedSprite;

    private WordData _word;

    public event Action<WordData> Clicked;

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

    public void Bind(WordData word)
    {
        _word = word;

        if (_label != null)
            _label.text = word != null ? word.DisplayName : string.Empty;
    }

    public void SetSelected(bool selected)
    {
        _background.sprite = selected ? _selectedSprite : _defaultSprite;
    }

    private void HandleClicked()
    {
        Clicked?.Invoke(_word);
    }
}
