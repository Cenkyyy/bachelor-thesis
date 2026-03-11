using UnityEngine;

public sealed class MainMenuController : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private MenuButtonVisual _startGameButtonVisual;
    [SerializeField] private MenuButtonVisual _settingsButtonVisual;
    [SerializeField] private MenuButtonVisual _exitGameButtonVisual;

    [Header("Panels")]
    [SerializeField] private GameObject _settingsPanel;

    private bool _isSettingsOpen;

    private void Awake()
    {
        _isSettingsOpen = false;

        if (_settingsPanel != null)
        {
            _settingsPanel.SetActive(false);
        }

        ApplyButtonSelection();
    }

    public void StartGame()
    {
        if (SceneLoader.Instance != null)
        {
            SceneLoader.Instance.LoadGameplayWithTransition();
        }
    }

    public void OpenSettings()
    {
        if (_isSettingsOpen)
            return;

        _isSettingsOpen = true;
        SetSettingsPanelActive(true);
        ApplyButtonSelection();
    }

    public void CloseSettings()
    {
        if (!_isSettingsOpen)
            return;

        _isSettingsOpen = false;
        SetSettingsPanelActive(false);
        ApplyButtonSelection();
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
        {
            SceneLoader.Instance.QuitGame();
        }
    }

    private void SetSettingsPanelActive(bool isActive)
    {
        if (_settingsPanel != null)
        {
            _settingsPanel.SetActive(isActive);
        }
    }

    private void ApplyButtonSelection()
    {
        _startGameButtonVisual?.SetSelected(false);
        _settingsButtonVisual?.SetSelected(_isSettingsOpen);
        _exitGameButtonVisual?.SetSelected(false);
    }
}
