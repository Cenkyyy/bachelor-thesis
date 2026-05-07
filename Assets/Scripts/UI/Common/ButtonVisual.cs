using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Applies shared button hover, selection, and click feedback.
/// </summary>
[RequireComponent(typeof(Image), typeof(Button))]
public sealed class ButtonVisual : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("Refs")]
    [SerializeField] private Image _targetImage;

    [Header("Sprites")]
    [SerializeField] private Sprite _normalSprite;
    [SerializeField] private Sprite _highlightSprite;
    [SerializeField] private bool _allowHighlightSprite = true;

    private bool _isHovered;
    private bool _isSelected;

    private void Awake()
    {
        ApplyVisual();
    }

    private void OnValidate()
    {
        ApplyVisual();
    }

    public void SetSelected(bool selected)
    {
        _isSelected = selected;
        ApplyVisual();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayUiHoverSfx();

        _isHovered = true;
        ApplyVisual();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _isHovered = false;
        ApplyVisual();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayUiClickSfx();
    }

    private void ApplyVisual()
    {
        if (_targetImage == null)
            return;

        bool useHighlight = _allowHighlightSprite && _highlightSprite != null && (_isSelected || _isHovered);
        _targetImage.sprite = useHighlight ? _highlightSprite : _normalSprite;
    }
}
