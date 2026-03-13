using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public sealed class SceneLoader : MonoBehaviour
{
    public static SceneLoader Instance { get; private set; }

    [Header("Scene Names")]
    [SerializeField] private string _bootScene = "00_Boot";
    [SerializeField] private string _menuScene = "01_Menu";
    [SerializeField] private string _gameplayScene = "02_Gameplay";

    [Header("Settings")]
    [SerializeField] private bool _autoGoToMenuFromBoot = true;

    [Header("Transition")]
    [SerializeField] private SceneTransitionController _sceneTransitionController;
    [SerializeField] private PreGameController _preGameController;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (_sceneTransitionController == null)
        {
            _sceneTransitionController = GetComponent<SceneTransitionController>();
        }

        if (_preGameController == null)
        {
            _preGameController = GetComponent<PreGameController>();
        }

        if (_autoGoToMenuFromBoot && SceneManager.GetActiveScene().name == _bootScene)
        {
            Load(_menuScene);
        }
    }

    public void Load(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
            return;
        if (SceneManager.GetActiveScene().name == sceneName)
            return;

        GameStateManager.SetPause(false);
        SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
    }

    public void LoadWithTransition(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
            return;
        if (SceneManager.GetActiveScene().name == sceneName)
            return;
        if (_sceneTransitionController != null && _sceneTransitionController.TryLoadSceneWithTransition(sceneName))
            return;

        Load(sceneName);

    }

    public void LoadMenu() => Load(_menuScene);
    public void LoadGameplay() => Load(_gameplayScene);
    public void LoadMenuWithTransition() => LoadWithTransition(_menuScene);
    public void LoadGameplayWithTransition()
    {
        if (_preGameController != null && _preGameController.TryStartNewGame(_gameplayScene))
            return;

        LoadWithTransition(_gameplayScene);
    }
    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
