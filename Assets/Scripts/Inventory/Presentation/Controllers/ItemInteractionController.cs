using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Translates pointer input on inventory slots into inventory operations
/// (pick up, place, swap, merge, quick-transfer, right-click behaviors)
/// and renders the "held item" (cursor) UI.
/// </summary>
public class ItemInteractionController : MonoBehaviour
{
    public static ItemInteractionController Instance { get; private set; }
    
    // UI
    [SerializeField] private Canvas _canvas;
    [SerializeField] private RectTransform _heldItemContainer;
    [SerializeField] private Image _heldItemIcon;
    [SerializeField] private TMP_Text _heldItemAmountText;

    // Panels
    [SerializeField] private BackpackPanel _backpackPanel;
    [SerializeField] private RectTransform _backpackPanelRect;

    [SerializeField] private HotbarPanel _hotbarPanel;
    [SerializeField] private RectTransform _hotbarPanelRect;

    [SerializeField] private DeathChestPanel _deathChestPanel;
    [SerializeField] private RectTransform _deathChestPanelRect;

    [SerializeField] private EquipmentPanel _equipmentPanel;
    [SerializeField] private RectTransform _equipmentPanelRect;

    [SerializeField] private CraftingPanel _craftingPanel;
    [SerializeField] private RectTransform _craftingPanelRect;

    // Item dropping
    [SerializeField] private Player _player;

    [SerializeField] private ItemDropSpawner _worldItemSpawner;
    [SerializeField] private float _dropSpawnDistance = 0.6f;

    // Double click
    [SerializeField] private float _doubleClickDelay = 0.2f;

    private Coroutine _doubleClickCoroutine; // coroutine measuring the double-click time window
    private Slot _firstClickSlot; // slot that was clicked first
    private bool _firstClickPickedUp; // boolean that remembers if first click picked up an item via regular left click
    private IInventory _firstClickInventory; // inventory of the first clicked slot

    // Currently held (cursor) item
    private InventoryItem _heldItem = InventoryItem.Empty;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        // make sure the held item UI does not block raycasts
        if (_heldItemIcon != null)
            _heldItemIcon.raycastTarget = false;
        if (_heldItemAmountText != null)
            _heldItemAmountText.raycastTarget = false;

        // hide held item UI initially
        if (_heldItemContainer != null)
            _heldItemContainer.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void Update()
    {
        if (GameStateManager.IsGamePaused)
            return;

        if (_heldItem.IsEmpty)
            return;        

        // make the item icon follow the mouse cursor
        RectTransformUtility.ScreenPointToLocalPointInRectangle
        (
            _canvas.transform as RectTransform,
            Input.mousePosition,
            _canvas.worldCamera,
            out var pos
        );
        _heldItemContainer.anchoredPosition = pos;

        // drop held stack when clicking outside inventory panels
        if (!Input.GetMouseButtonDown(0))
            return;

        if (IsPointerOverInventoryPanels(Input.mousePosition))
            return;

        // spawn the held item in the world
        AimUtils.ComputeAim2D(_player.transform, _dropSpawnDistance, out var direction, out var spawnPos);
        _worldItemSpawner?.Spawn(_heldItem, spawnPos, direction);

        // clear held cursor item
        _heldItem = InventoryItem.Empty;
        UpdateHeldItem();
    }

    private bool IsPointerOverInventoryPanels(Vector2 screenPoint)
    {
        var uiCamera = _canvas ? _canvas.worldCamera : null;

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
    public void OnSlotPointerClicked(Slot slot, PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            if (ReferenceEquals(slot.Owner, _player.Equipment))
            {
                HandleEquipmentLeftClick(slot);
                return;
            }

            ProcessLeftClickImmediateThenMaybeDouble(slot, eventData);
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
    public void OnSlotPointerEnter(Slot slot, PointerEventData eventData)
    {
        if (ReferenceEquals(slot.Owner, _player.Equipment))
        {
            return;
        }

        HandleRightClickPointerEnter(slot);
    }

    private void HandleEquipmentLeftClick(Slot slot)
    {
        var eq = _player.Equipment;
        int index = slot.SlotIndex;

        // Place from cursor into this slot
        if (!_heldItem.IsEmpty)
        {
            // Only EquipmentItem can go into equipment slots
            if (!(_heldItem.Item is EquipmentItemData))
                return;

            // Try to place exactly 1 into this specific slot
            if (eq.TryMergeInto(new InventoryItem(_heldItem.Item, 1), index, out var leftoverOnce))
            {
                _heldItem = _heldItem.WithAmount(_heldItem.Amount - 1);
                UpdateHeldItem();
            }
            return;
        }

        // Pick from slot to cursor (if anything there)
        var slotItem = eq.GetItemAt(index);
        if (!slotItem.IsEmpty)
        {
            _heldItem = slotItem;
            eq.ClearItemAt(index);
            UpdateHeldItem();
        }
    }

    private void HandleEquipmentRightClick(Slot slot)
    {
        var eq = _player.Equipment;
        int index = slot.SlotIndex;

        var slotItem = eq.GetItemAt(index);

        // Quick-unequip to inventory
        if (_heldItem.IsEmpty)
        {
            if (slotItem.IsEmpty) return; // do nothing on empty equipment slot
            TransferStack(eq, index, _player.Inventory);
            return;
        }

        // Holding something: allow quick place of ONE equipment item if compatible
        if (_heldItem.Item is EquipmentItemData)
        {
            if (eq.TryMergeInto(new InventoryItem(_heldItem.Item, 1), index, out var leftoverOnce))
            {
                _heldItem = _heldItem.WithAmount(_heldItem.Amount - 1);
                UpdateHeldItem();
            }
        }
    }

    private void TransferStack(IInventory src, int index, IInventory dst)
    {
        var item = src.GetItemAt(index);
        if (item.IsEmpty) return;

        dst.TryAddItemToRange(item, new SlotRange(0, dst.Capacity), out var leftover);
        if (leftover.IsEmpty)
            src.ClearItemAt(index);
        else
            src.SetItemAt(index, leftover);
    }

    /// <summary>
    /// Processes a left click immediately, then opens a short window to detect a second click.
    /// Shift+LeftClick triggers quick transfer and skips the double-click window.
    /// </summary>
    /// <param name="slot">Clicked slot.</param>
    /// <param name="eventData">Pointer event data for this click.</param>
    private void ProcessLeftClickImmediateThenMaybeDouble(Slot slot, PointerEventData eventData)
    {
        // first click happened, check for second click within the window
        if (_doubleClickCoroutine != null)
        {
            // if same slot clicked again, then perform double-click
            if (_firstClickSlot == slot)
            {
                StopCoroutine(_doubleClickCoroutine);
                _doubleClickCoroutine = null;

                if (_firstClickPickedUp && !_heldItem.IsEmpty && _heldItem.Item != null)
                {
                    // if the first click picked up an item, then collect more of the same type into the held item
                    HandleDoubleLeftClickAfterRegularLeftClick(_firstClickInventory, _heldItem.Item, _heldItem.Item.MaxStackSize - _heldItem.Amount);
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
            var wasHoldingBefore = !_heldItem.IsEmpty;
            HandleRegularLeftClick(slot);
            var isHoldingAfter = !_heldItem.IsEmpty;
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
    public void HandleQuickTransferLeftClick(Slot slot)
    {
        // validation
        if (!IsValidSlot(slot) || !TryGetInventoryOwner(slot, out var sourceInventory))
            return;

        if (_deathChestPanel != null && _deathChestPanel.IsOpen && _deathChestPanel.Inventory != null)
        {
            if (ReferenceEquals(sourceInventory, _deathChestPanel.Inventory))
            {
                TransferStackFromDeathChestToPlayerInventory(sourceInventory, slot.SlotIndex);
            }
            else
            {
                TransferStack(sourceInventory, slot.SlotIndex, _deathChestPanel.Inventory);
            }
            return;
        }

        if (!_backpackPanel.IsOpen || !ReferenceEquals(sourceInventory, _player.Inventory))
            return;

        var clickedSlotItem = sourceInventory.GetItemAt(slot.SlotIndex);
        if (clickedSlotItem.IsEmpty)
            return;

        // get the target range - rangeStart included, rangeEnd excluded
        (var rangeStart, var rangeEnd) = GetTransferRanges(slot);

        // try adding item into the target range
        sourceInventory.TryAddItemToRange(clickedSlotItem, new SlotRange(rangeStart, rangeEnd), out var leftoverItem);
        if (leftoverItem.IsEmpty)
            sourceInventory.ClearItemAt(slot.SlotIndex);
        else
            sourceInventory.SetItemAt(slot.SlotIndex, leftoverItem);
    }

    private void TransferStackFromDeathChestToPlayerInventory(IInventory chestInventory, int sourceIndex)
    {
        var item = chestInventory.GetItemAt(sourceIndex);
        if (item.IsEmpty)
            return;

        // fill hotbar first, then backpack.
        _player.Inventory.TryAddItemToRange(item, new SlotRange(0, _player.Inventory.HotbarSize), out var leftoverAfterHotbar);
        if (!leftoverAfterHotbar.IsEmpty)
            _player.Inventory.TryAddItemToRange(leftoverAfterHotbar, new SlotRange(_player.Inventory.HotbarSize, _player.Inventory.Capacity), out leftoverAfterHotbar);

        if (leftoverAfterHotbar.IsEmpty)
            chestInventory.ClearItemAt(sourceIndex);
        else
            chestInventory.SetItemAt(sourceIndex, leftoverAfterHotbar);
    }

    /// <summary>
    /// Computes the quick-transfer target range for a slot (from hotbar to backpack or from backpack to hotbar).
    /// </summary>
    /// <param name="slot">Source slot for the transfer.</param>
    /// <returns> A tuple (rangeStart, rangeEndExclusive) describing the half-open index range of the target region. </returns>
    private (int, int) GetTransferRanges(Slot slot)
    {
        var fromHotbar = slot.SlotIndex < _player.Inventory.HotbarSize;
        if (fromHotbar)
        {
            return (_player.Inventory.HotbarSize, _player.Inventory.Capacity);
        }
        else
        {
            return (0, _player.Inventory.HotbarSize);
        }
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
    public void HandleRegularLeftClick(Slot slot)
    {
        // validation
        if (!IsValidSlot(slot) || !TryGetInventoryOwner(slot, out var inventory))
            return;

        var clickedSlotItem = inventory.GetItemAt(slot.SlotIndex);

        // nothing in hand, then pick up the item
        if (_heldItem.IsEmpty)
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
        if (IsSameItem(clickedSlotItem, _heldItem) && _heldItem.Item.IsStackable)
        {
            TryMergeToSlotAndHideHeldIfEmpty(inventory, slot.SlotIndex);
            return;
        }

        // different, then swap
        SwapHeldWithSlot(inventory, slot.SlotIndex);
    }

    private void PickWholeItemUp(IInventory inventory, int slotIndex, InventoryItem item)
    {
        _heldItem = item;
        inventory.ClearItemAt(slotIndex);

        UpdateHeldItem();
    }

    private void PlaceWholeHeldIntoSlot(IInventory inventory, int slotIndex)
    {
        inventory.SetItemAt(slotIndex, _heldItem);

        _heldItem = InventoryItem.Empty;
        UpdateHeldItem();
    }

    private void TryMergeToSlotAndHideHeldIfEmpty(IInventory inventory, int slotIndex)
    {
        inventory.TryMergeInto(_heldItem, slotIndex, out var leftover);
        _heldItem = leftover;
        UpdateHeldItem();
    }

    private void SwapHeldWithSlot(IInventory inventory, int slotIndex)
    {
        var targetItem = inventory.GetItemAt(slotIndex);

        inventory.SetItemAt(slotIndex, _heldItem);
        _heldItem = targetItem;

        UpdateHeldItem();
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
        if (inventory == null || itemSO == null || _heldItem.IsEmpty || _heldItem.Item != itemSO || maxToCollect <= 0)
            return;

        var collected = _heldItem.Amount;
        inventory.TryRemoveFromRange(new InventoryItem(itemSO, maxToCollect), new SlotRange(0, inventory.Capacity), out var removed);
        collected += removed.Amount;

        _heldItem = _heldItem.WithAmount(collected);
        UpdateHeldItem();
    }

    /// <summary>
    /// Standard double-click on a slot that has a stackable item.
    /// Picks up that slot, then finds same type items from the whole inventory
    /// to fill up to the stack cap into the held item.
    /// </summary>
    /// <param name="slot">The slot that was double-clicked.</param>
    public void HandleDoubleLeftClick(Slot slot)
    {
        if (!IsValidSlot(slot) || !TryGetInventoryOwner(slot, out var inventory))
            return;

        var clickedSlotItem = inventory.GetItemAt(slot.SlotIndex);

        if (clickedSlotItem.IsEmpty || !clickedSlotItem.Item.IsStackable)
            return;
        if (!_heldItem.IsEmpty && _heldItem.Item != clickedSlotItem.Item)
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
            _heldItem = new InventoryItem(clickedSlotItem.Item, collected);
            UpdateHeldItem();
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
    public void HandleRegularRightClick(Slot slot)
    {
        // validation
        if (!IsValidSlot(slot) || !TryGetInventoryOwner(slot, out var inventory))
            return;

        var clickedSlotItem = inventory.GetItemAt(slot.SlotIndex);

        // nothing in hand, try picking the item up with half the amount if there is one
        if (_heldItem.IsEmpty)
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
        if (IsSameItem(clickedSlotItem, _heldItem) && clickedSlotItem.Item.IsStackable)
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
    public void HandleRightClickPointerEnter(Slot slot)
    {
        // validation
        if (!IsValidSlot(slot) || !TryGetInventoryOwner(slot, out var inventory))
            return;
        if (_heldItem.IsEmpty)
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
        if (IsSameItem(enteredSlotItem, _heldItem) && enteredSlotItem.Item.IsStackable)
        {
            TryPlaceOneFromHeldIntoStackableSlot(inventory, slot.SlotIndex);
            return;
        }
    }

    private void PickHalvedSlotItemUp(IInventory inventory, int slotIndex, InventoryItem item)
    {
        var half = Mathf.CeilToInt((item.Amount + 1) / 2);

        // update held item
        _heldItem = new InventoryItem(item.Item, half);
        UpdateHeldItem();

        // update inventory slot
        var newItem = item.WithAmount(item.Amount - half);
        if (newItem.IsEmpty)
        {
            inventory.ClearItemAt(slotIndex);
        }
        else
        {
            inventory.SetItemAt(slotIndex, newItem);
        }
    }

    private void TryPlaceOneFromHeldIntoEmptySlot(IInventory inventory, int slotIndex)
    {
        inventory.SetItemAt(slotIndex, new InventoryItem(_heldItem.Item));

        _heldItem = _heldItem.WithAmount(_heldItem.Amount - 1);
        UpdateHeldItem();
    }

    private void TryPlaceOneFromHeldIntoStackableSlot(IInventory inventory, int slotIndex)
    {
        var item = inventory.GetItemAt(slotIndex);
        if (item.IsEmpty || item.Item != _heldItem.Item)
            return;
        if (item.Amount >= item.Item.MaxStackSize)
            return;
        
        item = item.WithAmount(item.Amount + 1);
        _heldItem = _heldItem.WithAmount(_heldItem.Amount - 1);

        inventory.SetItemAt(slotIndex, item);
        UpdateHeldItem();
    }

    #endregion

    #region General helpers

    /// <summary>
    /// Returns true if the slot is non-null and its index is within the player's inventory bounds.
    /// </summary>
    /// <param name="slot">The slot to validate.</param>
    /// <returns>true if the slot is valid for the player's inventory; otherwise false.</returns>
    private bool IsValidSlot(Slot slot) => slot != null && slot.Owner != null && slot.SlotIndex >= 0 && slot.SlotIndex < slot.Owner.Capacity;

    private bool TryGetInventoryOwner(Slot slot, out IInventory inventory)
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
        if (_heldItem.IsEmpty)
            return;

        _player.Inventory.TryAddItemToRange(_heldItem, new SlotRange(0, _player.Inventory.Capacity), out var leftover);

        if (!leftover.IsEmpty)
        {
            AimUtils.ComputeAim2D(_player.transform, _dropSpawnDistance, out var direction, out var spawnPos);
            _worldItemSpawner.Spawn(leftover, spawnPos, direction);
        }

        _heldItem = InventoryItem.Empty;
        UpdateHeldItem();
    }


    /// <summary>
    /// Updates the held-item UI visibility and content based on the current held item.
    /// </summary>
    private void UpdateHeldItem()
    {
        if (_heldItem.IsEmpty)
        {
            HideHeldItem();
        }
        else
        {
            ShowHeldItem();
        }
    }

    /// <summary>
    /// Shows the floating held-item UI (icon + amount).
    /// </summary>
    private void ShowHeldItem()
    {
        if (_heldItem.IsEmpty)
        {
            HideHeldItem();
            return;
        }

        _heldItemContainer.gameObject.SetActive(true);

        // show icon
        ImageIconUtility.SetIcon(_heldItemIcon, _heldItem.Item.Icon);
        _heldItemIcon.gameObject.SetActive(true);

        if (_heldItem.Item.IsStackable && _heldItem.Amount > 1)
        {
            // show item amount text if amount is greater than 1
            _heldItemAmountText.text = _heldItem.Amount.ToString();
            _heldItemAmountText.gameObject.SetActive(true);
        }
        else
        {
            // hide item amount text if amount is 1 or less
            _heldItemAmountText.text = string.Empty;
            _heldItemAmountText.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Clears the cursor item and hides the floating held-item UI.
    /// </summary>
    private void HideHeldItem()
    {
        _heldItem = InventoryItem.Empty;

        ImageIconUtility.SetIcon(_heldItemIcon, null);
        _heldItemIcon.gameObject.SetActive(false);
        _heldItemAmountText.gameObject.SetActive(false);
        _heldItemContainer.gameObject.SetActive(false);
    }

    #endregion
}
