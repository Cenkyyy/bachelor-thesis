using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Visual layer that darkens the screen based on brightness coming from DayNightSystem.
/// Works with either a full-screen UI Image or a SpriteRenderer.
/// </summary>
[DisallowMultipleComponent]
public sealed class DarknessOverlay : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private DayNightSystem _time;
    [SerializeField] private Image _uiImage; 

    [Header("Appearance")]
    [SerializeField, Range(0f, 1f)] private float _maxAlpha = 0.65f;
    [SerializeField] private Color _tint = Color.black;

    private void Awake()
    {
        if (_time == null) _time = FindFirstObjectByType<DayNightSystem>();
        if (_uiImage == null) _uiImage = GetComponent<Image>();

        if (_uiImage != null) _uiImage.raycastTarget = false;
    }

    private void OnEnable()
    {
        if (_time != null)
            _time.OnBrightnessChanged += HandleBrightnessChanged;

        if (_time != null)
            HandleBrightnessChanged(_time.Brightness);
    }

    private void OnDisable()
    {
        if (_time != null)
            _time.OnBrightnessChanged -= HandleBrightnessChanged;
    }

    private void HandleBrightnessChanged(float brightness)
    {
        float alpha = Mathf.Clamp01((1f - brightness) * _maxAlpha);
        if (_uiImage != null)
        {
            var c = _tint; c.a = alpha;
            _uiImage.color = c;
        }
    }
}
