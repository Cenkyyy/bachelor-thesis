using UnityEngine.UI;

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
