using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class ParallelWorldPanel : MonoBehaviour, IMajorPanel
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

    public PanelId Id => PanelId.ParallelWorld;
    public bool IsOpen => _root.gameObject.activeSelf;
    public bool PausesGame => false;
    public bool BlocksGameplayInput => true;

    private void Awake()
    {
        _root.gameObject.SetActive(false);

        _mazeButton.onClick.AddListener(() => OnClickStartMinigame("MazeMinigame", levelCost: 2));
        _puzzleButton.onClick.AddListener(() => OnClickStartMinigame("PuzzleMinigame", levelCost: 3));
        _exitParallelWorld.onClick.AddListener(OnClickExit);
    }

    private void OnEnable()
    {
        _player.Data.OnMemoryXPChanged += HandleMemoryChanged;
    }

    private void OnDisable()
    {
        _player.Data.OnMemoryXPChanged -= HandleMemoryChanged;
    }

    public void Open()
    {
        _root.gameObject.SetActive(true);
        RefreshAll();
    }

    public void Close()
    {
        _root.gameObject.SetActive(false);
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
        var lvl = _player.Data.CurrentMemoryLevel;
        var cur = _player.Data.CurrentMemoryXP;
        var max = _player.Data.MaxMemoryXP;

        _memoryLevelText.text = $"Memory Lv. {lvl}";
        _memoryXPText.text = $"XP: {cur} / {max}";
    }

    private void RefreshMinigameButtons()
    {
        var lvl = _player.Data.CurrentMemoryLevel;

        // TODO: Modify level requirements
        _mazeButton.interactable = lvl >= 0;
        _puzzleButton.interactable = lvl >= 0;
    }

    public void OnClickStartMinigame(string minigameId, int levelCost)
    {
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
        PanelManager.Instance.CloseCurrentMajorPanel();
    }
}
