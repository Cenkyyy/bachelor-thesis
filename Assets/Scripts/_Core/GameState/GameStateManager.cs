using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Central pause state manager that combines manual pause requests with temporary pause locks.
/// </summary>
public class GameStateManager : MonoBehaviour
{
    public static GameStateManager Instance { get; private set; }
    public static bool IsGamePaused { get; private set; }
    public static event Action<bool> PauseChanged;

    /// <summary>
    /// This field tracks whether a manual pause has been requested (e.g., via a settings menu), independent of anything related to scene transitions and their possible blockers.
    /// </summary>
    private static bool _manualPauseRequested;
    
    /// <summary>
    /// Represents the set of active pause locks which are currently active in the game, preventing the game from unpausing until all locks are released.
    /// </summary>
    private static readonly HashSet<int> _pauseLockOwners = new();

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

    public static void AcquirePauseLock(object owner)
    {
        if (owner == null)
            return;

        int ownerId = System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(owner);
        _pauseLockOwners.Add(ownerId);
        ApplyPauseState();
    }

    public static void ReleasePauseLock(object owner)
    {
        if (owner == null)
            return;

        int ownerId = System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(owner);
        _pauseLockOwners.Remove(ownerId);
        ApplyPauseState();
    }

    private static void ApplyPauseState()
    {
        bool shouldPause = _manualPauseRequested || _pauseLockOwners.Count > 0;
        if (IsGamePaused == shouldPause)
            return;

        IsGamePaused = shouldPause;
        Time.timeScale = IsGamePaused ? 0f : 1f;
        PauseChanged?.Invoke(IsGamePaused);
    }
}
