using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class SceneTransitionController : MonoBehaviour
{
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

    private bool _isTransitionRunning;
    private bool _holdsTransitionPauseLock;
    private float _loadingTimer;
    private int _loadingStep;

    private void Awake()
    {
        SetTransitionRootVisible(false);
    }

    private void OnDisable()
    {
        ReleaseTransitionPauseLock();
    }

    public bool TryLoadScene(string sceneName)
    {
        if (_isTransitionRunning || string.IsNullOrEmpty(sceneName))
            return false;

        if (SceneManager.GetActiveScene().name == sceneName)
            return false;

        if (_overlayCanvasGroup == null || _overlayImage == null || _loadingText == null)
            return false;

        StartCoroutine(LoadSceneRoutine(sceneName));
        return true;
    }

    private IEnumerator LoadSceneRoutine(string sceneName)
    {
        _isTransitionRunning = true;

        // Show the transition root, pause the game, assign the overlay image and hide the loading text until the fade in is done
        SetTransitionRootVisible(true);
        AcquireTransitionPauseLock();
        ApplyOverlayVisual();
        SetLoadingTextVisible(false);

        // Fade in the overlay, show the loading text and start loading the scene asynchronously until the scene is ready to be activated
        yield return Fade(0f, 1f, _fadeInDuration);

        // Load the scene asynchronously in the background
        SetLoadingTextVisible(true);
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

        UpdateLoadingText();
        yield return null;

        // Turn off the loading text, fade out the overlay, unpause the game and hide the transition root
        SetLoadingTextVisible(false);
        yield return Fade(1f, 0f, _fadeOutDuration);

        ReleaseTransitionPauseLock();
        SetTransitionRootVisible(false);
        _isTransitionRunning = false;
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

    private IEnumerator Fade(float fromAlpha, float toAlpha, float duration)
    {
        if (_overlayCanvasGroup == null)
            yield break;

        if (duration <= 0f)
        {
            _overlayCanvasGroup.alpha = toAlpha;
            yield break;
        }

        float t = 0f;
        _overlayCanvasGroup.alpha = fromAlpha;

        while (t < duration)
        {
            t += Mathf.Min(Time.unscaledDeltaTime, _maxTransitionDeltaTime);
            float normalized = Mathf.Clamp01(t / duration);
            _overlayCanvasGroup.alpha = Mathf.Lerp(fromAlpha, toAlpha, normalized);
            yield return null;
        }

        _overlayCanvasGroup.alpha = toAlpha;
    }

    private void SetTransitionRootVisible(bool visible)
    {
        if (_transitionRoot == null)
            return;

        if (_transitionRoot.activeSelf == visible)
            return;

        _transitionRoot.SetActive(visible);
    }

    private void ApplyOverlayVisual()
    {
        if (_overlayImage == null)
            return;

        bool hasSprite = _overlaySprite != null;
        _overlayImage.sprite = _overlaySprite;
        _overlayImage.type = Image.Type.Simple;
        _overlayImage.color = hasSprite ? _overlayColorWhenSpriteAssigned : _overlayFallbackColor;
    }

    private void AcquireTransitionPauseLock()
    {
        if (_holdsTransitionPauseLock)
            return;

        GameStateManager.PushTransitionPauseLock();
        _holdsTransitionPauseLock = true;
    }

    private void ReleaseTransitionPauseLock()
    {
        if (!_holdsTransitionPauseLock)
            return;

        GameStateManager.PopTransitionPauseLock();
        _holdsTransitionPauseLock = false;
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
