using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class PreGameManager : MonoBehaviour
{
    public static PreGameManager Instance;

    public enum PreGameState
    {
        Idle = 0,
        LoadingWorld = 1,
        NarrativeIntro = 2,
        WordSelection = 3,
        EnteringGameplay = 4,
    }

    [Header("Settings")]
    [SerializeField] private bool _enablePreGame = true;

    [Header("References")]
    [SerializeField] private SceneTransitionController _sceneTransitionController;

    [Header("Step Controllers")]
    [SerializeField] private NarrativeIntroductionController _narrativeIntroductionController;
    [SerializeField] private StarterWordSelectionController _starterWordSelectionController;

    [Header("Word Selection")]
    [SerializeField] private StarterWordSelectionData _starterWordSelectionData;
    [SerializeField] private Player _player;

    public PreGameState CurrentState { get; private set; } = PreGameState.Idle;
    public bool IsPreGameEnabled => _enablePreGame;

    private string _expectedGameplaySceneName;
    private Coroutine _preGameCoroutine;

    protected virtual void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (_sceneTransitionController == null)
            _sceneTransitionController = GetComponent<SceneTransitionController>();

        if (_player == null)
            _player = FindFirstObjectByType<Player>();

        if (_narrativeIntroductionController != null)
            _narrativeIntroductionController.Hide();

        if (_starterWordSelectionController != null)
            _starterWordSelectionController.Hide();
    }

    private void OnEnable()
    {
        if (_sceneTransitionController == null)
            return;

        _sceneTransitionController.TransitionMiddleCompleted += HandleTransitionMiddleCompleted;
        _sceneTransitionController.TransitionFinished += HandleTransitionFinished;
    }

    private void OnDisable()
    {
        if (_sceneTransitionController != null)
        {
            _sceneTransitionController.TransitionMiddleCompleted -= HandleTransitionMiddleCompleted;
            _sceneTransitionController.TransitionFinished -= HandleTransitionFinished;
        }

        StopPreGameCoroutine();
        if (_narrativeIntroductionController != null)
            _narrativeIntroductionController.Hide();

        if (_starterWordSelectionController != null)
            _starterWordSelectionController.Hide();

        SetState(PreGameState.Idle);
    }

    public bool TryStartNewGame(string gameplaySceneName)
    {
        if (!_enablePreGame)
            return false;

        if (CurrentState != PreGameState.Idle)
            return false;

        if (_sceneTransitionController == null || string.IsNullOrEmpty(gameplaySceneName))
            return false;

        _expectedGameplaySceneName = gameplaySceneName;
        SetState(PreGameState.LoadingWorld);

        bool started = _sceneTransitionController.TryBeginSceneTransition(gameplaySceneName);
        if (!started)
        {
            _expectedGameplaySceneName = string.Empty;
            SetState(PreGameState.Idle);
            return false;
        }

        return true;
    }

    private void HandleTransitionMiddleCompleted(string sceneName)
    {
        if (CurrentState != PreGameState.LoadingWorld)
            return;

        if (!string.Equals(sceneName, _expectedGameplaySceneName))
            return;

        _expectedGameplaySceneName = string.Empty;

        StopPreGameCoroutine();
        _preGameCoroutine = StartCoroutine(RunPreGameStepsCoroutine());
    }

    private IEnumerator RunPreGameStepsCoroutine()
    {
        if (_narrativeIntroductionController != null && _narrativeIntroductionController.IsEnabled)
        {
            SetState(PreGameState.NarrativeIntro);
            yield return _narrativeIntroductionController.RunIntroCoroutine();
        }

        if (_starterWordSelectionController != null && _starterWordSelectionController.IsEnabled)
        {
            SetState(PreGameState.WordSelection);
            yield return _starterWordSelectionController.RunSelectionCoroutine(_starterWordSelectionData, _player, _narrativeIntroductionController?.BackgroundSprite, _narrativeIntroductionController?.FallbackBackgroundColor ?? Color.black);
        }

        SetState(PreGameState.EnteringGameplay);

        if (_sceneTransitionController == null || !_sceneTransitionController.TryFinishSceneTransition())
            SetState(PreGameState.Idle);
    }

    private void HandleTransitionFinished(string sceneName)
    {
        if (CurrentState == PreGameState.EnteringGameplay)
            SetState(PreGameState.Idle);
    }

    private void StopPreGameCoroutine()
    {
        if (_preGameCoroutine == null)
            return;

        StopCoroutine(_preGameCoroutine);
        _preGameCoroutine = null;
    }

    private void SetState(PreGameState nextState)
    {
        CurrentState = nextState;
    }
}
