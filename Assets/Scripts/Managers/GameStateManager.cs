using UnityEngine;

public class GameStateManager : MonoBehaviour
{
    public static GameStateManager Instance { get; private set; }
    public static bool IsGamePaused { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SetPause(false);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public static void SetPause(bool paused)
    {
        if (IsGamePaused == paused) return;

        IsGamePaused = paused;
        Time.timeScale = paused ? 0f : 1f;
    }
}
