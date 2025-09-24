using UnityEngine;

public class MainMenuController : MonoBehaviour
{
    public void StartGame()
    {
        if (SceneLoader.Instance != null)
        {
            SceneLoader.Instance.LoadGameplay();
        }
    }

    public void QuitGame()
    {
        if (SceneLoader.Instance != null)
        {
            SceneLoader.Instance.QuitGame();
        }
    }
}
