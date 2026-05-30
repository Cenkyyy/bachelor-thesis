using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Translates pointer input on inventory slots into inventory operations
/// (pick up, place, swap, merge, quick-transfer, right-click behaviors)
/// </summary>
[RequireComponent(typeof(HeldInventoryItemController))]
public class InventoryItemInteractionController : MonoBehaviour
{
    [Header("Held Item")]
    [SerializeField] private HeldInventoryItemController _heldItemController;

    [Header("Inventory Panels")]
    [SerializeField] private BackpackPanel _backpackPanel;
    [SerializeField] private RectTransform _backpackPanelRect;

    [SerializeField] private RectTransform _hotbarPanelRect;

    [SerializeField] private DeathChestPanel _deathChestPanel;
    [SerializeField] private RectTransform _deathChestPanelRect;

    [SerializeField] private EquipmentPanel _equipmentPanel;
    [SerializeField] private RectTransform _equipmentPanelRect;

    [SerializeField] private CraftingPanel _craftingPanel;
    [SerializeField] private RectTransform _craftingPanelRect;

    [Header("Player")]
    [SerializeField] private Player _player;

    [Header("Double Click")]
    [SerializeField] private float _doubleClickDelay = 0.2f;

    private Coroutine _doubleClickCoroutine; // coroutine measuring the double-click time window
    private InventorySlotView _firstClickSlot; // slot that was clicked first
    private bool _firstClickPickedUp; // boolean that remembers if first click picked up an item via regular left click
    private IInventory _firstClickInventory; // inventory of the first clicked slot

    private InventoryItem HeldItem
    {
        get => _heldItemController.HeldItem;
        set => _heldItemController?.SetHeldItem(value);
    }

    private void Awake()
    {
        if (_heldItemController == null)
            _heldItemController = GetComponent<HeldInventoryItemController>();
    }

    private void Update()
    {
        if (GameStateManager.IsGamePaused)
            return;

        if (_heldItemController == null || !_heldItemController.HasHeldItem)
            return;        

        _heldItemController.UpdateCursorPosition();

        // drop held stack when clicking outside inventory panels
        if (!Input.GetMouseButtonDown(0))
            return;

        if (IsPointerOverInventoryPanels(Input.mousePosition))
            return;

        _heldItemController.DropHeldItemInWorld();
    }

    private bool IsPointerOverInventoryPanels(Vector2 screenPoint)
    {
        var uiCamera = _heldItemController != null ? _heldItemController.UICamera : null;

        // hotbar is always visible/interactive in gameplay
        var overHotbar = _hotbarPanelRect != null &&
            RectTransformUtility.RectangleContainsScreenPoint(_hotbarPanelRect, screenPoint, uiCamera);

        var overBackpack = _backpackPanel != null && _backpackPanel.IsOpen && _backpackPanelRect != null &&
            RectTransformUtility.RectangleContainsScreenPoint(_backpackPanelRect, screenPoint, uiCamera);

        var overDeathChest = _deathChestPanel != null && _deathChestPanel.IsOpen && _deathChestPanelRect != null &&
            RectTransformUtility.RectangleContainsScreenPoint(_deathChestPanelRect, screenPoint, uiCamera);

        var overCharacter = _equipmentPanel != null && _equipmentPanel.IsOpen && _equipmentPanelRect != null &&
            RectTransformUtility.RectangleContainsScreenPoint(_equipmentPanelRect, screenPoint, uiCamera);

        var overCrafting = _craftingPanel != null && _craftingPanel.IsOpen && _craftingPanelRect != null &&
            RectTransformUtility.RectangleContainsScreenPoint(_craftingPanelRect, screenPoint, uiCamera);

        return overHotbar || overBackpack || overDeathChest || overCharacter || overCrafting;
    }

    /// <summary>
    /// Handles left/right mouse button clicks.
    /// </summary>
    /// <param name="slot">The slot that was clicked.</param>
    /// <param name="eventData">Pointer event data.</param>
    public void OnSlotPointerClicked(InventorySlotView slot, PointerEventData eventData)
    {
        if (GameStateManager.IsGamePaused)
            return;

        if (eventData.button == PointerEventData.InputButton.Left)
        {
            if (ReferenceEquals(slot.Owner, _player.Equipment))
            {
                if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                {
                    HandleQuickTransferLeftClick(slot);
                    return;
                }

                HandleEquipmentLeftClick(slot);
                return;
            }

            ProcessLeftClickImmediateThenMaybeDouble(slot);
        }
        else if (eventData.button == PointerEventData.InputButton.Right)
        {
            if (ReferenceEquals(slot.Owner, _player.Equipment))
            {
                HandleEquipmentRightClick(slot);
                return;
            }

            HandleRegularRightClick(slot);
        }
    }

    /// <summary>
    /// Supports right mouse dragging placement.
    /// </summary>
    /// <param name="slot">The slot the pointer entered.</param>
    /// <param name="eventData">Pointer event data.</param>
    public void OnSlotPointerEnter(InventorySlotView slot)
    {
        if (GameStateManager.IsGamePaused)
            return;

        if (ReferenceEquals(slot.Owner, _player.Equipment))
            return;

        HandleRightClickPointerEnter(slot);
    }

    private void HandleEquipmentLeftClick(InventorySlotView slot)
    {
        var equipment = _player.Equipment;
        int index = slot.SlotIndex;

        // Place from cursor into this slot
        if (!HeldItem.IsEmpty)
        {
            if (HeldItem.Item is not EquipmentItemData)
                return;

            PlaceOrSwapHeldEquipment(equipment, index);
            return;
        }

        // Pick from slot to cursor (if anything there)
        var slotItem = equipment.GetItemAt(index);
        if (!slotItem.IsEmpty)
        {
            HeldItem = slotItem;
            equipment.ClearItemAt(index);
        }
    }

    private void HandleEquipmentRightClick(InventorySlotView slot)
    {
        var equipment = _player.Equipment;
        int index = slot.SlotIndex;

        var slotItem = equipment.GetItemAt(index);

        // Quick-unequip to inventory
        if (HeldItem.IsEmpty)
        {
            if (slotItem.IsEmpty) return; // do nothing on empty equipment slot
            InventoryTransferUtility.TransferStackToPlayerInventoryPreferred(equipment, index, _player.Inventory);
            return;
        }

        // Holding something: allow quick place of ONE equipment item if compatible
        if (HeldItem.Item is EquipmentItemData)
            PlaceOrSwapHeldEquipment(equipment, index);
    }

    private void PlaceOrSwapHeldEquipment(EquipmentInventory equipment, int index)
    {
        var itemToEquip = new InventoryItem(HeldItem.Item, 1);
        if (!equipment.CanAcceptItemAt(itemToEquip, index))
            return;

        var equippedItem = equipment.GetItemAt(index);
        if (!equippedItem.IsEmpty && HeldItem.Amount > 1)
            return;

        equipment.SetItemAt(index, itemToEquip);
        HeldItem = equippedItem.IsEmpty ? HeldItem.WithAmount(HeldItem.Amount - 1) : equippedItem;
    }

    /// <summary>
    /// Processes a left click immediately, then opens a short window to detect a second click.
    /// Shift+LeftClick triggers quick transfer and skips the double-click window.
    /// </summary>
    /// <param name="slot">Clicked slot.</param>
    /// <param name="eventData">Pointer event data for this click.</param>
    private void ProcessLeftClickImmediateThenMaybeDouble(InventorySlotView slot)
    {
        // first click happened, check for second click within the window
        if (_doubleClickCoroutine != null)
        {
            // if same slot clicked again, then perform double-click
            if (_firstClickSlot == slot)
            {
                StopCoroutine(_doubleClickCoroutine);
                _doubleClickCoroutine = null;

                if (_firstClickPickedUp && !HeldItem.IsEmpty && HeldItem.Item != null)
                {
                    // if the first click picked up an item, then collect more of the same type into the held item
                    HandleDoubleLeftClickAfterRegularLeftClick(_firstClickInventory, HeldItem.Item, HeldItem.Item.MaxStackSize - HeldItem.Amount);
                }
                else
                {
                    // otherwise, just do a regular double-click action
                    HandleDoubleLeftClick(slot);
                }

                _firstClickSlot = null;
                _firstClickPickedUp = false;
                _firstClickInventory = null;
                return;
            }
            else
            {
                // different slot clicked, cancel pending double-click and process as a new single click
                _firstClickSlot = null;
                _firstClickPickedUp = false;
                _firstClickInventory = null;
                StopCoroutine(_doubleClickCoroutine);
                _doubleClickCoroutine = null;
            }
        }

        // first click registered, process immediately
        var didPickUp = false;
        if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
        {
            HandleQuickTransferLeftClick(slot);
            return;
        }
        else
        {
            var wasHoldingBefore = !HeldItem.IsEmpty;
            HandleRegularLeftClick(slot);
            var isHoldingAfter = !HeldItem.IsEmpty;
            didPickUp = !wasHoldingBefore && isHoldingAfter;
            _firstClickInventory = didPickUp ? slot.Owner as IInventory : null;
        }

        // start waiting for a possible second click
        _firstClickSlot = slot;
        _firstClickPickedUp = didPickUp;
        _doubleClickCoroutine = StartCoroutine(ClickWindowCoroutine());
    }

    /// <summary>
    /// Small time window for recognizing a double-click.
    /// </summary>
    /// <returns>An enumerator for the coroutine that measures the double-click window.</returns>
    private IEnumerator ClickWindowCoroutine()
    {
        var time = 0f;
        while (time < _doubleClickDelay)
        {
            time += Time.unscaledDeltaTime;
            yield return null;
        }

        // time's up, no second click
        _doubleClickCoroutine = null;
        _firstClickSlot = null;
        _firstClickPickedUp = false;
        _firstClickInventory = null;
    }

    #region Quick-transfer handler and its helpers

    /// <summary>
    /// Shift+LeftClick: moves the clicked stack to the opposite region (from hotbar to backpack or from backpack to hotbar).
    /// Tries to stack first, then places into the first empty slot. Clears the source if fully moved.
    /// </summary>
    /// <param name="slot">The slot clicked with Shift+LeftClick.</param>
    public void HandleQuickTransferLeftClick(InventorySlotView slot)
    {
        // validation
        if (!IsValidSlot(slot) || !TryGetInventoryOwner(slot, out var sourceInventory))
            return;

        if (ReferenceEquals(sourceInventory, _player.Equipment))
        {
            InventoryTransferUtility.TransferStackToPlayerInventoryPreferred(sourceInventory, slot.SlotIndex, _player.Inventory);
            return;
        }

        if (_deathChestPanel != null && _deathChestPanel.IsOpen && _deathChestPanel.Inventory != null)
        {
            if (ReferenceEquals(sourceInventory, _deathChestPanel.Inventory))
            {
                InventoryTransferUtility.TransferStackToPlayerInventoryPreferred(sourceInventory, slot.SlotIndex, _player.Inventory);
            }
            else
            {
                InventoryTransferUtility.TransferStack(sourceInventory, slot.SlotIndex, _deathChestPanel.Inventory);
            }
            return;
        }

        if (!_backpackPanel.IsOpen || !ReferenceEquals(sourceInventory, _player.Inventory))
            return;

        InventoryTransferUtility.QuickTransferPlayerInventorySlot(_player.Inventory, _player.Equipment, slot.SlotIndex);
    }

    #endregion

    #region Regular left click handler and its helpers

    /// <summary>
    /// Left-click behaviour:
    /// - If not holding anything: pick up the clicked stack.
    /// - If holding and target slot is empty: place held stack.
    /// - If holding same target slot is stackable type: merge into target up to max stack.
    /// - Else: swap held with slot.
    /// </summary>
    /// <param name="slot">The clicked slot.</param>
    public void HandleRegularLeftClick(InventorySlotView slot)
    {
        // validation
        if (!IsValidSlot(slot) || !TryGetInventoryOwner(slot, out var inventory))
            return;

        var clickedSlotItem = inventory.GetItemAt(slot.SlotIndex);

        // nothing in hand, then pick up the item
        if (HeldItem.IsEmpty)
        {
            if (!clickedSlotItem.IsEmpty)
            {
                PickWholeItemUp(inventory, slot.SlotIndex, clickedSlotItem);
            }
            return;
        }

        // holding something and clicked on a empty slot, place item
        if (clickedSlotItem.IsEmpty)
        {
            PlaceWholeHeldIntoSlot(inventory, slot.SlotIndex);
            return;
        }

        // holding something and items are of same type and are stackable, then merge
        if (IsSameItem(clickedSlotItem, HeldItem) && HeldItem.Item.IsStackable)
        {
            TryMergeToSlotAndHideHeldIfEmpty(inventory, slot.SlotIndex);
            return;
        }

        // different, then swap
        SwapHeldWithSlot(inventory, slot.SlotIndex);
    }

    private void PickWholeItemUp(IInventory inventory, int slotIndex, InventoryItem item)
    {
        HeldItem = item;
        inventory.ClearItemAt(slotIndex);

    }

    private void PlaceWholeHeldIntoSlot(IInventory inventory, int slotIndex)
    {
        inventory.SetItemAt(slotIndex, HeldItem);

        HeldItem = InventoryItem.Empty;
    }

    private void TryMergeToSlotAndHideHeldIfEmpty(IInventory inventory, int slotIndex)
    {
        inventory.TryMergeInto(HeldItem, slotIndex, out var leftover);
        HeldItem = leftover;
    }

    private void SwapHeldWithSlot(IInventory inventory, int slotIndex)
    {
        var targetItem = inventory.GetItemAt(slotIndex);

        inventory.SetItemAt(slotIndex, HeldItem);
        HeldItem = targetItem;

    }

    #endregion

    #region Double left click handler and its helpers

    /// <summary>
    /// After a first click that picked up a stack, handles second click, creating a double-click.
    /// Double click should top up the held stack by collecting more of the same type from the entire inventory up to the max stack.
    /// </summary>
    /// <param name="itemSO">The item type to collect.</param>
    /// <param name="maxToCollect">Maximum additional units to collect into the held stack.</param>
    private void HandleDoubleLeftClickAfterRegularLeftClick(IInventory inventory, ItemData itemSO, int maxToCollect)
    {
        if (inventory == null || itemSO == null || HeldItem.IsEmpty || HeldItem.Item != itemSO || maxToCollect <= 0)
            return;

        var collected = HeldItem.Amount;
        inventory.TryRemoveFromRange(new InventoryItem(itemSO, maxToCollect), new SlotRange(0, inventory.Capacity), out var removed);
        collected += removed.Amount;

        HeldItem = HeldItem.WithAmount(collected);
    }

    /// <summary>
    /// Standard double-click on a slot that has a stackable item.
    /// Picks up that slot, then finds same type items from the whole inventory
    /// to fill up to the stack cap into the held item.
    /// </summary>
    /// <param name="slot">The slot that was double-clicked.</param>
    public void HandleDoubleLeftClick(InventorySlotView slot)
    {
        if (!IsValidSlot(slot) || !TryGetInventoryOwner(slot, out var inventory))
            return;

        var clickedSlotItem = inventory.GetItemAt(slot.SlotIndex);

        if (clickedSlotItem.IsEmpty || !clickedSlotItem.Item.IsStackable)
            return;
        if (!HeldItem.IsEmpty && HeldItem.Item != clickedSlotItem.Item)
            return;

        // pick up the clicked item first
        var collected = clickedSlotItem.Amount;
        inventory.ClearItemAt(slot.SlotIndex);

        // try collecting other items of the same type across the inventory
        var remainingCapacity = clickedSlotItem.Item.MaxStackSize - collected;
        if (remainingCapacity > 0)
        {
            inventory.TryRemoveFromRange(new InventoryItem(clickedSlotItem.Item, remainingCapacity), new SlotRange(0, inventory.Capacity), out var removed);
            collected += removed.Amount;
        }

        if (collected > 0)
        {
            HeldItem = new InventoryItem(clickedSlotItem.Item, collected);
        }
    }

    #endregion

    #region Right click and Right click pointer handlers and its helpers

    /// <summary>
    /// Right-click behaviour:
    /// - If not holding: pick up half (ceil) of a stack if stackable.
    /// - If holding and target slot is empty: place one unit into empty.
    /// - If holding same stackable type as target slot: add one unit into target (respect stack cap).
    /// - Else: swap.
    /// </summary>
    /// <param name="slot">The slot that was right-clicked.</param>
    public void HandleRegularRightClick(InventorySlotView slot)
    {
        // validation
        if (!IsValidSlot(slot) || !TryGetInventoryOwner(slot, out var inventory))
            return;

        var clickedSlotItem = inventory.GetItemAt(slot.SlotIndex);

        // nothing in hand, try picking the item up with half the amount if there is one
        if (HeldItem.IsEmpty)
        {
            if (!clickedSlotItem.IsEmpty && clickedSlotItem.Item.IsStackable)
            {
                PickHalvedSlotItemUp(inventory, slot.SlotIndex, clickedSlotItem);
                return;
            }
        }

        // holding an item and clicked on an empty slot, then place one into the empty slot
        if (clickedSlotItem.IsEmpty)
        {
            TryPlaceOneFromHeldIntoEmptySlot(inventory, slot.SlotIndex);
            return;
        }

        // holding an item and clicked on the same type stackable item, then add one to the slot's item
        if (IsSameItem(clickedSlotItem, HeldItem) && clickedSlotItem.Item.IsStackable)
        {
            TryPlaceOneFromHeldIntoStackableSlot(inventory, slot.SlotIndex);
            return;
        }

        // different item or non-stackable item, then fully swap the items
        SwapHeldWithSlot(inventory, slot.SlotIndex);
    }

    /// <summary>
    /// Right-mouse dragging behavior:
    /// - entered empty slot: place one
    /// - entered same stackable slot: add one (if under cap)
    /// </summary>
    /// <param name="slot">The slot the pointer entered while RMB is down.</param>
    public void HandleRightClickPointerEnter(InventorySlotView slot)
    {
        // validation
        if (!IsValidSlot(slot) || !TryGetInventoryOwner(slot, out var inventory))
            return;
        if (HeldItem.IsEmpty)
            return;
        if (!Input.GetMouseButton(1))
            return;

        var enteredSlotItem = inventory.GetItemAt(slot.SlotIndex);

        // hovering over an empty slot with item while holding right click, then place one into the empty slot
        if (enteredSlotItem.IsEmpty)
        {
            TryPlaceOneFromHeldIntoEmptySlot(inventory, slot.SlotIndex);
            return;
        }

        // hovering over the same type stackable slot item while holding right click, then add one to the slot
        if (IsSameItem(enteredSlotItem, HeldItem) && enteredSlotItem.Item.IsStackable)
        {
            TryPlaceOneFromHeldIntoStackableSlot(inventory, slot.SlotIndex);
            return;
        }
    }

    private void PickHalvedSlotItemUp(IInventory inventory, int slotIndex, InventoryItem item)
    {
        var half = Mathf.CeilToInt((item.Amount + 1) / 2);

        // update held item
        HeldItem = new InventoryItem(item.Item, half);

        // update inventory slot
        var newItem = item.WithAmount(item.Amount - half);
        if (newItem.IsEmpty)
            inventory.ClearItemAt(slotIndex);
        else
            inventory.SetItemAt(slotIndex, newItem);
    }

    private void TryPlaceOneFromHeldIntoEmptySlot(IInventory inventory, int slotIndex)
    {
        inventory.SetItemAt(slotIndex, new InventoryItem(HeldItem.Item));

        HeldItem = HeldItem.WithAmount(HeldItem.Amount - 1);
    }

    private void TryPlaceOneFromHeldIntoStackableSlot(IInventory inventory, int slotIndex)
    {
        var item = inventory.GetItemAt(slotIndex);
        if (item.IsEmpty || item.Item != HeldItem.Item)
            return;
        if (item.Amount >= item.Item.MaxStackSize)
            return;
        
        item = item.WithAmount(item.Amount + 1);
        HeldItem = HeldItem.WithAmount(HeldItem.Amount - 1);

        inventory.SetItemAt(slotIndex, item);
    }

    #endregion

    #region General helpers

    /// <summary>
    /// Returns true if the slot is non-null and its index is within the player's inventory bounds.
    /// </summary>
    /// <param name="slot">The slot to validate.</param>
    /// <returns>true if the slot is valid for the player's inventory; otherwise false.</returns>
    private bool IsValidSlot(InventorySlotView slot) => slot != null && slot.Owner != null && slot.SlotIndex >= 0 && slot.SlotIndex < slot.Owner.Capacity;

    private bool TryGetInventoryOwner(InventorySlotView slot, out IInventory inventory)
    {
        inventory = slot?.Owner as IInventory;
        return inventory != null;
    }

    /// <summary>
    /// Returns true if both items are non-empty and share the same <see cref="ItemData"/> reference.
    /// </summary>
    /// <param name="a">First item to compare.</param>
    /// <param name="b">Second item to compare.</param>
    /// <returns>true if both are non-empty and of the same type; otherwise false.</returns>
    private bool IsSameItem(InventoryItem a, InventoryItem b) => !a.IsEmpty && !b.IsEmpty && a.Item == b.Item;

    public void ResolveHeldItemToInventoryOrDrop()
    {
        _heldItemController?.ResolveHeldItemToInventoryOrDrop(_player.Inventory);
    }

    #endregion
}
