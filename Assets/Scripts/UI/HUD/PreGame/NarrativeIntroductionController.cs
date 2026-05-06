using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class NarrativeIntroductionController : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private bool _enableNarrativeIntro = true;

    [Header("Data")]
    [SerializeField] private DialogueData _introDialogueData;

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

        _introMessageText.text = string.Empty;

        if (string.IsNullOrEmpty(message))
            yield break;

        int characterCount = 0;
        float progress = 0f;

        while (characterCount < message.Length)
        {
            if (Input.GetKeyDown(_introDialogueData.AdvanceKey))
            {
                _introMessageText.text = message;
                _currentMessageWasCompletedByAdvanceKey = true;
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
