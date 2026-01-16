using UnityEngine;
using UnityEngine.UI;

public sealed class AudioSettingsPageController : MonoBehaviour
{
    [SerializeField] private SettingsController _settingsController;
    [SerializeField] private Button _backButton;

    private void OnEnable()
    {
        _backButton.onClick.AddListener(OnBack);
    }

    private void OnDisable()
    {
        _backButton.onClick.RemoveListener(OnBack);
    }

    private void OnBack()
    {
        _settingsController.ShowMainPage();
    }
}
