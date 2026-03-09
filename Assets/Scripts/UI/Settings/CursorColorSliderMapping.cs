using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Created by ChatGPT to read or estimate colors from a reference image, mainly for the cursor color slider in the settings panel.
/// TODO: Generalize this for better mapping, maybe create a Util out of this to be inside Core folder for the project.
/// </summary>
public static class CursorColorSliderMapping
{
    public static Color GetColor(float normalizedValue, Image referenceImage, float fallbackSaturation, float fallbackValue)
    {
        float t = Mathf.Clamp01(normalizedValue);

        if (TrySampleGradientColor(referenceImage, t, out Color sampledColor))
        {
            return sampledColor;
        }

        return Color.HSVToRGB(t, fallbackSaturation, fallbackValue);
    }

    public static float EstimateSliderValue(Color color, Image referenceImage)
    {
        if (TryEstimateValueFromGradient(color, referenceImage, out float sampledValue))
        {
            return sampledValue;
        }

        Color.RGBToHSV(color, out float h, out _, out _);
        return h;
    }

    private static bool TrySampleGradientColor(Image referenceImage, float t, out Color sampledColor)
    {
        sampledColor = default;

        if (referenceImage == null)
            return false;

        Sprite sprite = referenceImage.sprite;
        if (sprite == null || sprite.texture == null)
            return false;

        Texture2D texture = sprite.texture;
        Rect rect = sprite.textureRect;

        float textureX = Mathf.Lerp(rect.xMin, rect.xMax - 1f, t);
        float textureY = rect.center.y;

        int px = Mathf.Clamp(Mathf.RoundToInt(textureX), 0, texture.width - 1);
        int py = Mathf.Clamp(Mathf.RoundToInt(textureY), 0, texture.height - 1);

        try
        {
            sampledColor = texture.GetPixel(px, py);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool TryEstimateValueFromGradient(Color color, Image referenceImage, out float value)
    {
        value = 0f;

        if (referenceImage == null)
            return false;

        Sprite sprite = referenceImage.sprite;
        if (sprite == null || sprite.texture == null)
            return false;

        Texture2D texture = sprite.texture;
        Rect rect = sprite.textureRect;

        int y = Mathf.Clamp(Mathf.RoundToInt(rect.center.y), 0, texture.height - 1);
        const int samples = 48;

        float bestT = 0f;
        float bestDistance = float.MaxValue;

        try
        {
            for (int i = 0; i < samples; i++)
            {
                float t = i / (samples - 1f);
                int x = Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(rect.xMin, rect.xMax - 1f, t)), 0, texture.width - 1);
                Color sample = texture.GetPixel(x, y);

                float dr = sample.r - color.r;
                float dg = sample.g - color.g;
                float db = sample.b - color.b;
                float distance = (dr * dr) + (dg * dg) + (db * db);

                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestT = t;
                }
            }

            value = bestT;
            return true;
        }
        catch
        {
            return false;
        }
    }
}
