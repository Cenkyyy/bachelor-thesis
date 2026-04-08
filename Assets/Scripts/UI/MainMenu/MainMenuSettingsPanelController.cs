using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// This class was created using ChatGPT, mainly for reading colors from an cursor color reference image 
/// and applying them to the cursor, as well as syncing the slider value with the current cursor color on enable.
/// </summary>
public sealed class MainMenuSettingsPanelController : MonoBehaviour
{
    [Header("Audio")]
    [SerializeField] private Slider _audioSlider;

    [Header("Cursor")]
    [SerializeField] private Slider _cursorColorSlider;
    [SerializeField] private Image _cursorColorReferenceImage;
    [SerializeField, Range(0f, 1f)] private float _fallbackSaturation = 1f;
    [SerializeField, Range(0f, 1f)] private float _fallbackValue = 1f;

    private void OnEnable()
    {
        if (_audioSlider != null)
        {
            _audioSlider.SetValueWithoutNotify(AudioListener.volume);
            _audioSlider.onValueChanged.AddListener(OnAudioChanged);
        }

        if (_cursorColorSlider != null)
        {
            _cursorColorSlider.onValueChanged.AddListener(OnCursorColorChanged);
        }

        SyncCursorSliderFromCurrentColor();
    }

    private void OnDisable()
    {
        if (_audioSlider != null)
        {
            _audioSlider.onValueChanged.RemoveListener(OnAudioChanged);
        }

        if (_cursorColorSlider != null)
        {
            _cursorColorSlider.onValueChanged.RemoveListener(OnCursorColorChanged);
        }
    }

    private void OnAudioChanged(float volume)
    {
        AudioListener.volume = Mathf.Clamp01(volume);
    }

    private void OnCursorColorChanged(float value)
    {
        var cursor = CustomCursorController.Instance;
        if (cursor == null)
            return;

        cursor.ApplyFillColor(CursorColorSliderMapping.GetColor(value, _cursorColorReferenceImage, _fallbackSaturation, _fallbackValue));
    }

    private void SyncCursorSliderFromCurrentColor()
    {
        if (_cursorColorSlider == null)
            return;

        var cursor = CustomCursorController.Instance;
        float sliderValue = 0f;

        if (cursor != null)
        {
            Color currentColor = cursor.GetCurrentFillColor();
            sliderValue = CursorColorSliderMapping.EstimateSliderValue(currentColor, _cursorColorReferenceImage);
        }

        _cursorColorSlider.SetValueWithoutNotify(sliderValue);
    }
}
