using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class PreGameController : MonoBehaviour
{
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

    [Header("Narrative Intro")]
    [SerializeField] private bool _enableNarrativeIntro = true;
    [SerializeField] private DialogueData _introDialogueData;
    [SerializeField] private GameObject _introRoot;
    [SerializeField] private Image _introBackgroundImage;
    [SerializeField] private TMP_Text _introMessageText;
    [SerializeField] private TMP_Text _introContinueHintText;

    [Header("Word selection")]
    [SerializeField] private bool _enableWordSelection = true;

    public PreGameState CurrentState { get; private set; } = PreGameState.Idle;
    public bool IsPreGameEnabled => _enablePreGame;

    private string _expectedGameplaySceneName;
    private Coroutine _preGameCoroutine;
    private bool _blockAdvanceUntilKeyRelease;

    private void Awake()
    {
        if (_sceneTransitionController == null)
            _sceneTransitionController = GetComponent<SceneTransitionController>();

        SetIntroVisible(false);
    }

    private void OnEnable()
    {
        if (_sceneTransitionController != null)
        {
            _sceneTransitionController.TransitionMiddleCompleted += HandleTransitionMiddleCompleted;
            _sceneTransitionController.TransitionFinished += HandleTransitionFinished;
        }
    }

    private void OnDisable()
    {
        if (_sceneTransitionController != null)
        {
            _sceneTransitionController.TransitionMiddleCompleted -= HandleTransitionMiddleCompleted;
            _sceneTransitionController.TransitionFinished -= HandleTransitionFinished;
        }

        StopPreGameCoroutine();
        SetIntroVisible(false);
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
        if (_enableNarrativeIntro)
            yield return RunNarrativeIntroCoroutine();

        if (_enableWordSelection)
            yield return RunWordSelectionCoroutine();

        SetState(PreGameState.EnteringGameplay);

        if (_sceneTransitionController == null || !_sceneTransitionController.TryFinishSceneTransition())
            SetState(PreGameState.Idle);
    }

    private IEnumerator RunNarrativeIntroCoroutine()
    {
        SetState(PreGameState.NarrativeIntro);
        SetIntroVisible(true);
        ApplyIntroBackgroundVisual();
        UpdateContinueHintLabel();

        for (int i = 0; i < _introDialogueData.Lines.Count; i++)
        {
            string message = _introDialogueData.Lines[i] ?? string.Empty;
            yield return TypeMessageCoroutine(message);
            yield return WaitForMessageAdvanceCoroutine();
        }

        SetIntroVisible(false);
    }

    private IEnumerator TypeMessageCoroutine(string message)
    {
        _introMessageText.text = string.Empty;
        _blockAdvanceUntilKeyRelease = false;

        if (string.IsNullOrEmpty(message))
            yield break;

        int characterCount = 0;
        float progress = 0f;

        while (characterCount < message.Length)
        {
            if (Input.GetKeyDown(_introDialogueData.AdvanceKey))
            {
                _introMessageText.text = message;
                _blockAdvanceUntilKeyRelease = true;
                yield break;
            }

            float delta = Mathf.Min(Time.unscaledDeltaTime, _introDialogueData.MaxDeltaTime);
            progress += delta * _introDialogueData.CharactersPerSecond;

            int targetCount = Mathf.Clamp(Mathf.FloorToInt(progress), 0, message.Length);
            if (targetCount != characterCount)
            {
                characterCount = targetCount;
                _introMessageText.text = message.Substring(0, characterCount);
            }

            yield return null;
        }

        _introMessageText.text = message;
    }

    private IEnumerator WaitForMessageAdvanceCoroutine()
    {
        if (_blockAdvanceUntilKeyRelease)
        {
            while (Input.GetKey(_introDialogueData.AdvanceKey))
                yield return null;

            _blockAdvanceUntilKeyRelease = false;
        }

        if (_introDialogueData.AutoAdvanceDelay <= 0f)
            yield break;

        float elapsed = 0f;
        while (elapsed < _introDialogueData.AutoAdvanceDelay)
        {
            if (Input.GetKeyDown(_introDialogueData.AdvanceKey))
                yield break;

            elapsed += Mathf.Min(Time.unscaledDeltaTime, _introDialogueData.MaxDeltaTime);
            yield return null;
        }
    }

    private IEnumerator RunWordSelectionCoroutine()
    {
        // TODO: Implement word selcection here
        SetState(PreGameState.WordSelection);
        yield return null;
    }

    private void HandleTransitionFinished(string sceneName)
    {
        if (CurrentState == PreGameState.EnteringGameplay)
            SetState(PreGameState.Idle);
    }

    private void ApplyIntroBackgroundVisual()
    {
        bool hasSprite = _introDialogueData.BackgroundSprite != null;
        _introBackgroundImage.sprite = _introDialogueData.BackgroundSprite;
        _introBackgroundImage.color = hasSprite ? _introDialogueData.BackgroundColorWhenSpriteAssigned : _introDialogueData.BackgroundFallbackColor;
    }

    private void UpdateContinueHintLabel()
    {
        if (_introContinueHintText == null)
            return;

        string advanceKey = _introDialogueData != null ? _introDialogueData.AdvanceKey.ToString() : "Key";
        _introContinueHintText.enabled = true;
        _introContinueHintText.text = $"Press \"{advanceKey}\" to continue";
    }

    private void SetIntroVisible(bool visible)
    {
        if (_introRoot != null)
            _introRoot.SetActive(visible);
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
