using UnityEngine;
using UnityEngine.UI;

public class SettingsController : MonoBehaviour
{
    [SerializeField] private GameObject _settingsPanel;
    [SerializeField] private BackpackPanel _backpackPanel;
    [SerializeField] private Button _resumeButton;

    private void Start()
    {
        if (_resumeButton)
        {
            _resumeButton.onClick.AddListener(ResumeGame);
        }

        _settingsPanel.SetActive(false);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (_backpackPanel && _backpackPanel.IsOpen)
            {
                _backpackPanel.Close();
                return;
            }

            if (GameStateManager.IsGamePaused)
            {
                ResumeGame();
            }
            else
            {
                PauseGame();
            }
        }    
    }

    private void PauseGame()
    {
        _settingsPanel.SetActive(true);
        GameStateManager.SetPause(true);
    }

    private void ResumeGame()
    {
        _settingsPanel.SetActive(false);
        GameStateManager.SetPause(false);
    }
}
