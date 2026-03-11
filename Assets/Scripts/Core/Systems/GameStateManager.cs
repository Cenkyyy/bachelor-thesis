using UnityEngine;

public class GameStateManager : MonoBehaviour
{
    public static GameStateManager Instance { get; private set; }
    public static bool IsGamePaused { get; private set; }

    /// <summary>
    /// This field tracks whether a manual pause has been requested (e.g., via a settings menu), independent of anything related to scene transitions and their possible blockers.
    /// </summary>
    private static bool _manualPauseRequested;
    
    /// <summary>
    /// Represents the number of active transition pause locks. When greater than 0, the game is considered paused due to an active transition.
    /// </summary>
    private static int _transitionPauseLockCount;

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
        _manualPauseRequested = paused;
        ApplyPauseState();
    }

    public static void PushTransitionPauseLock()
    {
        _transitionPauseLockCount++;
        ApplyPauseState();
    }

    public static void PopTransitionPauseLock()
    {
        if (_transitionPauseLockCount > 0)
        {
            _transitionPauseLockCount--;
        }

        ApplyPauseState();
    }

    private static void ApplyPauseState()
    {
        bool shouldPause = _manualPauseRequested || _transitionPauseLockCount > 0;
        if (IsGamePaused == shouldPause)
            return;

        IsGamePaused = shouldPause;
        Time.timeScale = IsGamePaused ? 0f : 1f;
    }
}
