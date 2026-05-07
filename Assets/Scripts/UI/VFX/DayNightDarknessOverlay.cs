using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Visual layer that darkens the screen based on brightness coming from DayNightSystem.
/// </summary>
[DisallowMultipleComponent]
public sealed class DayNightDarknessOverlay : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private DayNightSystem _time;
    [SerializeField] private Image _uiImage; 

    [Header("Appearance")]
    [SerializeField, Range(0f, 1f)] private float _maxAlpha = 0.65f;
    [SerializeField] private Color _tint = Color.black;

    private void OnEnable()
    {
        if (_time != null) 
        {
            _time.OnBrightnessChanged += HandleBrightnessChanged;
            HandleBrightnessChanged(_time.Brightness);
        }
    }

    private void OnDisable()
    {
        if (_time != null) 
        {
            _time.OnBrightnessChanged -= HandleBrightnessChanged;
        }
    }

    private void HandleBrightnessChanged(float brightness)
    {
        float alpha = Mathf.Clamp01((1f - brightness) * _maxAlpha);
        if (_uiImage != null)
        {
            _tint.a = alpha;
            _uiImage.color = _tint;
        }
    }
}
