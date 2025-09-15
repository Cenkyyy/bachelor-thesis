using UnityEngine;

public class MainMenuActions : MonoBehaviour
{
    public void StartGame()
    {
        if (SceneLoader.Instance != null)
        {
            SceneLoader.Instance.LoadByIndex(SceneLoader.Instance.GameplayBuildIndex);
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
