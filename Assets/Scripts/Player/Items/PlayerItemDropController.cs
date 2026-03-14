using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class PlayerItemDropController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Player _player;
    [SerializeField] private BackpackPanel _backpackPanel;
    [SerializeField] private ItemDropSpawner _worldItemSpawner;
    [SerializeField] private GraphicRaycaster _uiRaycaster; //  UI Canvas

    [Header("Input")]
    [SerializeField] private GameplayInputBindingsData _inputBindings;

    [Header("Throw")]
    [SerializeField] private float _throwSpawnDistance = 0.6f;

    // cached list for UI raycasting
    private readonly List<RaycastResult> _raycastResults = new List<RaycastResult>();

    private void Awake()
    {
        if (_player == null) 
            _player = GetComponent<Player>() ?? GetComponentInParent<Player>();

        if (_worldItemSpawner == null)
        {
            var arr = FindObjectsByType<ItemDropSpawner>(FindObjectsSortMode.None);
            if (arr != null && arr.Length > 0)
                _worldItemSpawner = arr[0];
        }
        if (_backpackPanel == null)
        {
            var arr = FindObjectsByType<BackpackPanel>(FindObjectsSortMode.None);
            if (arr != null && arr.Length > 0)
                _backpackPanel = arr[0];
        }
        if (_uiRaycaster == null)
        {
            var arr = FindObjectsByType<GraphicRaycaster>(FindObjectsSortMode.None);
            if (arr != null && arr.Length > 0)
                _uiRaycaster = arr[0];
        }
    }

    private void Update()
    {
        if (_player.Inventory == null) 
            return;
        if (GameStateManager.IsGamePaused) 
            return;

        if (Input.GetKeyDown(_inputBindings.DropKey))
        {
            var dropAll = Input.GetKey(_inputBindings.DropAllModifierKey);
            HandleDrop(dropAll);
        }
    }

    private void HandleDrop(bool dropAll)
    {
        // determine source slot index (hotbar or backpack)
        var sourceIndex = ResolveSourceSlotIndex();
        if (sourceIndex < 0) 
            return;

        // get item from inventory
        var item = _player.Inventory.GetItemAt(sourceIndex);
        if (item.IsEmpty) 
            return;

        // spawn the dropping item in the world
        var toDrop = dropAll ? item.Amount : 1;
        var droppingItem = new InventoryItem(item.Item, toDrop);

        // compute spawn position and direction
        AimUtils.ComputeAim2D(_player.transform, _throwSpawnDistance, out var direction, out var spawnPos);

        // spawn the item in the world
        _worldItemSpawner.Spawn(droppingItem, spawnPos, direction);

        // reduce or clear the source stack in inventory
        var remaining = item.Amount - toDrop;
        if (remaining <= 0)
            _player.Inventory.ClearItemAt(sourceIndex);
        else
            _player.Inventory.SetItemAt(sourceIndex, item.WithAmount(remaining));
    }

    /// <summary>
    /// If backpack UI is open, use the hovered Slot (hotbar or backpack).
    /// Otherwise use currently selected hotbar index.
    /// </summary>
    private int ResolveSourceSlotIndex()
    {
        // inventory open, then return on hovered slot index
        if (_backpackPanel != null && _backpackPanel.IsOpen)
        {
            var hovered = RaycastSlotUnderMouse();
            return hovered != null ? hovered.SlotIndex : -1;
        }

        // inventory closed, then return selected hotbar slot index
        return _player.Inventory.SelectedHotbarIndex;
    }

    /// <summary>
    /// Raycasts UI under the mouse using the assigned GraphicRaycaster and returns the first Slot.
    /// </summary>
    private Slot RaycastSlotUnderMouse()
    {
        if (_uiRaycaster == null || EventSystem.current == null)
            return null;

        var data = new PointerEventData(EventSystem.current) 
        { 
            position = Input.mousePosition
        };

        _raycastResults.Clear();
        _uiRaycaster.Raycast(data, _raycastResults);

        for (int i = 0; i < _raycastResults.Count; i++)
        {
            if (_raycastResults[i].gameObject == null) 
                continue;

            var slot = _raycastResults[i].gameObject.GetComponentInParent<Slot>();
            if (slot != null)
                return slot;
        }
        return null;
    }
}
