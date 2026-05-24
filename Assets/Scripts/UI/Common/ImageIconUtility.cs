using UnityEngine.UI;

/// <summary>
/// Shared helper for assigning item and UI sprites to icon images safely.
/// </summary>
public static class ImageIconUtility
{
    public static void SetIcon(Image image, UnityEngine.Sprite sprite)
    {
        if (image == null)
            return;

        image.sprite = sprite;
        image.enabled = sprite != null;
        image.preserveAspect = true;
    }
}
