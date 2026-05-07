using System.Collections;
using UnityEngine;

public sealed class MainMenuController : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private ButtonVisual _startGameButtonVisual;
    [SerializeField] private ButtonVisual _settingsButtonVisual;
    [SerializeField] private ButtonVisual _exitGameButtonVisual;

    [Header("Panels")]
    [SerializeField] private GameObject _settingsPanelRoot;

    [Header("Audio")]
    [SerializeField, Min(0f)] private float _menuMusicStartDelaySeconds = 1.5f;
    [SerializeField, Min(0f)] private float _menuMusicFadeInDuration = 8.5f;

    private bool _isSettingsOpen;

    private void Awake()
    {
        _isSettingsOpen = false;

        if (_settingsPanelRoot != null)
            _settingsPanelRoot.SetActive(false);

        ApplyButtonSelection();
    }

    private void Start()
    {
        if (AudioManager.Instance == null)
            return;

        StartCoroutine(PlayMenuMusicWithDelayCoroutine());
    }

    public void StartGame()
    {
        if (SceneLoader.Instance != null)
            SceneLoader.Instance.LoadGameplayWithTransition();
    }

    public void ToggleSettings()
    {
        if (_isSettingsOpen)
        {
            CloseSettings();
            return;
        }

        OpenSettings();
    }

    public void QuitGame()
    {
        if (SceneLoader.Instance != null)
            SceneLoader.Instance.QuitGame();
    }

    private void ApplyButtonSelection()
    {
        _startGameButtonVisual?.SetSelected(false);
        _settingsButtonVisual?.SetSelected(_isSettingsOpen);
        _exitGameButtonVisual?.SetSelected(false);
    }

    private IEnumerator PlayMenuMusicWithDelayCoroutine()
    {
        if (_menuMusicStartDelaySeconds > 0f)
            yield return new WaitForSecondsRealtime(_menuMusicStartDelaySeconds);

        AudioManager.Instance?.PlayMenuMusic(shouldFadeIn: true, shouldFadeOut: false, fadeDuration: _menuMusicFadeInDuration, shouldLoop: true);
    }

    private void OpenSettings()
    {
        if (_isSettingsOpen)
            return;

        _isSettingsOpen = true;
        SetSettingsPanelActive(true);
        ApplyButtonSelection();
    }

    private void CloseSettings()
    {
        if (!_isSettingsOpen)
            return;

        _isSettingsOpen = false;
        SetSettingsPanelActive(false);
        ApplyButtonSelection();
    }

    private void SetSettingsPanelActive(bool isActive)
    {
        if (_settingsPanelRoot != null)
            _settingsPanelRoot.SetActive(isActive);
    }
}
