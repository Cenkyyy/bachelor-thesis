using UnityEngine;

public class HotbarSlot : Slot
{
    [Header("Hotbar Highlight")]
    [SerializeField] Sprite highlightedBackgroundSprite;

    /// <summary>
    /// Highlights this hotbar slot.
    /// </summary>
    public void HighlightSelected()
    {
        if (backgroundImage != null && highlightedBackgroundSprite != null)
            backgroundImage.sprite = highlightedBackgroundSprite;
    }

    /// <summary>
    /// Resets to default background.
    /// </summary>
    public void SetToDefault()
    { 
        if (backgroundImage != null && backgroundSprite != null)
            backgroundImage.sprite = backgroundSprite;
    }
}
