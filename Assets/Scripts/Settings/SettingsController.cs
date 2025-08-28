using UnityEngine;
using UnityEngine.UI;

public class SettingsController : MonoBehaviour
{
    [SerializeField] GameObject settingsPanel;
    [SerializeField] BackpackController backpackInventory;
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
        //Time.timeScale = 0f; // freeze world
    }

    private void ResumeGame()
    {
        settingsPanel.SetActive(false);
        GameStateManager.SetPause(false);
        //Time.timeScale = 1f; // unfreeze world
    }
}
