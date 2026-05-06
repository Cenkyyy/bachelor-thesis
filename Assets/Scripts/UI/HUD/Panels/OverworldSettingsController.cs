using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public sealed class OverworldSettingsController : MonoBehaviour, IMajorPanel
{
    [Header("Root")]
    [SerializeField] private GameObject _settingsPanel;

    [Header("Main Buttons")]
    [SerializeField] private MenuButtonVisual _resumeButton;
    [SerializeField] private MenuButtonVisual _returnToMenuButton;
    [SerializeField] private MenuButtonVisual _exitGameButton;

    [Header("Runtime Settings")]
    [SerializeField] private Slider _audioSlider;
    [SerializeField] private Slider _cursorColorSlider;
    [SerializeField] private Image _cursorColorReferenceImage;
    [SerializeField, Range(0f, 1f)] private float _fallbackSaturation = 1f;
    [SerializeField, Range(0f, 1f)] private float _fallbackValue = 1f;

    [Header("Scene Switch Optimization")]
    [SerializeField, Min(1)] private int _colliderDisableOperationsPerFrame = 200;

    private CustomCursorController _cursorController;
    private bool _isReturningToMenu;

    public PanelId Id => PanelId.Settings;
    public bool IsOpen => _settingsPanel.activeSelf;
    public bool PausesGame => true;
    public bool BlocksGameplayInput => true;

    private void Awake()
    {
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

    public void Resume()
    {
        PanelManager.Instance.CloseCurrentMajorPanel();
    }

    public void ReturnToMenu()
    {
        if (_isReturningToMenu)
            return;

        StartCoroutine(ReturnToMenuCoroutine());
    }

    public void ExitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
        Application.Quit();
    }

    private void SyncFromRuntimeState()
    {
        if (_audioSlider != null)
        {
            _audioSlider.SetValueWithoutNotify(AudioManager.Instance != null ? AudioManager.Instance.MasterVolume : AudioListener.volume);
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
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetMasterVolume(value);
            return;
        }

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

    private IEnumerator ReturnToMenuCoroutine()
    {
        _isReturningToMenu = true;

        if (_returnToMenuButton != null)
        {
            var returnToMenuButtonComponent = _returnToMenuButton.GetComponent<Button>();
            returnToMenuButtonComponent.interactable = false;
        }
            

        yield return DisableActiveSceneCollidersCoroutine();

        if (SceneLoader.Instance != null)
            SceneLoader.Instance.LoadMenuWithTransition();

        _isReturningToMenu = false;

        if (_returnToMenuButton != null)
        {
            var returnToMenuButtonComponent = _returnToMenuButton.GetComponent<Button>();
            returnToMenuButtonComponent.interactable = true;
        }
    }

    private IEnumerator DisableActiveSceneCollidersCoroutine()
    {
        var colliders = FindObjectsByType<Collider2D>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

        if (colliders.Length == 0)
            yield break;

        int operations = 0;
        for (int i = 0; i < colliders.Length; i++)
        {
            colliders[i].enabled = false;
            operations++;

            if (operations >= _colliderDisableOperationsPerFrame)
            {
                operations = 0;
                yield return null;
            }
        }
    }
}
