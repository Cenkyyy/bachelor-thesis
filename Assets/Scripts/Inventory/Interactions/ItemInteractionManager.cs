using TMPro;
using UnityEngine;
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

    private Item _heldItem;

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
        if (_heldItem == null)
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

    #region Quick-transfer handler and its helpers
    public void HandleQuickTransferLeftClick(Slot slot)
    {
        // validation
        if (!IsValidSlot(slot))
            return;
        if (!backpackController.gameObject.activeSelf)
            return;

        var clickedSlotItem = playerInventory.Inventory.GetItemAt(slot.SlotIndex);

        if (!IsValidItem(clickedSlotItem))
            return;

        // get the target range - rangeStart included, rangeEnd excluded
        (int rangeStart, int rangeEndExclusive) = GetTransferRanges(slot);

        // try finding a same type stackable item in the target range
        if (clickedSlotItem.item.IsStackable)
        {
            bool emptied = TryStackSlotToRangeAndClearOriginalIfEmpty(slot, clickedSlotItem, rangeStart, rangeEndExclusive);
            if (emptied) 
                return;
        }

        // try finding an empty slot in the target range
        for (int i = rangeStart; i < rangeEndExclusive; i++)
        {
            if (playerInventory.Inventory.GetItemAt(i) == null)
            {
                TransferSlotToFirstEmptySlot(slot.SlotIndex, i, clickedSlotItem);
                return;
            }
        }
    }

    private (int, int) GetTransferRanges(Slot slot)
    {
        bool fromHotbar = slot.SlotIndex < playerInventory.Inventory.HotbarSize;

        int rangeStartInclusive, rangeEndExclusive;
        if (fromHotbar)
        {
            rangeStartInclusive = playerInventory.Inventory.HotbarSize;
            rangeEndExclusive = playerInventory.Inventory.TotalSize;
        }
        else
        {
            rangeStartInclusive = 0;
            rangeEndExclusive = playerInventory.Inventory.HotbarSize;
        }

        return (rangeStartInclusive, rangeEndExclusive);
    }

    private bool TryStackSlotToRangeAndClearOriginalIfEmpty(Slot slot, Item sourceItem, int rangeStartInclusive, int rangeEndExclusive)
    {
        for (int i = rangeStartInclusive; i < rangeEndExclusive; i++)
        {
            var destItem = playerInventory.Inventory.GetItemAt(i);
            if (!IsSameItem(sourceItem, destItem))
                continue;

            // check how many can be moved
            var freeSpace = destItem.item.MaxStackSize - destItem.amount;
            if (freeSpace <= 0)
                continue;

            // move as much as possible
            int toMove = Mathf.Min(sourceItem.amount, freeSpace);
            destItem.amount += toMove;
            sourceItem.amount -= toMove;
            RefreshSlot(i);

            // if the clicked slot item is fully moved, clear the slot and return
            if (sourceItem.amount <= 0)
            {
                playerInventory.Inventory.ClearItemAt(slot.SlotIndex);
                RefreshSlot(slot.SlotIndex);
                return true;
            }
            
        }
        return false;
    }

    private void TransferSlotToFirstEmptySlot(int sourceSlotIdx, int destSlotIdx, Item item)
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
        if (_heldItem == null)
        {
            if (IsValidItem(clickedSlotItem))
            {
                PickWholeItemUp(slot, clickedSlotItem);
            }
            return;
        }

        // holding something and clicked on a empty slot, place item
        if (!IsValidItem(clickedSlotItem))
        {
            PlaceWholeHeldIntoSlot(slot);
            return;
        }

        // holding something and items are of same type and are stackable, then merge
        if (IsSameItem(clickedSlotItem, _heldItem) && _heldItem.item.IsStackable)
        {
            TryMergeToSlotAndHideHeldIfEmpty(slot, clickedSlotItem);
            return;
        }

        // different, then swap
        SwapHeldWithSlot(slot, clickedSlotItem);
    }

    private void PickWholeItemUp(Slot slot, Item item)
    {
        _heldItem = item;
        playerInventory.Inventory.ClearItemAt(slot.SlotIndex);

        RefreshSlot(slot.SlotIndex);
        ShowHeldItem();
    }

    private void PlaceWholeHeldIntoSlot(Slot slot)
    {
        playerInventory.Inventory.SetItemAt(slot.SlotIndex, _heldItem);
        _heldItem = null;

        RefreshSlot(slot.SlotIndex);
        HideHeldItem();
    }

    private void TryMergeToSlotAndHideHeldIfEmpty(Slot slot, Item item)
    {
        // merge as much as possible from held into existing stack in slot
        int freeSpace = item.item.MaxStackSize - item.amount;
        int toMove = Mathf.Min(_heldItem.amount, freeSpace);
        item.amount += toMove;
        _heldItem.amount -= toMove;

        UpdateHeldItem();
        RefreshSlot(slot.SlotIndex);
    }

    private void SwapHeldWithSlot(Slot slot, Item item)
    {
        playerInventory.Inventory.SetItemAt(slot.SlotIndex, _heldItem);
        _heldItem = item;

        RefreshSlot(slot.SlotIndex);
        UpdateHeldItem();
    }

    #endregion

    #region Double left click handler and its helpers

    public void HandleDoubleLeftClick(Slot slot)
    {
        // validation
        if (!IsValidSlot(slot)) 
            return;

        var clickedSlotItem = playerInventory.Inventory.GetItemAt(slot.SlotIndex);

        if (!IsValidItem(clickedSlotItem) || !clickedSlotItem.item.IsStackable)
            return;
        if (_heldItem == null && clickedSlotItem.amount == clickedSlotItem.item.MaxStackSize)
            return;

        // nothing in hand, try collecting up same items to MaxStackSize across the inventory
        if (_heldItem == null)
        {
            int collected = CollectSameItemsFromInventoryIntoHeld(slot, clickedSlotItem);
            if (collected > 0)
            {
                _heldItem = new Item(clickedSlotItem.item, collected);
                ShowHeldItem();
            }
        }
    }

    private int CollectSameItemsFromInventoryIntoHeld(Slot slot, Item item)
    {
        // initialize collected to the clicked item and clear the slot
        int collected = item.amount;
        playerInventory.Inventory.ClearItemAt(slot.SlotIndex);

        // search the rest of the inventory for same items to collect
        for (int i = 0; i < playerInventory.Inventory.TotalSize; i++)
        {
            if (i == slot.SlotIndex)
                continue;

            var currentItem = playerInventory.Inventory.GetItemAt(i);
            if (!IsSameItem(currentItem, item))
                continue;

            int canTake = Mathf.Min(currentItem.amount, item.item.MaxStackSize - collected);
            if (canTake > 0)
            {
                collected += canTake;
                currentItem.amount -= canTake;
                if (currentItem.amount <= 0)
                {
                    playerInventory.Inventory.ClearItemAt(i);
                }
                RefreshSlot(i);
                if (collected == item.item.MaxStackSize)
                {
                    break;
                }
            }
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
        if (_heldItem == null)
        {
            if (IsValidItem(clickedSlotItem) && clickedSlotItem.item.IsStackable)
            {
                PickHalvedSlotItemUp(slot, clickedSlotItem);
                return;
            }
        }

        // holding an item and clicked on an empty slot, then place one into the empty slot
        if (!IsValidItem(clickedSlotItem))
        {
            TryPlaceOneFromHeldIntoEmptySlot(slot);
            return;
        }

        // holding an item and clicked on the same type stackable item, then add one to the slot's item
        if (IsSameItem(clickedSlotItem, _heldItem) && clickedSlotItem.item.IsStackable)
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
        if (_heldItem == null)
            return;
        if (!Input.GetMouseButton(1))
            return;

        var enteredSlotItem = playerInventory.Inventory.GetItemAt(slot.SlotIndex);

        // hovering over an empty slot with item while holding right click, then place one into the empty slot
        if (!IsValidItem(enteredSlotItem))
        {
            TryPlaceOneFromHeldIntoEmptySlot(slot);
            return;
        }

        // hovering over the same type stackable slot item while holding right click, then add one to the slot
        if (IsSameItem(enteredSlotItem, _heldItem) && enteredSlotItem.item.IsStackable)
        {
            TryPlaceOneFromHeldIntoStackableSlot(slot, enteredSlotItem);
            return;
        }
    }

    private void PickHalvedSlotItemUp(Slot slot, Item item)
    {
        int half = Mathf.CeilToInt((item.amount + 1) / 2);
        item.amount -= half;
        if (item.amount <= 0)
        {
            playerInventory.Inventory.ClearItemAt(slot.SlotIndex);
        }
        _heldItem = new Item(item.item, half);
        ShowHeldItem();
        RefreshSlot(slot.SlotIndex);
    }

    private void TryPlaceOneFromHeldIntoEmptySlot(Slot slot)
    {
        playerInventory.Inventory.SetItemAt(slot.SlotIndex, new Item(_heldItem.item));
        _heldItem.amount -= 1;

        RefreshSlot(slot.SlotIndex);
        UpdateHeldItem();
    }

    private void TryPlaceOneFromHeldIntoStackableSlot(Slot slot, Item item)
    {
        item.amount += 1;
        _heldItem.amount -= 1;

        RefreshSlot(slot.SlotIndex);
        UpdateHeldItem();
    }

    #endregion

    #region General helpers

    private bool IsValidSlot(Slot slot) => slot != null && slot.SlotIndex >= 0 && slot.SlotIndex < playerInventory.Inventory.TotalSize;

    private bool IsValidItem(Item item) => item != null && item.item != null;

    private bool IsSameItem(Item a, Item b) => IsValidItem(a) && IsValidItem(b) && a.item == b.item;

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
        if (_heldItem.amount <= 0)
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
        if (_heldItem == null)
        {
            HideHeldItem();
            return;
        }

        // show icon
        heldItemIcon.sprite = _heldItem.item.icon;
        heldItemIcon.gameObject.SetActive(true);

        if (_heldItem.item.IsStackable && _heldItem.amount > 1)
        {
            // show item amount text if amount is greater than 1
            heldItemAmountText.text = _heldItem.amount.ToString();
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
        _heldItem = null;
        heldItemIcon.gameObject.SetActive(false);
        heldItemAmountText.gameObject.SetActive(false);
    }

    #endregion
}
