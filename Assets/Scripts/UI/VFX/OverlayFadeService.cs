using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public sealed class OverlayFadeService
{
    private readonly CanvasGroup _overlayCanvasGroup;
    private readonly float _maxTransitionDeltaTime;

    public OverlayFadeService(CanvasGroup overlayCanvasGroup, float maxTransitionDeltaTime)
    {
        _overlayCanvasGroup = overlayCanvasGroup;
        _maxTransitionDeltaTime = Mathf.Max(0.001f, maxTransitionDeltaTime);
    }

    public IEnumerator FadeIn(float duration)
    {
        yield return Fade(0f, 1f, duration);
    }

    public IEnumerator FadeOut(float duration)
    {
        yield return Fade(1f, 0f, duration);
    }

    public void ApplyOverlayVisual(Image overlayImage, Sprite overlaySprite, Color overlayColorWhenSpriteAssigned, Color overlayFallbackColor)
    {
        if (overlayImage == null)
            return;

        bool hasSprite = overlaySprite != null;
        overlayImage.sprite = overlaySprite;
        overlayImage.type = Image.Type.Simple;
        overlayImage.color = hasSprite ? overlayColorWhenSpriteAssigned : overlayFallbackColor;
    }

    public void SetAlpha(float alpha)
    {
        if (_overlayCanvasGroup == null)
            return;

        _overlayCanvasGroup.alpha = Mathf.Clamp01(alpha);
    }

    private IEnumerator Fade(float fromAlpha, float toAlpha, float duration)
    {
        if (_overlayCanvasGroup == null)
            yield break;

        if (duration <= 0f)
        {
            _overlayCanvasGroup.alpha = toAlpha;
            yield break;
        }

        float t = 0f;
        _overlayCanvasGroup.alpha = fromAlpha;

        while (t < duration)
        {
            t += Mathf.Min(Time.unscaledDeltaTime, _maxTransitionDeltaTime);
            float normalized = Mathf.Clamp01(t / duration);
            _overlayCanvasGroup.alpha = Mathf.Lerp(fromAlpha, toAlpha, normalized);
            yield return null;
        }

        _overlayCanvasGroup.alpha = toAlpha;
    }
}
