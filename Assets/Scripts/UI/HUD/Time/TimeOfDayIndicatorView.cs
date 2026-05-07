using System.Collections;
using UnityEngine;

/// <summary>
/// Moves the HUD time indicator across a bar that starts at 06:00 and wraps back after 05:59.
/// </summary>
[DisallowMultipleComponent]
public sealed class TimeOfDayIndicatorView : MonoBehaviour
{
    private const float DayStartTime01 = 6f / 24f;

    [Header("References")]
    [SerializeField] private DayNightSystem _time;
    [SerializeField] private RectTransform _progressIndicatorImage;
    [SerializeField] private RectTransform _barImage;

    [Header("Debug (Inspector Debug Mode)")]
    [SerializeField] private string _currentTime = "06:00";
    [SerializeField, Range(0f, 1f)] private float _timeProgress;
    private Coroutine _initialRefreshCoroutine;

    private void OnEnable()
    {
        if (_time != null)
        {
            _time.OnMinuteChanged += HandleMinuteChanged;
            _initialRefreshCoroutine = StartCoroutine(RefreshIndicatorNextFrameCoroutine());
        }
    }

    private void OnDisable()
    {
        if (_time != null)
            _time.OnMinuteChanged -= HandleMinuteChanged;

        if (_initialRefreshCoroutine != null)
        {
            StopCoroutine(_initialRefreshCoroutine);
            _initialRefreshCoroutine = null;
        }
    }

    private IEnumerator RefreshIndicatorNextFrameCoroutine()
    {
        yield return null;
        HandleMinuteChanged(_time.Hour, _time.Minute);
        _initialRefreshCoroutine = null;
    }

    private void HandleMinuteChanged(int _, int __)
    {
        if (_time == null)
            return;

        _currentTime = _time.GetTimeString();
        _timeProgress = GetIndicatorPosition01(_time.Time01);
        UpdateIndicatorPosition(_timeProgress);
    }

    private static float GetIndicatorPosition01(float time01)
    {
        return Mathf.Repeat(time01 - DayStartTime01, 1f);
    }

    private void UpdateIndicatorPosition(float progress01)
    {
        if (_progressIndicatorImage == null || _barImage == null)
            return;

        float barWidth = _barImage.rect.width;
        if (barWidth <= 0f)
            return;

        float x = -barWidth * 0.5f + barWidth * Mathf.Clamp01(progress01);

        Vector2 anchoredPosition = _progressIndicatorImage.anchoredPosition;
        anchoredPosition.x = x;
        _progressIndicatorImage.anchoredPosition = anchoredPosition;
    }
}
