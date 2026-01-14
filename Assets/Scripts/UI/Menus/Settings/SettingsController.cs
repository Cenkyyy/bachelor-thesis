using UnityEngine;
using UnityEngine.UI;

public class SettingsController : MonoBehaviour, IMajorPanel
{
    [SerializeField] private GameObject _settingsPanel;
    [SerializeField] private Button _resumeButton;

    public PanelId Id => PanelId.Settings;
    public bool IsOpen => _settingsPanel.activeSelf;
    public bool PausesGame => true;
    public bool BlocksGameplayInput => true;

    private void Start()
    {
        _resumeButton.onClick.AddListener(OnClickResume);
        _settingsPanel.SetActive(false);
    }

    public void Open()
    {
        _settingsPanel.SetActive(true);
    }

    public void Close()
    {
        _settingsPanel.SetActive(false);
    }

    private void OnClickResume()
    {
        PanelManager.Instance.CloseCurrentMajorPanel();
    }
}
