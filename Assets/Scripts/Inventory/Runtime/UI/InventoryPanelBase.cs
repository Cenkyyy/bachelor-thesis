using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Base class for inventory panels that display player inventory slots and forward slot UI events.
/// </summary>
public abstract class InventoryPanelBase<T> : MonoBehaviour where T : InventorySlotView
{
    [Header("View")]
    [SerializeField] protected T slotPrefab;
    [SerializeField] protected Transform slotParent;

    [Header("Model")]
    [SerializeField] protected Player player;
    [SerializeField] private ItemCooldownTrackController _itemCooldownTrackController;

    [Header("Input")]
    [SerializeField] private InventoryItemInteractionController _itemInteractionController;

    [Header("Tooltip")]
    [SerializeField] private ItemTooltipController _tooltipController;

    [Header("Initialization")]
    [SerializeField, Min(1)] protected int slotBuildBatchSize = 4;

    protected abstract int SlotCount { get; }
    protected abstract int GetInventorySlotIndex(int panelSlotIndex);

    protected T[] slots;

    protected virtual void Start()
    {
        if (player.Inventory != null)
            player.Inventory.OnItemChanged += HandleItemChanged;
    }

    protected virtual void Update()
    {
        if (GameStateManager.IsGamePaused)
            return;

        RefreshCooldownOverlays();
    }

    protected virtual void OnDestroy()
    {
        if (player.Inventory != null)
            player.Inventory.OnItemChanged -= HandleItemChanged;

        if (slots != null)
        {
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] == null)
                    continue;

                slots[i].OnPointerClicked -= HandleSlotClicked;
                slots[i].OnPointerEntered -= HandleSlotEnter;
                slots[i].OnPointerExited -= HandleSlotExit;
                slots[i].OnSlotDisabled -= HandleSlotDisabled;
            }
        }
    }

    public abstract void RefreshSlot(int index);

    protected virtual void HandleItemChanged(int index) => RefreshSlot(index);

    protected virtual void HandleSlotClicked(InventorySlotView slot, PointerEventData eventData)
    {
        _itemInteractionController?.OnSlotPointerClicked(slot, eventData);
    }

    protected virtual void HandleSlotEnter(InventorySlotView slot, PointerEventData _)
    {
        _itemInteractionController?.OnSlotPointerEnter(slot);
        _tooltipController?.OnSlotPointerEnter(slot);
    }

    protected virtual void HandleSlotExit(InventorySlotView slot, PointerEventData _)
    {
        _tooltipController?.OnSlotPointerExit(slot);
    }

    protected virtual void HandleSlotDisabled(InventorySlotView slot)
    {
        _tooltipController?.OnSlotDisabled(slot);
    }

    protected void RefreshCooldownOverlayForPanelSlot(int panelSlotIndex)
    {
        if (slots == null || panelSlotIndex < 0 || panelSlotIndex >= slots.Length)
            return;

        var slot = slots[panelSlotIndex];
        if (slot == null || player == null || _itemCooldownTrackController == null)
        {
            slot?.SetItemCooldownOverlayFill(0f);
            return;
        }

        var inventorySlotIndex = GetInventorySlotIndex(panelSlotIndex);
        if (inventorySlotIndex < 0 || inventorySlotIndex >= player.Inventory.Capacity)
        {
            slot.SetItemCooldownOverlayFill(0f);
            return;
        }

        var item = player.Inventory.GetItemAt(inventorySlotIndex);
        if (item.IsEmpty || !_itemCooldownTrackController.TryGetItemCooldown01(item.Item, out var cooldown01))
        {
            slot.SetItemCooldownOverlayFill(0f);
            return;
        }

        slot.SetItemCooldownOverlayFill(cooldown01);
    }

    private void RefreshCooldownOverlays()
    {
        if (slots == null)
            return;

        for (int i = 0; i < slots.Length; i++)
        {
            RefreshCooldownOverlayForPanelSlot(i);
        }
    }
}
