using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ItemInteractionManager : MonoBehaviour
{
    public static ItemInteractionManager Instance { get; private set; }

    [Header("UI")]
    [SerializeField] private Canvas canvas;
    [SerializeField] private RectTransform heldItemContainer;
    [SerializeField] private Image heldItemIcon;
    [SerializeField] private TMP_Text heldItemAmountText;

    [Header("Inventory Source")]
    [SerializeField] private PlayerInventoryWrapper playerInventory;
    [SerializeField] private BackpackController backpackController;
    [SerializeField] private HotbarController hotbarController;

    [Header("Double click")]
    [SerializeField] private float doubleClickDelay = 0.2f;

    // Double click detection fields
    private Coroutine _doubleClickCoroutine; // coroutine measuring the double-click time window
    private Slot _firstClickSlot; // slot that was clicked first
    private bool _firstClickPickedUp; // boolean that remembers if first click picked up an item via regular left click

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
        if (heldItemIcon != null)
            heldItemIcon.raycastTarget = false;
        if (heldItemAmountText != null)
            heldItemAmountText.raycastTarget = false;

        // hide held item UI initially
        if (heldItemContainer != null)
            heldItemContainer.gameObject.SetActive(true);
        if (heldItemIcon != null)
            heldItemIcon.gameObject.SetActive(false);
        if (heldItemAmountText != null)
            heldItemAmountText.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (_heldItem.IsEmpty)
            return;

        // make the item icon follow the mouse cursor
        Vector2 pos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle
        (
            canvas.transform as RectTransform,
            Input.mousePosition,
            canvas.worldCamera,
            out pos
        );
        heldItemContainer.anchoredPosition = pos;

        // TODO: drop _heldItem into the world
    }

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

    public void OnSlotPointerEnter(Slot slot)
    {
        HandleRightClickPointerEnter(slot);
    }

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

    private IEnumerator ClickWindowCoroutine()
    {
        float time = 0f;
        while (time < doubleClickDelay)
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

    public void HandleQuickTransferLeftClick(Slot slot)
    {
        // validation
        if (!IsValidSlot(slot))
            return;
        if (!backpackController.IsInventoryOpen)
            return;

        var clickedSlotItem = playerInventory.Inventory.GetItemAt(slot.SlotIndex);
        if (clickedSlotItem.IsEmpty)
            return;

        // get the target range - rangeStart included, rangeEnd excluded
        (int rangeStart, int rangeEndExclusive) = GetTransferRanges(slot);

        // try finding a same type stackable item in the target range
        if (clickedSlotItem.ItemSO.IsStackable)
        {
            bool emptied = TryStackSlotToRangeAndClearOriginalIfEmpty(slot, clickedSlotItem, rangeStart, rangeEndExclusive);
            if (emptied) 
                return;
        }

        // try finding an empty slot in the target range
        for (int i = rangeStart; i < rangeEndExclusive; i++)
        {
            if (playerInventory.Inventory.GetItemAt(i).IsEmpty)
            {
                TransferSlotToFirstEmptySlot(slot.SlotIndex, i, clickedSlotItem);
                return;
            }
        }
    }

    private (int, int) GetTransferRanges(Slot slot)
    {
        bool fromHotbar = slot.SlotIndex < playerInventory.Inventory.HotbarSize;
        if (fromHotbar)
        {
            return (playerInventory.Inventory.HotbarSize, playerInventory.Inventory.TotalSize);
        }
        else
        {
            return (0, playerInventory.Inventory.HotbarSize);
        }
    }

    private bool TryStackSlotToRangeAndClearOriginalIfEmpty(Slot slot, InventoryItem sourceItem, int rangeStartInclusive, int rangeEndExclusive)
    {
        for (int i = rangeStartInclusive; i < rangeEndExclusive; i++)
        {
            var destItem = playerInventory.Inventory.GetItemAt(i);
            if (!IsSameItem(sourceItem, destItem))
                continue;

            // check how many can be moved
            var freeSpace = destItem.ItemSO.MaxStackSize - destItem.Amount;
            if (freeSpace <= 0)
                continue;

            // move as much as possible
            int toMove = Mathf.Min(sourceItem.Amount, freeSpace);
            destItem = destItem.WithAmount(destItem.Amount + toMove);
            sourceItem = sourceItem.WithAmount(sourceItem.Amount - toMove);

            playerInventory.Inventory.SetItemAt(i, destItem);
            RefreshSlot(i);

            // if the clicked slot item is fully moved, clear the slot and return
            if (sourceItem.Amount <= 0)
            {
                playerInventory.Inventory.ClearItemAt(slot.SlotIndex);
                RefreshSlot(slot.SlotIndex);
                return true;
            }   
        }

        // write back partially moved
        var original = playerInventory.Inventory.GetItemAt(slot.SlotIndex);
        if (original.Amount != sourceItem.Amount || original.ItemSO != sourceItem.ItemSO)
        {
            if (sourceItem.IsEmpty) playerInventory.Inventory.ClearItemAt(slot.SlotIndex);
            else playerInventory.Inventory.SetItemAt(slot.SlotIndex, sourceItem);

            RefreshSlot(slot.SlotIndex);
        }

        return false;
    }

    private void TransferSlotToFirstEmptySlot(int sourceSlotIdx, int destSlotIdx, InventoryItem item)
    {
        playerInventory.Inventory.SetItemAt(destSlotIdx, item);
        playerInventory.Inventory.ClearItemAt(sourceSlotIdx);
        RefreshSlot(sourceSlotIdx);
        RefreshSlot(destSlotIdx);
    }

    #endregion

    #region Regular left click handler and its helpers

    public void HandleRegularLeftClick(Slot slot)
    {
        // validation
        if (!IsValidSlot(slot))
            return;

        var clickedSlotItem = playerInventory.Inventory.GetItemAt(slot.SlotIndex);

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
            TryMergeToSlotAndHideHeldIfEmpty(slot, clickedSlotItem);
            return;
        }

        // different, then swap
        SwapHeldWithSlot(slot, clickedSlotItem);
    }

    private void PickWholeItemUp(Slot slot, InventoryItem item)
    {
        _heldItem = item;
        playerInventory.Inventory.ClearItemAt(slot.SlotIndex);

        RefreshSlot(slot.SlotIndex);
        ShowHeldItem();
    }

    private void PlaceWholeHeldIntoSlot(Slot slot)
    {
        playerInventory.Inventory.SetItemAt(slot.SlotIndex, _heldItem);
        _heldItem = InventoryItem.Empty;

        RefreshSlot(slot.SlotIndex);
        HideHeldItem();
    }

    private void TryMergeToSlotAndHideHeldIfEmpty(Slot slot, InventoryItem item)
    {
        // merge as much as possible from held into existing stack in slot
        int freeSpace = item.ItemSO.MaxStackSize - item.Amount;
        int toMove = Mathf.Min(_heldItem.Amount, freeSpace);

        item = item.WithAmount(item.Amount + toMove);
        _heldItem = _heldItem.WithAmount(_heldItem.Amount - toMove);

        playerInventory.Inventory.SetItemAt(slot.SlotIndex, item);
        UpdateHeldItem();
        RefreshSlot(slot.SlotIndex);
    }

    private void SwapHeldWithSlot(Slot slot, InventoryItem item)
    {
        playerInventory.Inventory.SetItemAt(slot.SlotIndex, _heldItem);
        _heldItem = item;

        RefreshSlot(slot.SlotIndex);
        UpdateHeldItem();
    }

    #endregion

    #region Double left click handler and its helpers

    private void HandleDoubleLeftClickAfterRegularLeftClick(ItemBaseSO itemSO, int maxToCollect)
    {
        if (itemSO == null || _heldItem.IsEmpty || _heldItem.ItemSO != itemSO || maxToCollect <= 0) 
            return;

        int collected = CollectItemsFromInventory(itemSO, _heldItem.Amount, maxToCollect);
        _heldItem = _heldItem.WithAmount(collected);

        UpdateHeldItem();
    }

    public void HandleDoubleLeftClick(Slot slot)
    {
        if (!IsValidSlot(slot)) 
            return;

        var clickedSlotItem = playerInventory.Inventory.GetItemAt(slot.SlotIndex);
        
        if (clickedSlotItem.IsEmpty || !clickedSlotItem.ItemSO.IsStackable)
            return;
        if (!_heldItem.IsEmpty && _heldItem.ItemSO != clickedSlotItem.ItemSO)
            return;

        // pick up the clicked item first
        int collected = clickedSlotItem.Amount;
        playerInventory.Inventory.ClearItemAt(slot.SlotIndex);
        RefreshSlot(slot.SlotIndex);

        // try collecting other items of the same type across the inventory
        collected = CollectItemsFromInventory(clickedSlotItem.ItemSO, collected, clickedSlotItem.ItemSO.MaxStackSize);

        if (collected > 0)
        {
            _heldItem = new InventoryItem(clickedSlotItem.ItemSO, collected);
            ShowHeldItem();
        }
    }

    private int CollectItemsFromInventory(ItemBaseSO itemSO, int startAmount, int maxToCollect)
    {
        int collected = startAmount;
        int remaining = maxToCollect - startAmount;

        for (int i = 0; i < playerInventory.Inventory.TotalSize && remaining > 0; i++)
        {
            var currentItem = playerInventory.Inventory.GetItemAt(i);
            
            if (currentItem.IsEmpty || currentItem.ItemSO != itemSO) 
                continue;

            int canTake = Mathf.Min(currentItem.Amount, remaining);
            collected += canTake;
            currentItem = currentItem.WithAmount(currentItem.Amount - canTake);

            if (currentItem.IsEmpty)
                playerInventory.Inventory.ClearItemAt(i);
            else
                playerInventory.Inventory.SetItemAt(i, currentItem);

            RefreshSlot(i);
            remaining -= canTake;
        }

        return collected;
    }

    #endregion

    #region Right click and Right click pointer handlers and its helpers

    public void HandleRegularRightClick(Slot slot)
    {
        // validation
        if (!IsValidSlot(slot))
            return;

        var clickedSlotItem = playerInventory.Inventory.GetItemAt(slot.SlotIndex);

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
            TryPlaceOneFromHeldIntoStackableSlot(slot, clickedSlotItem);
            return;
        }

        // different item or non-stackable item, then fully swap the items
        SwapHeldWithSlot(slot, clickedSlotItem);
    }

    public void HandleRightClickPointerEnter(Slot slot)
    {
        // validation
        if (!IsValidSlot(slot)) 
            return;
        if (_heldItem.IsEmpty)
            return;
        if (!Input.GetMouseButton(1))
            return;

        var enteredSlotItem = playerInventory.Inventory.GetItemAt(slot.SlotIndex);

        // hovering over an empty slot with item while holding right click, then place one into the empty slot
        if (enteredSlotItem.IsEmpty)
        {
            TryPlaceOneFromHeldIntoEmptySlot(slot);
            return;
        }

        // hovering over the same type stackable slot item while holding right click, then add one to the slot
        if (IsSameItem(enteredSlotItem, _heldItem) && enteredSlotItem.ItemSO.IsStackable)
        {
            TryPlaceOneFromHeldIntoStackableSlot(slot, enteredSlotItem);
            return;
        }
    }

    private void PickHalvedSlotItemUp(Slot slot, InventoryItem item)
    {
        int half = Mathf.CeilToInt((item.Amount + 1) / 2);

        // update held item
        _heldItem = new InventoryItem(item.ItemSO, half);
        ShowHeldItem();

        // update inventory slot
        var newItem = item.WithAmount(item.Amount - half);
        if (newItem.IsEmpty)
        {
            playerInventory.Inventory.ClearItemAt(slot.SlotIndex);
        }
        else
        {
            playerInventory.Inventory.SetItemAt(slot.SlotIndex, newItem);
        }
        RefreshSlot(slot.SlotIndex);
    }

    private void TryPlaceOneFromHeldIntoEmptySlot(Slot slot)
    {
        playerInventory.Inventory.SetItemAt(slot.SlotIndex, new InventoryItem(_heldItem.ItemSO));
        _heldItem = _heldItem.WithAmount(_heldItem.Amount - 1);

        RefreshSlot(slot.SlotIndex);
        UpdateHeldItem();
    }

    private void TryPlaceOneFromHeldIntoStackableSlot(Slot slot, InventoryItem item)
    {
        item = item.WithAmount(item.Amount + 1);
        _heldItem = _heldItem.WithAmount(_heldItem.Amount - 1);

        playerInventory.Inventory.SetItemAt(slot.SlotIndex, item);
        RefreshSlot(slot.SlotIndex);
        UpdateHeldItem();
    }

    #endregion

    #region General helpers

    private bool IsValidSlot(Slot slot) => slot != null && slot.SlotIndex >= 0 && slot.SlotIndex < playerInventory.Inventory.TotalSize;

    private bool IsSameItem(InventoryItem a, InventoryItem b) => !a.IsEmpty && !b.IsEmpty && a.ItemSO == b.ItemSO;

    private void RefreshSlot(int index)
    {
        if (index < playerInventory.Inventory.HotbarSize)
        {
            hotbarController.RefreshSlot(index);
        }
        else
        {
            backpackController.RefreshSlot(index);
        }
    }

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

    private void ShowHeldItem()
    {
        if (_heldItem.IsEmpty)
        {
            HideHeldItem();
            return;
        }

        // show icon
        heldItemIcon.sprite = _heldItem.ItemSO.Icon;
        heldItemIcon.gameObject.SetActive(true);

        if (_heldItem.ItemSO.IsStackable && _heldItem.Amount > 1)
        {
            // show item amount text if amount is greater than 1
            heldItemAmountText.text = _heldItem.Amount.ToString();
            heldItemAmountText.gameObject.SetActive(true);
        }
        else
        {
            // hide item amount text if amount is 1 or less
            heldItemAmountText.text = string.Empty;
            heldItemAmountText.gameObject.SetActive(false);
        }
    }

    private void HideHeldItem()
    {
        _heldItem = InventoryItem.Empty;
        heldItemIcon.gameObject.SetActive(false);
        heldItemAmountText.gameObject.SetActive(false);
    }

    #endregion
}
