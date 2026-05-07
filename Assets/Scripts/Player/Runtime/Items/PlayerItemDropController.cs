using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Drops player inventory items from the selected hotbar slot or hovered backpack slot into the world.
/// </summary>
[DisallowMultipleComponent]
public sealed class PlayerItemDropController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Player _player;
    [SerializeField] private PlayerInputController _inputController;
    [SerializeField] private BackpackPanel _backpackPanel;
    [SerializeField] private WorldItemSpawner _worldItemSpawner;
    [SerializeField] private GraphicRaycaster _uiRaycaster;
    [SerializeField] private Camera _worldCamera;

    [Header("Throw")]
    [SerializeField] private float _throwSpawnDistance = 0.6f;

    private readonly List<RaycastResult> _raycastResults = new();

    private void OnEnable()
    {
        if (_inputController != null)
            _inputController.DropPressed += HandleDrop;
    }

    private void OnDisable()
    {
        if (_inputController != null)
            _inputController.DropPressed -= HandleDrop;
    }

    private void HandleDrop(bool dropAll)
    {
        var sourceIndex = ResolveSourceSlotIndex();
        if (sourceIndex < 0)
            return;

        var item = _player.Inventory.GetItemAt(sourceIndex);
        if (item.IsEmpty)
            return;

        var amountToDrop = dropAll ? item.Amount : 1;
        var droppingItem = new InventoryItem(item.Item, amountToDrop);

        ResolveDropSpawn(out var direction, out var spawnPosition);
        _worldItemSpawner.Spawn(droppingItem, spawnPosition, direction);

        var remainingAmount = item.Amount - amountToDrop;
        if (remainingAmount <= 0)
            _player.Inventory.ClearItemAt(sourceIndex);
        else
            _player.Inventory.SetItemAt(sourceIndex, item.WithAmount(remainingAmount));
    }

    private void ResolveDropSpawn(out Vector2 direction, out Vector3 spawnPosition)
    {
        var originPosition = _player.transform.position;
        var mouseWorldPosition = _worldCamera.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPosition.z = 0f;

        direction = (mouseWorldPosition - originPosition).normalized;
        if (direction.sqrMagnitude < Mathf.Epsilon)
            direction = Vector2.down;

        spawnPosition = originPosition + (Vector3)(direction * _throwSpawnDistance);
    }

    private int ResolveSourceSlotIndex()
    {
        if (_backpackPanel.IsOpen)
        {
            var hovered = RaycastSlotUnderMouse();
            return hovered != null ? hovered.SlotIndex : -1;
        }

        return _player.Inventory.SelectedHotbarIndex;
    }

    private InventorySlotView RaycastSlotUnderMouse()
    {
        if (EventSystem.current == null)
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

            var slot = _raycastResults[i].gameObject.GetComponentInParent<InventorySlotView>();
            if (slot != null)
                return slot;
        }

        return null;
    }
}
