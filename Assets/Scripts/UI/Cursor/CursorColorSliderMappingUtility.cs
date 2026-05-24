using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Converts cursor color slider values to colors and estimates slider values from current colors.
/// </summary>
public static class CursorColorSliderMappingUtility
{
    public static Color GetColor(float normalizedValue, Image referenceImage, float fallbackSaturation, float fallbackValue)
    {
        // tries to get the color from the slider's gradient image based on the normalized value
        float t = Mathf.Clamp01(normalizedValue);

        if (TrySampleGradientColor(referenceImage, t, out Color sampledColor))
        {
            return sampledColor;
        }

        return Color.HSVToRGB(t, fallbackSaturation, fallbackValue);
    }

    public static float EstimateSliderValue(Color color, Image referenceImage)
    {
        // tries to estimate the slider position/value based on the given color and finding the nearest match in the gradient
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

        var sprite = referenceImage.sprite;
        if (sprite == null || sprite.texture == null)
            return false;

        var texture = sprite.texture;
        var rect = sprite.textureRect;

        // calculate the texture's pixel color position, use center for Y axis and interpolate across the X axis based on t
        float textureX = Mathf.Lerp(rect.xMin, rect.xMax - 1f, t);
        float textureY = rect.center.y;

        int px = Mathf.Clamp(Mathf.RoundToInt(textureX), 0, texture.width - 1);
        int py = Mathf.Clamp(Mathf.RoundToInt(textureY), 0, texture.height - 1);

        // read the pixel color
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

        var sprite = referenceImage.sprite;
        if (sprite == null || sprite.texture == null)
            return false;

        var texture = sprite.texture;
        var rect = sprite.textureRect;

        // sample multiple (48) points along the gradient line and find the one closest to the target color
        int y = Mathf.Clamp(Mathf.RoundToInt(rect.center.y), 0, texture.height - 1);
        const int samples = 48;

        float bestT = 0f;
        float bestDistance = float.MaxValue;

        try
        {
            for (int i = 0; i < samples; i++)
            {
                // compute the RGB distance between the sampled color and the target color and return the closest one as the best match
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
