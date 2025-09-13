using UnityEngine;
using UnityEngine.UI;

public class SettingsController : MonoBehaviour
{
    [SerializeField] private GameObject _settingsPanel;
    [SerializeField] private BackpackPresenter _backpackInventory;
    [SerializeField] private Button _resumeButton;

    private void Start()
    {
        if (_resumeButton != null)
        {
            _resumeButton.onClick.AddListener(ResumeGame);
        }

        _settingsPanel.SetActive(false);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (_backpackInventory != null && _backpackInventory.IsInventoryOpen)
            {
                _backpackInventory.CloseInventory();
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
