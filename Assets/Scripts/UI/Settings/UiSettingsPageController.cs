using UnityEngine;
using UnityEngine.UI;

public sealed class UiSettingsPageController : MonoBehaviour
{
    [SerializeField] private SettingsController _settingsController;

    [SerializeField] private CustomCursorController _cursorController;
    [SerializeField] private Slider _cursorHueSlider;
    [SerializeField] private Image _cursorHueGradientImage;
    [SerializeField] private Image _cursorHueHandleImage;

    [SerializeField] private Button _backButton;
    [SerializeField] private Button _saveButton;

    [SerializeField, Range(0f, 1f)] private float _saturation = 1f;
    [SerializeField, Range(0f, 1f)] private float _value = 1f;
    [SerializeField, Range(0f, 1f)] private float _alpha = 1f;

    private Color _pendingColor;
    private bool _dirty;

    private void OnEnable()
    {
        EnsureHueGradient();

        Color current = _cursorController.GetCurrentFillColor();
        Color.RGBToHSV(current, out float h, out _, out _);
        _cursorHueSlider.SetValueWithoutNotify(h);

        _pendingColor = current;
        _dirty = false;

        _cursorHueHandleImage.color = current;

        _cursorHueSlider.onValueChanged.AddListener(OnHueChanged);
        _saveButton.onClick.AddListener(OnSave);
        _backButton.onClick.AddListener(OnBack);
    }

    private void OnDisable()
    {
        _cursorHueSlider.onValueChanged.RemoveListener(OnHueChanged);
        _saveButton.onClick.RemoveListener(OnSave);
        _backButton.onClick.RemoveListener(OnBack);
    }

    private void OnBack()
    {
        DiscardPending();
        _settingsController.ShowMainPage();
    }

    public void DiscardPending()
    {
        Color current = _cursorController.GetCurrentFillColor();
        Color.RGBToHSV(current, out float h, out _, out _);
        _cursorHueSlider.SetValueWithoutNotify(h);

        _pendingColor = current;
        _dirty = false;

        _cursorHueHandleImage.color = current;
    }

    private void OnHueChanged(float hue)
    {
        Color rgb = Color.HSVToRGB(hue, _saturation, _value);
        rgb.a = _alpha;

        _pendingColor = rgb;
        _dirty = true;

        _cursorHueHandleImage.color = rgb;
    }

    private void OnSave()
    {
        if (!_dirty)
            return;

        _cursorController.ApplyFillColor(_pendingColor);
        _dirty = false;
    }

    private void EnsureHueGradient()
    {
        if (_cursorHueGradientImage.sprite != null)
            return;

        const int w = 256;
        var tex = new Texture2D(w, 1, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        for (int x = 0; x < w; x++)
        {
            float hue = x / (w - 1f);
            tex.SetPixel(x, 0, Color.HSVToRGB(hue, 1f, 1f));
        }

        tex.Apply(false);

        var sprite = Sprite.Create(tex, new Rect(0, 0, w, 1), new Vector2(0.5f, 0.5f), 100f);
        _cursorHueGradientImage.sprite = sprite;
        _cursorHueGradientImage.type = Image.Type.Simple;
    }
}
