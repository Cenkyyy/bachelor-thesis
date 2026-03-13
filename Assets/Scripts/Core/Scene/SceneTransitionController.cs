using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class SceneTransitionController : MonoBehaviour
{
    // TODO: In the future I would probably like to remove this being a singleton and instead have the SceneLoader expose whether the Transition is active, but its 1 AM ;(
    public static SceneTransitionController Instance { get; private set; }

    [Header("Assigned UI References")]
    [SerializeField] private GameObject _transitionRoot;
    [SerializeField] private CanvasGroup _overlayCanvasGroup;
    [SerializeField] private Image _overlayImage;
    [SerializeField] private TMP_Text _loadingText;

    [Header("Fade")]
    [SerializeField, Min(0f)] private float _fadeInDuration = 0.75f;
    [SerializeField, Min(0f)] private float _fadeOutDuration = 0.75f;
    [SerializeField, Min(0.001f)] private float _maxTransitionDeltaTime = 0.05f;

    [Header("Loading Text")]
    [SerializeField] private string _loadingBaseText = "Loading";
    [SerializeField, Min(0.05f)] private float _loadingDotInterval = 0.75f;

    [Header("Overlay Visual")]
    [SerializeField] private Sprite _overlaySprite;
    [SerializeField] private Color _overlayColorWhenSpriteAssigned = Color.white;
    [SerializeField] private Color _overlayFallbackColor = Color.black;

    public bool IsTransitionActive => _isTransitionActive;
    public event Action<string> TransitionMiddleCompleted;
    public event Action<string> TransitionFinished;

    private OverlayFadeService _overlayFadeService;
    private bool _isTransitionActive;
    private bool _isMiddleCompleted;
    private string _activeTargetSceneName;
    private float _loadingTimer;
    private int _loadingStep;

    private void Awake()
    {
        _overlayFadeService = new OverlayFadeService(_overlayCanvasGroup, _maxTransitionDeltaTime);
        SetTransitionRootVisible(false);
    }

    private void OnDisable()
    {
        _isTransitionActive = false;
        _isMiddleCompleted = false;
        _activeTargetSceneName = string.Empty;
        GameStateManager.ReleasePauseLock(this);
    }

    public bool TryLoadSceneWithTransition(string sceneName)
    {
        if (!TryBeginSceneTransition(sceneName))
            return false;

        StartCoroutine(AutoFinishAfterMiddleCoroutine());
        return true;
    }

    public bool TryBeginSceneTransition(string sceneName)
    {
        if (!CanStartTransition(sceneName))
            return false;

        StartCoroutine(RunTransitionStartAndMiddleCoroutine(sceneName));
        return true;
    }

    public bool TryFinishSceneTransition()
    {
        if (!_isTransitionActive || !_isMiddleCompleted || string.IsNullOrEmpty(_activeTargetSceneName))
            return false;

        StartCoroutine(RunTransitionFinishCoroutine(_activeTargetSceneName));
        return true;
    }

    private bool CanStartTransition(string sceneName)
    {
        if (_isTransitionActive || string.IsNullOrEmpty(sceneName))
            return false;

        if (SceneManager.GetActiveScene().name == sceneName)
            return false;

        if (_overlayCanvasGroup == null || _overlayImage == null || _loadingText == null)
            return false;

        return true;
    }

    private IEnumerator AutoFinishAfterMiddleCoroutine()
    {
        while (_isTransitionActive && !_isMiddleCompleted)
            yield return null;

        if (_isMiddleCompleted)
            TryFinishSceneTransition();
    }

    private IEnumerator RunTransitionStartAndMiddleCoroutine(string sceneName)
    {
        _isTransitionActive = true;
        _isMiddleCompleted = false;
        _activeTargetSceneName = sceneName;

        // Show the transition root, pause the game, assign the overlay image and hide the loading text until the fade in is done
        SetTransitionRootVisible(true);
        GameStateManager.AcquirePauseLock(this);
        _overlayFadeService.ApplyOverlayVisual(_overlayImage, _overlaySprite, _overlayColorWhenSpriteAssigned, _overlayFallbackColor);
        SetLoadingTextVisible(false);

        // Fade in the overlay and show the loading text
        yield return _overlayFadeService.FadeIn(_fadeInDuration);

        SetLoadingTextVisible(true);

        // Load the scene asynchronously in the background
        AsyncOperation loadOperation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
        loadOperation.allowSceneActivation = false;

        // Keep updating the loading text until the 0.9 progress is reached, then allow scene activation
        // and keep updating the loading text until the final load is done and the scene is ready to be revealed
        _loadingStep = 0;
        _loadingTimer = 0f;

        while (loadOperation.progress < 0.9f)
        {
            UpdateLoadingText();
            yield return null;
        }

        UpdateLoadingText();
        loadOperation.allowSceneActivation = true;

        while (!loadOperation.isDone)
        {
            UpdateLoadingText();
            yield return null;
        }

        yield return WaitForSceneRevealReadiness(sceneName);

        // Turn off the loading text, unpause the game and hide the transition root
        SetLoadingTextVisible(false);
        _isMiddleCompleted = true;
        TransitionMiddleCompleted?.Invoke(sceneName);
    }

    private IEnumerator RunTransitionFinishCoroutine(string sceneName)
    {
        if (!_isTransitionActive || !_isMiddleCompleted)
            yield break;

        _isMiddleCompleted = false;

        GameStateManager.ReleasePauseLock(this);
        yield return _overlayFadeService.FadeOut(_fadeOutDuration);

        SetTransitionRootVisible(false);

        _isTransitionActive = false;
        _activeTargetSceneName = string.Empty;
        TransitionFinished?.Invoke(sceneName);
    }

    private IEnumerator WaitForSceneRevealReadiness(string targetSceneName)
    {
        // Update the loading text until the active scene is the target scene
        while (SceneManager.GetActiveScene().name != targetSceneName)
        {
            UpdateLoadingText();
            yield return null;
        }

        var activeScene = SceneManager.GetActiveScene();

        // Find all active Monobehaviours in the scene that implement ISceneTransitionReadinessBlocker
        var blockerComponents = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        var blockers = new List<ISceneTransitionReadinessBlocker>();
        for (int i = 0; i < blockerComponents.Length; i++)
        {
            var component = blockerComponents[i];
            if (component == null)
                continue;
            if (component.gameObject.scene != activeScene)
                continue;
            if (component is ISceneTransitionReadinessBlocker blocker)
                blockers.Add(blocker);
        }

        if (blockers.Count == 0)
            yield break;

        // Keep updating the loading text until all blockers are ready for scene reveal
        bool allReady;
        while (true)
        {
            allReady = true;

            for (int i = 0; i < blockers.Count; i++)
            {
                if (!blockers[i].IsReadyForSceneReveal)
                {
                    allReady = false;
                    break;
                }
            }

            if (allReady)
                yield break;

            UpdateLoadingText();
            yield return null;
        }
    }

    private void SetTransitionRootVisible(bool visible)
    {
        if (_transitionRoot == null)
            return;

        if (_transitionRoot.activeSelf == visible)
            return;

        _transitionRoot.SetActive(visible);
    }

    private void UpdateLoadingText()
    {
        _loadingTimer += Mathf.Min(Time.unscaledDeltaTime, _maxTransitionDeltaTime);
        while (_loadingTimer >= _loadingDotInterval)
        {
            _loadingTimer -= _loadingDotInterval;
            _loadingStep = (_loadingStep + 1) % 4;
        }

        _loadingText.text = _loadingBaseText + new string('.', _loadingStep);
    }

    private void SetLoadingTextVisible(bool visible)
    {
        if (_loadingText == null)
            return;

        _loadingText.enabled = visible;
        _loadingTimer = 0f;
        _loadingStep = 0;
        _loadingText.text = _loadingBaseText;
    }
}
