using UnityEngine;

public sealed class HotbarSlotView : InventorySlotView
{
    [Header("Hotbar Highlight")]
    [SerializeField] private Sprite _highlightedBackgroundSprite;

    /// <summary>
    /// Highlights this hotbar slot.
    /// </summary>
    public void HighlightSelected()
    {
        if (backgroundImage != null && _highlightedBackgroundSprite != null)
        {
            backgroundImage.sprite = _highlightedBackgroundSprite;
        }
    }

    /// <summary>
    /// Resets to default background.
    /// </summary>
    public void SetToDefault()
    { 
        if (backgroundImage != null && backgroundSprite != null)
        {
            backgroundImage.sprite = backgroundSprite;
        }
    }
}
