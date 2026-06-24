using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Runs the pre-game narrative intro with background visuals, typewriter text, and advance input.
/// </summary>
[DisallowMultipleComponent]
public sealed class NarrativeIntroductionController : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private bool _enableNarrativeIntro = true;

    [Header("Data")]
    [SerializeField] private NarrativeIntroductionData _introDialogueData;

    [Header("References")]
    [SerializeField] private GameObject _introRoot;
    [SerializeField] private Image _introBackgroundImage;
    [SerializeField] private TMP_Text _introMessageText;
    [SerializeField] private TMP_Text _introContinueHintText;

    public bool IsEnabled => _enableNarrativeIntro && _introDialogueData != null;
    public Sprite BackgroundSprite => _introDialogueData != null ? _introDialogueData.BackgroundSprite : null;
    public Color FallbackBackgroundColor => _introDialogueData != null ? _introDialogueData.BackgroundFallbackColor : Color.black;

    private bool _currentMessageWasCompletedByAdvanceKey;

    private void Awake()
    {
        Hide();
    }

    public IEnumerator RunIntroCoroutine()
    {
        if (!IsEnabled)
            yield break;

        Show();
        ApplyIntroBackgroundVisual();
        UpdateContinueHintLabel();

        for (int i = 0; i < _introDialogueData.Lines.Count; i++)
        {
            _currentMessageWasCompletedByAdvanceKey = false;
            string message = _introDialogueData.Lines[i] ?? string.Empty;
            yield return TypeMessageCoroutine(message);
            yield return WaitForMessageAdvanceCoroutine();
        }

        Hide();
    }

    public void Hide()
    {
        SetIntroVisible(false);
    }

    private void Show()
    {
        SetIntroVisible(true);
    }

    private IEnumerator TypeMessageCoroutine(string message)
    {
        if (_introMessageText == null)
            yield break;

        _introMessageText.text = message;
        _introMessageText.maxVisibleCharacters = 0;

        if (string.IsNullOrEmpty(message))
            yield break;

        _introMessageText.ForceMeshUpdate();

        int totalCharacterCount = _introMessageText.textInfo.characterCount;
        int visibleCharacterCount = 0;
        float progress = 0f;

        while (visibleCharacterCount < totalCharacterCount)
        {
            if (Input.GetKeyDown(_introDialogueData.AdvanceKey))
            {
                ShowFullMessage();
                _currentMessageWasCompletedByAdvanceKey = true;
                yield break;
            }

            float delta = Mathf.Min(Time.unscaledDeltaTime, _introDialogueData.MaxDeltaTime);
            progress += delta * _introDialogueData.CharactersPerSecond;

            int targetCount = Mathf.Clamp(Mathf.FloorToInt(progress), 0, totalCharacterCount);
            if (targetCount != visibleCharacterCount)
            {
                visibleCharacterCount = targetCount;
                _introMessageText.maxVisibleCharacters = visibleCharacterCount;
            }

            yield return null;
        }

        ShowFullMessage();
    }

    private void ShowFullMessage()
    {
        if (_introMessageText == null)
            return;

        _introMessageText.maxVisibleCharacters = int.MaxValue;
    }

    private IEnumerator WaitForMessageAdvanceCoroutine()
    {
        if (_currentMessageWasCompletedByAdvanceKey)
        {
            while (Input.GetKey(_introDialogueData.AdvanceKey))
                yield return null;
        }

        yield return null;

        if (_introDialogueData.AutoAdvanceDelay <= 0f)
            yield break;

        float elapsed = 0f;
        while (elapsed < _introDialogueData.AutoAdvanceDelay)
        {
            if (Input.GetKeyDown(_introDialogueData.AdvanceKey))
            {
                while (Input.GetKey(_introDialogueData.AdvanceKey))
                    yield return null;

                yield return null;
                yield break;
            }

            elapsed += Mathf.Min(Time.unscaledDeltaTime, _introDialogueData.MaxDeltaTime);
            yield return null;
        }
    }

    private void ApplyIntroBackgroundVisual()
    {
        if (_introBackgroundImage == null)
            return;

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
}
