using System;
using UnityEngine;

public sealed class PanelManager : MonoBehaviour
{
    public static PanelManager Instance { get; private set; }

    [Header("Input")]
    [SerializeField] private MajorPanelKeybindsData _panelKeybinds;

    [Header("Input Handlers")]
    [SerializeField] private SpellCastingPanelController _spellCastingPanel;

    [Header("Panels - Major")]
    [SerializeField] private BackpackPanel _backpackPanel;
    [SerializeField] private DeathChestPanel _deathChestPanel;
    [SerializeField] private WorldMapPanelController _mapPanel;
    [SerializeField] private OverworldSettingsController _settingsPanel;
    [SerializeField] private CraftingPanel _craftingPanel;
    [SerializeField] private WordShopPanelController _wordShopPanel;

    [Header("Panels - Secondary")]
    [SerializeField] private EquipmentPanel _equipmentPanel;

    private PanelId? _currentPanelId;
    private IInventory _currentDeathChestInventory;

    private IPanel[] _inventoryGroup;
    private IPanel[] _deathChestGroup;
    private IPanel[] _mapGroup;
    private IPanel[] _settingsGroup;
    private IPanel[] _craftingGroup;
    private IPanel[] _wordShopGroup;

    public event Action<IInventory> OnDeathChestClosed;

    public bool BlocksGameplayInput => _currentPanelId.HasValue && GetMajorPanel(_currentPanelId.Value).BlocksGameplayInput;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        _inventoryGroup = new IPanel[] { _backpackPanel, _equipmentPanel };
        _deathChestGroup = new IPanel[] { _deathChestPanel, _backpackPanel, _equipmentPanel };
        _mapGroup = new IPanel[] { _mapPanel };
        _settingsGroup = new IPanel[] { _settingsPanel };
        _craftingGroup = new IPanel[] { _craftingPanel };
        _wordShopGroup = new IPanel[] { _wordShopPanel };

        _currentPanelId = null;
        _currentDeathChestInventory = null;

        GameStateManager.SetPause(false);
    }

    private void Update()
    {
        HandleInput();
    }

    private void HandleInput()
    {
        if (SceneLoader.Instance != null && SceneLoader.Instance.IsTransitionActive)
            return;

        if (Input.GetKeyDown(_panelKeybinds.CloseOrPause))
        {
            if (_currentPanelId.HasValue)
            {
                CloseCurrentMajorPanel();
                return;
            }

            if (_spellCastingPanel != null && _spellCastingPanel.TryCancelActiveCasting())
                return;

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
        if (SceneLoader.Instance != null && SceneLoader.Instance.IsTransitionActive)
            return;

        ItemInteractionController.Instance?.ResolveHeldItemToInventoryOrDrop();

        if (_currentPanelId.HasValue)
            CloseGroup(GetGroup(_currentPanelId.Value));

        _currentDeathChestInventory = null;
        _currentPanelId = id;

        OpenGroup(GetGroup(id));
        ApplyPauseRule();
    }

    public bool ShouldBlockHotbarScrollInput()
    {
        return _currentPanelId.HasValue && _currentPanelId.Value == PanelId.Map;
    }

    public void CloseCurrentMajorPanel()
    {
        CloseCurrentMajorPanel(force: false);
    }

    public void CloseCurrentMajorPanel(bool force)
    {
        if (!_currentPanelId.HasValue)
            return;

        if (!force && SceneLoader.Instance != null && SceneLoader.Instance.IsTransitionActive)
            return;

        var closingPanelId = _currentPanelId.Value;
        var closingDeathChestInventory = closingPanelId == PanelId.DeathChest ? _currentDeathChestInventory : null;

        ItemInteractionController.Instance?.ResolveHeldItemToInventoryOrDrop();

        CloseGroup(GetGroup(closingPanelId));

        _currentPanelId = null;
        _currentDeathChestInventory = null;

        if (closingDeathChestInventory != null)
            OnDeathChestClosed?.Invoke(closingDeathChestInventory);

        GameStateManager.SetPause(false);
    }

    public void OpenDeathChest(IInventory inventory)
    {
        if (SceneLoader.Instance != null && SceneLoader.Instance.IsTransitionActive)
            return;

        ItemInteractionController.Instance?.ResolveHeldItemToInventoryOrDrop();

        if (_currentPanelId.HasValue)
            CloseGroup(GetGroup(_currentPanelId.Value));

        _currentDeathChestInventory = inventory;
        _deathChestPanel.Bind(inventory);

        _currentPanelId = PanelId.DeathChest;

        OpenGroup(_deathChestGroup);
        ApplyPauseRule();
    }

    public void InteractWithDeathChest(IInventory inventory)
    {
        if (_currentPanelId.HasValue)
        {
            CloseCurrentMajorPanel();
            return;
        }

        OpenDeathChest(inventory);
    }

    public void CloseDeathChestIfBoundTo(IInventory inventory)
    {
        if (!_currentPanelId.HasValue || _currentPanelId.Value != PanelId.DeathChest)
            return;

        if (!ReferenceEquals(_currentDeathChestInventory, inventory))
            return;

        CloseCurrentMajorPanel();
    }

    private IPanel[] GetGroup(PanelId id)
    {
        return id switch
        {
            PanelId.Inventory => _inventoryGroup,
            PanelId.DeathChest => _deathChestGroup,
            PanelId.Map => _mapGroup,
            PanelId.Settings => _settingsGroup,
            PanelId.Crafting => _craftingGroup,
            PanelId.WordShop => _wordShopGroup,
            _ => throw new ArgumentOutOfRangeException(nameof(id), id, null)
        };
    }

    private static void OpenGroup(IPanel[] group)
    {
        foreach (var panel in group)
        {
            if (panel == null)
                continue;

            panel.Open();
        }
    }

    private static void CloseGroup(IPanel[] group)
    {
        foreach (var panel in group)
        {
            if (panel == null)
                continue;

            panel.Close();
        }
    }

    private IMajorPanel GetMajorPanel(PanelId id)
    {
        return id switch
        {
            PanelId.Inventory => _backpackPanel,
            PanelId.DeathChest => _deathChestPanel,
            PanelId.Map => _mapPanel,
            PanelId.Settings => _settingsPanel,
            PanelId.Crafting => _craftingPanel,
            PanelId.WordShop => _wordShopPanel,
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
