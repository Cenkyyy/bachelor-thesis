using UnityEngine;
using UnityEngine.UI;

public sealed class OverworldSettingsController : MonoBehaviour, IMajorPanel
{
    [Header("Root")]
    [SerializeField] private GameObject _settingsPanel;

    [Header("Main Buttons")]
    [SerializeField] private Button _resumeButton;
    [SerializeField] private Button _returnToMenuButton;
    [SerializeField] private Button _exitGameButton;

    [Header("Runtime Settings")]
    [SerializeField] private Slider _audioSlider;
    [SerializeField] private Slider _cursorColorSlider;
    [SerializeField] private Image _cursorColorReferenceImage;
    [SerializeField] private CustomCursorController _cursorController;
    [SerializeField, Range(0f, 1f)] private float _fallbackSaturation = 1f;
    [SerializeField, Range(0f, 1f)] private float _fallbackValue = 1f;

    public PanelId Id => PanelId.Settings;
    public bool IsOpen => _settingsPanel.activeSelf;
    public bool PausesGame => true;
    public bool BlocksGameplayInput => true;

    private void Awake()
    {
        _resumeButton.onClick.AddListener(Resume);
        _returnToMenuButton.onClick.AddListener(ReturnToMenu);
        _exitGameButton.onClick.AddListener(ExitGame);

        _settingsPanel.SetActive(false);
    }

    private void OnEnable()
    {
        if (_audioSlider != null)
        {
            _audioSlider.onValueChanged.AddListener(OnAudioChanged);
        }

        if (_cursorColorSlider != null)
        {
            _cursorColorSlider.onValueChanged.AddListener(OnCursorColorChanged);
        }
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

    public void Open()
    {
        _settingsPanel.SetActive(true);
        SyncFromRuntimeState();
    }

    public void Close()
    {
        _settingsPanel.SetActive(false);
    }

    private void SyncFromRuntimeState()
    {
        if (_audioSlider != null)
        {
            _audioSlider.SetValueWithoutNotify(AudioListener.volume);
        }

        if (_cursorColorSlider == null)
            return;

        var cursor = ResolveCursorController();
        float sliderValue = 0f;

        if (cursor != null)
        {
            Color currentColor = cursor.GetCurrentFillColor();
            sliderValue = CursorColorSliderMapping.EstimateSliderValue(currentColor, _cursorColorReferenceImage);
        }

        _cursorColorSlider.SetValueWithoutNotify(sliderValue);
    }

    private void OnAudioChanged(float value)
    {
        AudioListener.volume = Mathf.Clamp01(value);
    }

    private void OnCursorColorChanged(float value)
    {
        var cursor = ResolveCursorController();
        if (cursor == null)
            return;

        Color color = CursorColorSliderMapping.GetColor(value, _cursorColorReferenceImage, _fallbackSaturation, _fallbackValue);
        cursor.ApplyFillColor(color);
    }

    private CustomCursorController ResolveCursorController()
    {
        if (_cursorController != null)
            return _cursorController;

        _cursorController = CustomCursorController.Instance;
        return _cursorController;
    }

    private void Resume()
    {
        PanelManager.Instance.CloseCurrentMajorPanel();
    }

    private void ReturnToMenu()
    {
        if (SceneLoader.Instance != null)
        {
            SceneLoader.Instance.LoadMenu();
        }
    }

    private void ExitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
        Application.Quit();
    }
}
