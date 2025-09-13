using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class PlayerItemDropController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Player player;
    [SerializeField] private BackpackPresenter backpackPresenter;
    [SerializeField] private WorldItemSpawner worldItemSpawner;
    [SerializeField] private GraphicRaycaster uiRaycaster; //  UI Canvas

    [Header("Input")]
    [SerializeField] private KeyCode dropKey = KeyCode.Q;

    [Header("Throw")]
    [Tooltip("How far from the player to spawn the dropped item (towards the mouse).")]
    [SerializeField] private float throwSpawnDistance = 0.6f;
    [Tooltip("Impulse applied to the dropped item (if it has a Rigidbody2D).")]
    [SerializeField] private float throwImpulse = 2.0f;

    private readonly List<RaycastResult> _raycastResults = new List<RaycastResult>();

    private void Reset()
    {
        if (player == null)
            player = GetComponent<Player>();
        if (uiRaycaster == null)
            uiRaycaster = FindObjectsByType<GraphicRaycaster>(FindObjectsSortMode.None)[0];
        if (backpackPresenter == null)
            backpackPresenter = FindObjectsByType<BackpackPresenter>(FindObjectsSortMode.None)[0];
        if (worldItemSpawner == null)
            worldItemSpawner = FindObjectsByType<WorldItemSpawner>(FindObjectsSortMode.None)[0];
    }

    private void Update()
    {
        if (player.Inventory == null) 
            return;
        if (GameStateManager.IsGamePaused) 
            return;

        if (Input.GetKeyDown(dropKey))
        {
            bool dropAll = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
            HandleDrop(dropAll);
        }
    }

    private void HandleDrop(bool dropAll)
    {
        var sourceIndex = ResolveSourceSlotIndex();
        if (sourceIndex < 0) 
            return;

        var item = player.Inventory.GetItemAt(sourceIndex);
        if (item.IsEmpty) 
            return;

        var toDrop = dropAll ? item.Amount : 1;
        var droppingItem = new InventoryItem(item.ItemSO, toDrop);

        // spawn the dropped item in the world
        var playerPos = player.transform.position;
        var mouseWorld = Camera.main ? Camera.main.ScreenToWorldPoint(Input.mousePosition) : playerPos + Vector3.down;
        mouseWorld.z = 0f;
        var direction = ((Vector2)(mouseWorld - playerPos)).normalized;
        if (direction.sqrMagnitude < 0.0001f)
            direction = Vector2.down;

        var spawnPos = playerPos + (Vector3)(direction * throwSpawnDistance);
        worldItemSpawner.Spawn(droppingItem, spawnPos, direction * throwImpulse);

        // reduce or clear the source stack in inventory
        var remaining = item.Amount - toDrop;
        if (remaining <= 0)
            player.Inventory.ClearItemAt(sourceIndex);
        else
            player.Inventory.SetItemAt(sourceIndex, item.WithAmount(remaining));
    }

    /// <summary>
    /// If backpack UI is open, use the hovered Slot (hotbar or backpack).
    /// Otherwise use currently selected hotbar index.
    /// </summary>
    private int ResolveSourceSlotIndex()
    {
        // inventory open, then return on hovered slot index
        if (backpackPresenter != null && backpackPresenter.IsInventoryOpen)
        {
            var hovered = RaycastSlotUnderMouse();
            return hovered != null ? hovered.SlotIndex : -1;
        }

        // inventory closed, then return selected hotbar slot index
        return player.Inventory.SelectedHotbarIndex;
    }

    /// <summary>
    /// Raycasts UI under the mouse using the assigned GraphicRaycaster and returns the first Slot.
    /// </summary>
    private Slot RaycastSlotUnderMouse()
    {
        if (uiRaycaster == null || EventSystem.current == null)
            return null;

        var ped = new PointerEventData(EventSystem.current) 
        { 
            position = Input.mousePosition
        };

        _raycastResults.Clear();
        uiRaycaster.Raycast(ped, _raycastResults);

        for (int i = 0; i < _raycastResults.Count; i++)
        {
            var go = _raycastResults[i].gameObject;
            if (go == null) 
                continue;

            var slot = go.GetComponentInParent<Slot>();
            if (slot != null)
                return slot;
        }
        return null;
    }
}