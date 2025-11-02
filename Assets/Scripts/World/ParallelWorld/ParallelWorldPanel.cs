using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class ParallelWorldPanel : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Player _player;
    [SerializeField] private RectTransform _root;
    [SerializeField] private Image _background;

    [Header("Memory Info")]
    [SerializeField] private TMP_Text _memoryLevelText;
    [SerializeField] private TMP_Text _memoryXPText;

    [Header("Minigame Buttons")]
    [SerializeField] private Button _mazeButton;
    [SerializeField] private Button _puzzleButton;

    [Header("Controls")]
    [SerializeField] private Button _exitParallelWorld;

    private bool _isOpen;

    private void Awake()
    {
        if (_root != null)
            _root.gameObject.SetActive(false);

        if (_mazeButton != null)
            _mazeButton.onClick.AddListener(() => OnClickStartMinigame("MazeMinigame", levelCost: 2));

        if (_puzzleButton != null)
            _puzzleButton.onClick.AddListener(() => OnClickStartMinigame("PuzzleMinigame", levelCost: 3));

        if (_exitParallelWorld != null)
            _exitParallelWorld.onClick.AddListener(OnClickExit);
    }

    private void OnEnable()
    {
        if (_player?.Data != null)
            _player.Data.OnMemoryXPChanged += HandleMemoryChanged;
    }

    private void OnDisable()
    {
        if (_player?.Data != null)
            _player.Data.OnMemoryXPChanged -= HandleMemoryChanged;
    }

    public void Open()
    {
        if (_isOpen) 
            return;
        _isOpen = true;

        if (_root == null) 
            return;
        _root.gameObject.SetActive(true);

        GameStateManager.SetPause(paused: true);

        RefreshAll();
    }

    public void Close()
    {
        if (!_isOpen) 
            return;
        _isOpen = false;

        if (_root == null) 
            return;
        _root.gameObject.SetActive(false);

        GameStateManager.SetPause(paused: false);
    }


    private void HandleMemoryChanged(int currentXP, int maxXP, int currentLevel)
    {
        RefreshMemoryUI();
        RefreshMinigameButtons();
    }

    private void RefreshAll()
    {
        RefreshMemoryUI();
        RefreshMinigameButtons();
    }

    private void RefreshMemoryUI()
    {
        if (_player?.Data == null) return;

        var lvl = _player.Data.CurrentMemoryLevel;
        var cur = _player.Data.CurrentMemoryXP;
        var max = _player.Data.MaxMemoryXP;

        if (_memoryLevelText)
        {
            _memoryLevelText.text = $"Memory Lv. {lvl}";
        }
        if (_memoryXPText)
        {
            _memoryXPText.text = $"XP: {cur} / {max}";
        }
    }

    private void RefreshMinigameButtons()
    {
        if (_player?.Data == null)
            return;

        var lvl = _player.Data.CurrentMemoryLevel;

        // TODO: Modify level requirements
        if (_mazeButton)
            _mazeButton.interactable = lvl >= 0;
        if (_puzzleButton)
            _puzzleButton.interactable = lvl >= 0;
    }

    public void OnClickStartMinigame(string minigameId, int levelCost)
    {
        if (_player?.Data == null) 
            return;

        if (!_player.Data.TrySpendMemoryLevels(levelCost, baseMax: _player.Data.MaxMemoryXP, growthPerLevel: 25))
        {
            Debug.Log($"[ParallelWorld] Not enough Memory levels for '{minigameId}' (cost {levelCost}).");
            return;
        }

        Debug.Log($"[ParallelWorld] START '{minigameId}' (paid {levelCost} levels). TODO: teleport/open scene.");
        RefreshAll();
    }

    public void OnClickExit()
    {
        Close();
    }
}
