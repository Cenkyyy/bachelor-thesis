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
public class ItemInteractionPresenter : MonoBehaviour
{
    public static ItemInteractionPresenter Instance { get; private set; }

    [Header(UIStrings.ItemInteraction_UI__Title)]
    [SerializeField] private Canvas _canvas;
    [SerializeField] private RectTransform _heldItemContainer;
    [SerializeField] private Image _heldItemIcon;
    [SerializeField] private TMP_Text _heldItemAmountText;

    [Header(UIStrings.ItemInteraction_InventorySource__Title)]
    [SerializeField] private Player _player;
    [SerializeField] private BackpackPresenter _backpackPresenter;
    [SerializeField] private HotbarPresenter _hotbarPresenter;
    [SerializeField] private RectTransform _backpackPanelRect;
    [SerializeField] private RectTransform _hotbarPanelRect;
    [SerializeField] private WorldItemSpawner _worldItemSpawner;
    [SerializeField] private float _dropSpawnDistance = 0.6f;

    [Header(UIStrings.ItemInteraction_DoubleClick__Title)]
    [SerializeField] private float _doubleClickDelay = 0.2f;

    // Double click detection fields
    private Coroutine _doubleClickCoroutine; // coroutine measuring the double-click time window
    private Slot _firstClickSlot; // slot that was clicked first
    private bool _firstClickPickedUp; // boolean that remembers if first click picked up an item via regular left click

    // Currently held (cursor) item
    private InventoryItem _heldItem = InventoryItem.Empty;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
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

    private void Update()
    {
        if (_heldItem.IsEmpty)
            return;

        // make the item icon follow the mouse cursor
        Vector2 pos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle
        (
            _canvas.transform as RectTransform,
            Input.mousePosition,
            _canvas.worldCamera,
            out pos
        );
        _heldItemContainer.anchoredPosition = pos;

        // drop held stack when clicking outside inventory panels
        if (_backpackPresenter != null && _backpackPresenter.IsInventoryOpen && !_heldItem.IsEmpty && Input.GetMouseButtonDown(0))
        {
            if (!IsPointerOverInventoryPanels(Input.mousePosition))
            {
                // spawn the held item in the world
                AimUtils.ComputeAim2D(_player.transform, _dropSpawnDistance, out var direction, out var spawnPos);
                _worldItemSpawner?.Spawn(_heldItem, spawnPos, direction);

                // clear held cursor item
                _heldItem = InventoryItem.Empty;
                UpdateHeldItem();
            }
        }
    }

    private bool IsPointerOverInventoryPanels(Vector2 screenPoint)
    {
        bool overBackpack = _backpackPanelRect != null &&
            RectTransformUtility.RectangleContainsScreenPoint(_backpackPanelRect, screenPoint, _canvas ? _canvas.worldCamera : null);

        bool overHotbar = _hotbarPanelRect != null &&
            RectTransformUtility.RectangleContainsScreenPoint(_hotbarPanelRect, screenPoint, _canvas ? _canvas.worldCamera : null);

        return overBackpack || overHotbar;
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
            ProcessLeftClickImmediateThenMaybeDouble(slot, eventData);
        }
        else if (eventData.button == PointerEventData.InputButton.Right)
        {
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
        HandleRightClickPointerEnter(slot);
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

                if (_firstClickPickedUp && !_heldItem.IsEmpty && _heldItem.ItemSO != null)
                {
                    // if the first click picked up an item, then collect more of the same type into the held item
                    HandleDoubleLeftClickAfterRegularLeftClick(_heldItem.ItemSO, _heldItem.ItemSO.MaxStackSize - _heldItem.Amount);
                }
                else
                {
                    // otherwise, just do a regular double-click action
                    HandleDoubleLeftClick(slot);
                }

                _firstClickSlot = null;
                _firstClickPickedUp = false;
                return;
            }
            else
            {
                // different slot clicked, cancel pending double-click and process as a new single click
                _firstClickSlot = null;
                _firstClickPickedUp = false;
                StopCoroutine(_doubleClickCoroutine);
                _doubleClickCoroutine = null;
            }
        }

        // first click registered, process immediately
        bool didPickUp = false;
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
        float time = 0f;
        while (time < _doubleClickDelay)
        {
            time += Time.unscaledDeltaTime;
            yield return null;
        }

        // time's up, no second click
        _doubleClickCoroutine = null;
        _firstClickSlot = null;
        _firstClickPickedUp = false;
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
        if (!IsValidSlot(slot))
            return;
        if (!_backpackPresenter.IsInventoryOpen)
            return;

        var clickedSlotItem = _player.Inventory.GetItemAt(slot.SlotIndex);
        if (clickedSlotItem.IsEmpty)
            return;

        // get the target range - rangeStart included, rangeEnd excluded
        (int rangeStart, int rangeEnd) = GetTransferRanges(slot);

        // try adding item into the target range
        _player.Inventory.TryAddItem(clickedSlotItem, rangeStart, rangeEnd, out var leftoverItem);
        if (leftoverItem.IsEmpty)
            _player.Inventory.ClearItemAt(slot.SlotIndex);
        else
            _player.Inventory.SetItemAt(slot.SlotIndex, leftoverItem);
    }

    /// <summary>
    /// Computes the quick-transfer target range for a slot (from hotbar to backpack or from backpack to hotbar).
    /// </summary>
    /// <param name="slot">Source slot for the transfer.</param>
    /// <returns> A tuple (rangeStart, rangeEndExclusive) describing the half-open index range of the target region. </returns>
    private (int, int) GetTransferRanges(Slot slot)
    {
        bool fromHotbar = slot.SlotIndex < _player.Inventory.HotbarSize;
        if (fromHotbar)
        {
            return (_player.Inventory.HotbarSize, _player.Inventory.TotalSize);
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
        if (!IsValidSlot(slot))
            return;

        var clickedSlotItem = _player.Inventory.GetItemAt(slot.SlotIndex);

        // nothing in hand, then pick up the item
        if (_heldItem.IsEmpty)
        {
            if (!clickedSlotItem.IsEmpty)
            {
                PickWholeItemUp(slot, clickedSlotItem);
            }
            return;
        }

        // holding something and clicked on a empty slot, place item
        if (clickedSlotItem.IsEmpty)
        {
            PlaceWholeHeldIntoSlot(slot);
            return;
        }

        // holding something and items are of same type and are stackable, then merge
        if (IsSameItem(clickedSlotItem, _heldItem) && _heldItem.ItemSO.IsStackable)
        {
            TryMergeToSlotAndHideHeldIfEmpty(slot);
            return;
        }

        // different, then swap
        SwapHeldWithSlot(slot);
    }

    private void PickWholeItemUp(Slot slot, InventoryItem item)
    {
        _heldItem = item;
        _player.Inventory.ClearItemAt(slot.SlotIndex);

        UpdateHeldItem();
    }

    private void PlaceWholeHeldIntoSlot(Slot slot)
    {
        _player.Inventory.SetItemAt(slot.SlotIndex, _heldItem);

        _heldItem = InventoryItem.Empty;
        UpdateHeldItem();
    }

    private void TryMergeToSlotAndHideHeldIfEmpty(Slot slot)
    {
        _heldItem = _player.Inventory.TryMergeIntoSlot(slot.SlotIndex, _heldItem);
        UpdateHeldItem();
    }

    private void SwapHeldWithSlot(Slot slot)
    {
        var targetItem = _player.Inventory.GetItemAt(slot.SlotIndex);

        _player.Inventory.SetItemAt(slot.SlotIndex, _heldItem);
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
    private void HandleDoubleLeftClickAfterRegularLeftClick(ItemBaseSO itemSO, int maxToCollect)
    {
        if (itemSO == null || _heldItem.IsEmpty || _heldItem.ItemSO != itemSO || maxToCollect <= 0) 
            return;

        int collected = _heldItem.Amount;
        collected += _player.Inventory.TryRemoveItemFromRange(itemSO, maxToCollect, 0, _player.Inventory.TotalSize);

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
        if (!IsValidSlot(slot)) 
            return;

        var clickedSlotItem = _player.Inventory.GetItemAt(slot.SlotIndex);
        
        if (clickedSlotItem.IsEmpty || !clickedSlotItem.ItemSO.IsStackable)
            return;
        if (!_heldItem.IsEmpty && _heldItem.ItemSO != clickedSlotItem.ItemSO)
            return;

        // pick up the clicked item first
        int collected = clickedSlotItem.Amount;
        _player.Inventory.ClearItemAt(slot.SlotIndex);

        // try collecting other items of the same type across the inventory
        int remainingCapacity = clickedSlotItem.ItemSO.MaxStackSize - collected;
        if (remainingCapacity > 0)
        {
            int removed = _player.Inventory.TryRemoveItemFromRange(clickedSlotItem.ItemSO, remainingCapacity, 0, _player.Inventory.TotalSize);
            collected += removed;
        }

        if (collected > 0)
        {
            _heldItem = new InventoryItem(clickedSlotItem.ItemSO, collected);
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
        if (!IsValidSlot(slot))
            return;

        var clickedSlotItem = _player.Inventory.GetItemAt(slot.SlotIndex);

        // nothing in hand, try picking the item up with half the amount if there is one
        if (_heldItem.IsEmpty)
        {
            if (!clickedSlotItem.IsEmpty && clickedSlotItem.ItemSO.IsStackable)
            {
                PickHalvedSlotItemUp(slot, clickedSlotItem);
                return;
            }
        }

        // holding an item and clicked on an empty slot, then place one into the empty slot
        if (clickedSlotItem.IsEmpty)
        {
            TryPlaceOneFromHeldIntoEmptySlot(slot);
            return;
        }

        // holding an item and clicked on the same type stackable item, then add one to the slot's item
        if (IsSameItem(clickedSlotItem, _heldItem) && clickedSlotItem.ItemSO.IsStackable)
        {
            TryPlaceOneFromHeldIntoStackableSlot(slot);
            return;
        }

        // different item or non-stackable item, then fully swap the items
        SwapHeldWithSlot(slot);
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
        if (!IsValidSlot(slot)) 
            return;
        if (_heldItem.IsEmpty)
            return;
        if (!Input.GetMouseButton(1))
            return;

        var enteredSlotItem = _player.Inventory.GetItemAt(slot.SlotIndex);

        // hovering over an empty slot with item while holding right click, then place one into the empty slot
        if (enteredSlotItem.IsEmpty)
        {
            TryPlaceOneFromHeldIntoEmptySlot(slot);
            return;
        }

        // hovering over the same type stackable slot item while holding right click, then add one to the slot
        if (IsSameItem(enteredSlotItem, _heldItem) && enteredSlotItem.ItemSO.IsStackable)
        {
            TryPlaceOneFromHeldIntoStackableSlot(slot);
            return;
        }
    }

    private void PickHalvedSlotItemUp(Slot slot, InventoryItem item)
    {
        int half = Mathf.CeilToInt((item.Amount + 1) / 2);

        // update held item
        _heldItem = new InventoryItem(item.ItemSO, half);
        UpdateHeldItem();

        // update inventory slot
        var newItem = item.WithAmount(item.Amount - half);
        if (newItem.IsEmpty)
        {
            _player.Inventory.ClearItemAt(slot.SlotIndex);
        }
        else
        {
            _player.Inventory.SetItemAt(slot.SlotIndex, newItem);
        }
    }

    private void TryPlaceOneFromHeldIntoEmptySlot(Slot slot)
    {
        _player.Inventory.SetItemAt(slot.SlotIndex, new InventoryItem(_heldItem.ItemSO));

        _heldItem = _heldItem.WithAmount(_heldItem.Amount - 1);
        UpdateHeldItem();
    }

    private void TryPlaceOneFromHeldIntoStackableSlot(Slot slot)
    {
        var item = _player.Inventory.GetItemAt(slot.SlotIndex);
        if (item.IsEmpty || item.ItemSO != _heldItem.ItemSO)
            return;
        if (item.Amount >= item.ItemSO.MaxStackSize)
            return;
        
        item = item.WithAmount(item.Amount + 1);
        _heldItem = _heldItem.WithAmount(_heldItem.Amount - 1);

        _player.Inventory.SetItemAt(slot.SlotIndex, item);
        UpdateHeldItem();
    }

    #endregion

    #region General helpers

    /// <summary>
    /// Returns true if the slot is non-null and its index is within the player's inventory bounds.
    /// </summary>
    /// <param name="slot">The slot to validate.</param>
    /// <returns>true if the slot is valid for the player's inventory; otherwise false.</returns>
    private bool IsValidSlot(Slot slot) => slot != null && slot.SlotIndex >= 0 && slot.SlotIndex < _player.Inventory.TotalSize;

    /// <summary>
    /// Returns true if both items are non-empty and share the same <see cref="ItemBaseSO"/> reference.
    /// </summary>
    /// <param name="a">First item to compare.</param>
    /// <param name="b">Second item to compare.</param>
    /// <returns>true if both are non-empty and of the same type; otherwise false.</returns>
    private bool IsSameItem(InventoryItem a, InventoryItem b) => !a.IsEmpty && !b.IsEmpty && a.ItemSO == b.ItemSO;

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
        _heldItemIcon.sprite = _heldItem.ItemSO.Icon;
        _heldItemIcon.gameObject.SetActive(true);

        if (_heldItem.ItemSO.IsStackable && _heldItem.Amount > 1)
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

        _heldItemIcon.gameObject.SetActive(false);
        _heldItemAmountText.gameObject.SetActive(false);
        _heldItemContainer.gameObject.SetActive(false);
    }

    #endregion
}
