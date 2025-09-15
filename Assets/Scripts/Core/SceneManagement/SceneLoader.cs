using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public sealed class SceneLoader : MonoBehaviour
{
    public static SceneLoader Instance { get; private set; }

    [Header("Build Indices")]
    [field: SerializeField] public int BootBuildIndex { get; private set; } = 0;
    [field: SerializeField] public int MenuBuildIndex { get; private set; } = 1;
    [field: SerializeField] public int GameplayBuildIndex { get; private set; } = 2;

    [Header("Fade")]
    [SerializeField] private CanvasGroup _fader;
    [SerializeField, Min(0.01f)] private float _fadeDuration = 0.25f;
    [SerializeField, Min(0f)] private float _holdAtBlackSeconds = 1.0f;

    [Header("Boot Behavior")]
    [SerializeField] private bool _loadMenuOnBoot = true;

    private bool _isLoading;

    private void Awake()
    {
        if (Instance != null && Instance != this) 
        { 
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        _fader.alpha = 1f;
        _fader.blocksRaycasts = true;

        if (_loadMenuOnBoot)
        {
            // already black, then load Menu, then fade in
            StartCoroutine(LoadByIndexRoutine(MenuBuildIndex, doFadeOut: false, doFadeIn: true));
        }
        else
        {
            // if not auto-loading, then just fade in Boot
            StartCoroutine(FadeTo(0f, _fadeDuration));
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    /// <summary>
    /// Load a scene by build index with fade-out, then load and then fade-in.
    /// </summary>
    /// <param name="buildIndex">Build index of the scene to load.</param>
    public void LoadByIndex(int buildIndex)
    {
        if (_isLoading) 
            return;
        StartCoroutine(LoadByIndexRoutine(buildIndex, doFadeOut: true, doFadeIn: true));
    }

    public void QuitGame()
    {
    #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
    #else
        Application.Quit();
    #endif
    }

    /// <summary>
    /// Coroutine to load a scene by build index with optional fade-out and fade-in.
    /// </summary>
    /// <param name="buildIndex">Build index of the scene to load.</param>
    /// <param name="doFadeOut">Whether to perform a fade-out before loading.</param>
    /// <param name="doFadeIn">Whether to perform a fade-in after loading.</param>
    /// <returns>IEnumerator for the coroutine.</returns>
    private IEnumerator LoadByIndexRoutine(int buildIndex, bool doFadeOut, bool doFadeIn)
    {
        _isLoading = true;

        if (doFadeOut) 
            yield return FadeTo(1f, _fadeDuration);

        if (_holdAtBlackSeconds > 0f)
            yield return new WaitForSecondsRealtime(_holdAtBlackSeconds);

        if (buildIndex < 0 || buildIndex >= SceneManager.sceneCountInBuildSettings)
        {
            Debug.LogError($"[SceneLoader] Build index {buildIndex} is not in Build Settings.");
        }
        else
        {
            var op = SceneManager.LoadSceneAsync(buildIndex, LoadSceneMode.Single);
            op.allowSceneActivation = true;
            while (!op.isDone) 
                yield return null;
        }

        if (doFadeIn) 
            yield return FadeTo(0f, _fadeDuration);

        _isLoading = false;
    }

    private IEnumerator FadeTo(float targetAlpha, float duration)
    {
        if (_fader == null) 
            yield break;

        // ensure fader is interactable during fade
        _fader.blocksRaycasts = true;

        var elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            var start = _fader.alpha;

            // smooth step for better easing
            var progress = Mathf.Clamp01(elapsed / duration);
            progress = Mathf.SmoothStep(0f, 1f, progress);

            // lerp alpha
            _fader.alpha = Mathf.Lerp(start, targetAlpha, progress);
            yield return null;
        }

        _fader.alpha = targetAlpha;
        _fader.blocksRaycasts = targetAlpha > 0.001f;
    }
}
