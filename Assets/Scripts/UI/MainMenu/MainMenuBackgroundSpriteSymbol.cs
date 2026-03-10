using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class MainMenuBackgroundSpriteSymbol : MonoBehaviour
{
    private Image _image;
    private RectTransform _rectTransform;
    private Vector2 _startPosition;
    private Vector2 _endPosition;
    private float _durationSeconds;
    private float _fadeStartPercentageY;
    private float _fadeEndPercentageY;
    private AnimationCurve _riseCurve;
    private Rect _spawnRect;

    private float _elapsed;

    private void Awake()
    {
        _rectTransform = transform as RectTransform;
    }

    public void Initialize(
        Image image,
        Vector2 startPosition,
        Vector2 endPosition,
        float durationSeconds,
        float fadeStartPercentageY,
        float fadeEndPercentageY,
        AnimationCurve riseCurve,
        Rect spawnRect)
    {
        _image = image;
        _startPosition = startPosition;
        _endPosition = endPosition;
        _durationSeconds = Mathf.Max(0.05f, durationSeconds);
        _fadeStartPercentageY = Mathf.Clamp01(fadeStartPercentageY);
        _fadeEndPercentageY = Mathf.Clamp01(fadeEndPercentageY);
        _riseCurve = riseCurve == null ? AnimationCurve.Linear(0f, 0f, 1f, 1f) : riseCurve;
        _spawnRect = spawnRect;

        if (_rectTransform == null)
            _rectTransform = transform as RectTransform;
    }

    private void Update()
    {
        if (_rectTransform == null || _image == null)
            return;

        // Move the symbol based on the elapsed time and the rise curve
        _elapsed += Time.unscaledDeltaTime;
        var t = Mathf.Clamp01(_elapsed / _durationSeconds);
        var easedT = Mathf.Clamp01(_riseCurve.Evaluate(t));
        _rectTransform.anchoredPosition = Vector2.LerpUnclamped(_startPosition, _endPosition, easedT);

        // Compute the alpha based on the current Y position compared to the fade start and fade end percentages
        var percentageY = Mathf.InverseLerp(_spawnRect.yMin, _spawnRect.yMax, _rectTransform.anchoredPosition.y);
        var alpha = EvaluateAlpha(percentageY);

        var color = _image.color;
        color.a = alpha;
        _image.color = color;

        // Destroy the symbol if it is fully faded out or has exceeded its duration
        if (alpha <= 0f || t >= 1f)
            Destroy(gameObject);
    }

    private float EvaluateAlpha(float percentageY)
    {
        if (percentageY <= _fadeStartPercentageY)
            return 1f;

        var fadeSpan = Mathf.Max(0.001f, _fadeEndPercentageY - _fadeStartPercentageY);
        var fadeT = Mathf.Clamp01((percentageY - _fadeStartPercentageY) / fadeSpan);
        return 1f - fadeT;
    }
}
