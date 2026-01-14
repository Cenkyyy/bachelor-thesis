using System;
using UnityEngine;

public sealed class PanelManager : MonoBehaviour
{
    public static PanelManager Instance { get; private set; }

    [Header("Input")]
    [SerializeField] private MajorPanelKeybinds _panelKeybinds;

    [Header("Panels - Major")]
    [SerializeField] private BackpackPanel _backpackPanel;
    [SerializeField] private ChestPanel _chestPanel;
    [SerializeField] private WorldMapPanelController _mapPanel;
    [SerializeField] private SettingsController _settingsPanel;
    [SerializeField] private ParallelWorldPanel _parallelWorldPanel;

    [Header("Panels - Secondary")]
    [SerializeField] private CharacterPanel _characterPanel;

    private PanelId? _currentPanelId;
    private IInventory _currentChestInventory;

    private IPanel[] _inventoryGroup;
    private IPanel[] _chestGroup;
    private IPanel[] _mapGroup;
    private IPanel[] _settingsGroup;
    private IPanel[] _parallelWorldGroup;

    public bool BlocksGameplayInput =>
        _currentPanelId.HasValue && GetMajorPanel(_currentPanelId.Value).BlocksGameplayInput;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        _inventoryGroup = new IPanel[] { _backpackPanel, _characterPanel };
        _chestGroup = new IPanel[] { _chestPanel, _backpackPanel, _characterPanel };
        _mapGroup = new IPanel[] { _mapPanel };
        _settingsGroup = new IPanel[] { _settingsPanel };
        _parallelWorldGroup = new IPanel[] { _parallelWorldPanel };

        _currentPanelId = null;
        _currentChestInventory = null;

        GameStateManager.SetPause(false);
    }

    private void Update()
    {
        HandleInput();
    }

    private void HandleInput()
    {
        if (Input.GetKeyDown(_panelKeybinds.CloseOrPause))
        {
            if (_currentPanelId.HasValue)
            {
                CloseCurrentMajorPanel();
                return;
            }

            OpenMajorPanel(PanelId.Settings);
            return;
        }

        if (Input.GetKeyDown(_panelKeybinds.Inventory))
        {
            HandlePanelInteraction(PanelId.Inventory);
            return;
        }

        if (Input.GetKeyDown(_panelKeybinds.Map))
        {
            HandlePanelInteraction(PanelId.Map);
            return;
        }

        if (Input.GetKeyDown(_panelKeybinds.ParallelWorld))
        {
            HandlePanelInteraction(PanelId.ParallelWorld);
            return;
        }

        if (Input.GetKeyDown(_panelKeybinds.Crafting))
        {
            HandlePanelInteraction(PanelId.Crafting);
            return;
        }
    }

    private void HandlePanelInteraction(PanelId requested)
    {
        if (_currentPanelId.HasValue)
        {
            CloseCurrentMajorPanel();
            return;
        }

        OpenMajorPanel(requested);
    }

    public void OpenMajorPanel(PanelId id)
    {
        ItemInteractionController.Instance?.ResolveHeldItemToInventoryOrDrop();

        if (_currentPanelId.HasValue)
            CloseGroup(GetGroup(_currentPanelId.Value));

        _currentChestInventory = null;
        _currentPanelId = id;

        OpenGroup(GetGroup(id));
        ApplyPauseRule();
    }

    public void CloseCurrentMajorPanel()
    {
        if (!_currentPanelId.HasValue)
            return;

        ItemInteractionController.Instance?.ResolveHeldItemToInventoryOrDrop();

        CloseGroup(GetGroup(_currentPanelId.Value));

        _currentPanelId = null;
        _currentChestInventory = null;

        GameStateManager.SetPause(false);
    }

    public void OpenChest(IInventory inventory)
    {
        ItemInteractionController.Instance?.ResolveHeldItemToInventoryOrDrop();

        if (_currentPanelId.HasValue)
            CloseGroup(GetGroup(_currentPanelId.Value));

        _currentChestInventory = inventory;
        _chestPanel.Bind(inventory);

        _currentPanelId = PanelId.Chest;

        OpenGroup(_chestGroup);
        ApplyPauseRule();
    }

    public void InteractWithChest(IInventory inventory)
    {
        if (_currentPanelId.HasValue)
        {
            CloseCurrentMajorPanel();
            return;
        }

        OpenChest(inventory);
    }

    public void CloseChestIfBoundTo(IInventory inventory)
    {
        if (!_currentPanelId.HasValue || _currentPanelId.Value != PanelId.Chest)
            return;

        if (!ReferenceEquals(_currentChestInventory, inventory))
            return;

        CloseCurrentMajorPanel();
    }

    private IPanel[] GetGroup(PanelId id)
    {
        return id switch
        {
            PanelId.Inventory => _inventoryGroup,
            PanelId.Chest => _chestGroup,
            PanelId.Map => _mapGroup,
            PanelId.Settings => _settingsGroup,
            PanelId.ParallelWorld => _parallelWorldGroup,
            PanelId.Crafting => throw new InvalidOperationException("Crafting is not wired in PanelManager yet."),
            _ => throw new ArgumentOutOfRangeException(nameof(id), id, null)
        };
    }

    private static void OpenGroup(IPanel[] group)
    {
        foreach (var panel in group)
        {
            panel.Open();
        }
    }

    private static void CloseGroup(IPanel[] group)
    {
        foreach (var panel in group)
        {
            panel.Close();
        }
    }

    private IMajorPanel GetMajorPanel(PanelId id)
    {
        return id switch
        {
            PanelId.Inventory => _backpackPanel,
            PanelId.Chest => _chestPanel,
            PanelId.Map => _mapPanel,
            PanelId.Settings => _settingsPanel,
            PanelId.ParallelWorld => _parallelWorldPanel,
            PanelId.Crafting => throw new InvalidOperationException("Crafting is not wired in PanelManager yet."),
            _ => throw new ArgumentOutOfRangeException(nameof(id), id, null)
        };
    }

    private void ApplyPauseRule()
    {
        if (!_currentPanelId.HasValue)
        {
            GameStateManager.SetPause(false);
            return;
        }

        GameStateManager.SetPause(GetMajorPanel(_currentPanelId.Value).PausesGame);
    }
}
