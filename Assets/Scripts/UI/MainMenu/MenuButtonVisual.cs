using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class MenuButtonVisual : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
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
        _isHovered = true;
        ApplyVisual();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _isHovered = false;
        ApplyVisual();
    }

    private void ApplyVisual()
    {
        if (_targetImage == null)
            return;

        bool useHighlight = _allowHighlightSprite && _highlightSprite != null && (_isSelected || _isHovered);
        _targetImage.sprite = useHighlight ? _highlightSprite : _normalSprite;
    }
}
