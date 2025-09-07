using UnityEngine;
using UnityEngine.UI;

public class SettingsController : MonoBehaviour
{
    [SerializeField] GameObject settingsPanel;
    [SerializeField] BackpackPresenter backpackInventory;
    [SerializeField] Button resumeButton;

    void Start()
    {
        if (resumeButton != null)
        {
            resumeButton.onClick.AddListener(ResumeGame);
        }

        settingsPanel.SetActive(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (backpackInventory != null && backpackInventory.IsInventoryOpen)
            {
                backpackInventory.CloseInventory();
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
        settingsPanel.SetActive(true);
        GameStateManager.SetPause(true);
    }

    private void ResumeGame()
    {
        settingsPanel.SetActive(false);
        GameStateManager.SetPause(false);
    }
}
