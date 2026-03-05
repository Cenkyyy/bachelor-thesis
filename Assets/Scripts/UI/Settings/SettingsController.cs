using UnityEngine;
using UnityEngine.UI;

public sealed class SettingsController : MonoBehaviour, IMajorPanel
{
    [Header("Root")]
    [SerializeField] private GameObject _settingsPanel;

    [Header("Pages")]
    [SerializeField] private GameObject _mainPage;
    [SerializeField] private GameObject _audioPage;
    [SerializeField] private GameObject _uiPage;

    [Header("Main Buttons")]
    [SerializeField] private Button _resumeButton;
    [SerializeField] private Button _audioSettingsButton;
    [SerializeField] private Button _uiSettingsButton;
    [SerializeField] private Button _returnToMenuButton;
    [SerializeField] private Button _exitGameButton;

    public PanelId Id => PanelId.Settings;
    public bool IsOpen => _settingsPanel.activeSelf;
    public bool PausesGame => true;
    public bool BlocksGameplayInput => true;

    private void Awake()
    {
        _resumeButton.onClick.AddListener(Resume);
        _audioSettingsButton.onClick.AddListener(ShowAudioPage);
        _uiSettingsButton.onClick.AddListener(ShowUiPage);
        _returnToMenuButton.onClick.AddListener(ReturnToMenu);
        _exitGameButton.onClick.AddListener(ExitGame);

        _settingsPanel.SetActive(false);
    }

    public void Open()
    {
        _settingsPanel.SetActive(true);
        ShowMainPage();
    }

    public void Close()
    {
        _settingsPanel.SetActive(false);
    }

    public void ShowMainPage()
    {
        SetActivePage(_mainPage);
    }

    public void ShowAudioPage()
    {
        SetActivePage(_audioPage);
    }

    public void ShowUiPage()
    {
        SetActivePage(_uiPage);
    }

    private void SetActivePage(GameObject active)
    {
        _mainPage.SetActive(active == _mainPage);
        _audioPage.SetActive(active == _audioPage);
        _uiPage.SetActive(active == _uiPage);
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
