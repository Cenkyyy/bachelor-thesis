using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public sealed class SceneLoader : MonoBehaviour
{
    public static SceneLoader Instance { get; private set; }

    [SerializeField] private string _bootScene = "00_Boot";
    [SerializeField] private string _menuScene = "01_Menu";
    [SerializeField] private string _gameplayScene = "02_Gameplay";
    [SerializeField] private bool _autoGoToMenuFromBoot = true;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

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

        Time.timeScale = 1f;

        SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
    }

    public void LoadMenu() => Load(_menuScene);
    public void LoadGameplay() => Load(_gameplayScene);

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
