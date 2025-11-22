using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public sealed class DialogueController : MonoBehaviour
{
    public static DialogueController Instance { get; private set; }

    [Header("UI")]
    [SerializeField] private GameObject _rootPanel;
    [SerializeField] private TMP_Text _nameText;
    [SerializeField] private TMP_Text _lineText;
    [SerializeField] private Image _portraitImage;

    [Header("Input")]
    [SerializeField] private KeyCode _advanceKey = KeyCode.X;

    [Header("Audio")]
    [SerializeField] private AudioSource _voiceSource;

    private NPCDialogue _currentDialogue;
    private HumanNpcCore _currentNpc;

    private int _currentLineIndex;
    private Coroutine _typingCoroutine;

    private bool _isActive;
    private bool _isTyping;
    private bool _ignoreNextAdvanceInput;

    private bool _autoProgressActive;
    private float _autoProgressTimer;

    public bool IsDialogueActive => _isActive;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (_rootPanel != null)
        {
            _rootPanel.SetActive(false);
        }
    }

    private void Update()
    {
        if (!_isActive)
            return;

        // handle player input to advance dialogue
        if (Input.GetKeyDown(_advanceKey))
        {
            if (_ignoreNextAdvanceInput)
            {
                _ignoreNextAdvanceInput = false;
                return;
            }

            HandleAdvanceInput();
        }

        // handle auto progress
        if (_autoProgressActive && !_isTyping)
        {
            _autoProgressTimer -= Time.unscaledDeltaTime;
            if (_autoProgressTimer <= 0f)
            {
                _autoProgressActive = false;
                AdvanceToNextLine();
            }
        }
    }


    public void StartDialogue(NPCDialogue dialogue, HumanNpcCore npc)
    {
        if (_isActive)
            return;

        _currentDialogue = dialogue;
        _currentNpc = npc;
        _currentLineIndex = 0;

        _isActive = true;
        _isTyping = false;
        _autoProgressActive = false;

        // set to ignore any advance input the moment dialogue starts
        // so the first line doesn't get skipped
        _ignoreNextAdvanceInput = true;

        if (_rootPanel != null) 
        {
            _rootPanel.SetActive(true);
        }
        if (_nameText != null) 
        {
            _nameText.text = _currentDialogue.NpcName;
        }
        if (_portraitImage != null)
        {
            _portraitImage.sprite = _currentDialogue.NpcPortrait;
        }

        // pause the game while dialogue is open
        GameStateManager.SetPause(true);

        // disable NPC AI state machine while talking
        if (_currentNpc != null) 
        {
            _currentNpc.enabled = false;
        }
        
        StartCurrentLine();
    }

    public void EndDialogue()
    {
        if (!_isActive)
            return;

        _isActive = false;
        _ignoreNextAdvanceInput = false;

        if (_rootPanel != null)
        {
            _rootPanel.SetActive(false);
        }

        // resume the game
        GameStateManager.SetPause(false);

        // re-enable NPC AI
        if (_currentNpc != null)
        {
            _currentNpc.enabled = true;
        }
            
        _currentDialogue = null;
        _currentNpc = null;

        if (_typingCoroutine != null)
        {
            StopCoroutine(_typingCoroutine);
            _typingCoroutine = null;
        }

        _isTyping = false;
        _autoProgressActive = false;
    }

    private void StartCurrentLine()
    {
        if (_currentDialogue == null || _currentLineIndex >= _currentDialogue.DialogueLines.Count)
        {
            EndDialogue();
            return;
        }

        string line = _currentDialogue.GetDialogueLine(_currentLineIndex);
        bool autoProgress = _currentDialogue.IsAutoProgressLine(_currentLineIndex);

        if (_typingCoroutine != null)
        {
            StopCoroutine(_typingCoroutine);
        }

        _typingCoroutine = StartCoroutine(TypeLineCoroutine(line, autoProgress));
    }

    private IEnumerator TypeLineCoroutine(string line, bool autoProgress)
    {
        _isTyping = true;
        _autoProgressActive = false;

        if (_lineText != null)
        {
            _lineText.text = string.Empty;
        }
            
        if (string.IsNullOrEmpty(line))
        {
            _isTyping = false;
            if (autoProgress)
            {
                _autoProgressTimer = _currentDialogue.AutoProgressDelay;
                _autoProgressActive = true;
            }
            yield break;
        }

        foreach (char ch in line)
        {
            _lineText.text += ch;
            PlayVoiceTick();

            yield return new WaitForSecondsRealtime(_currentDialogue.TypingSpeed);

            if (!_isTyping)
            {
                yield break;
            }
        }

        _isTyping = false;

        if (autoProgress)
        {
            _autoProgressTimer = _currentDialogue.AutoProgressDelay;
            _autoProgressActive = true;
        }
    }

    private void HandleAdvanceInput()
    {
        if (_currentDialogue == null)
            return;

        if (_isTyping)
        {
            // finish the current line instantly
            CompleteCurrentLineInstant();
        }
        else
        {
            // manual advance, cancel any pending auto progress
            _autoProgressActive = false;
            AdvanceToNextLine();
        }
    }

    private void CompleteCurrentLineInstant()
    {
        if (_typingCoroutine != null)
        {
            StopCoroutine(_typingCoroutine);
            _typingCoroutine = null;
        }

        if (_lineText != null && _currentDialogue != null)
            _lineText.text = _currentDialogue.GetDialogueLine(_currentLineIndex);

        _isTyping = false;

        bool auto = _currentDialogue.IsAutoProgressLine(_currentLineIndex);
        if (auto)
        {
            _autoProgressTimer = _currentDialogue.AutoProgressDelay;
            _autoProgressActive = true;
        }
    }

    private void AdvanceToNextLine()
    {
        _currentLineIndex++;
        StartCurrentLine();
    }

    private void PlayVoiceTick()
    {
        if (_voiceSource == null || _currentDialogue == null)
            return;

        var clip = _currentDialogue.VoiceSound;
        if (clip == null)
            return;

        _voiceSource.pitch = _currentDialogue.VoicePitch;
        _voiceSource.PlayOneShot(clip);
    }
}
