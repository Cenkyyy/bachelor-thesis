using UnityEngine;
using UnityEngine.EventSystems;

public abstract class InventoryPanelBase<T> : MonoBehaviour where T : Slot
{
    [Header("View")]
    [SerializeField] protected T slotPrefab;
    [SerializeField] protected Transform slotParent;

    [Header("Model")]
    [SerializeField] protected Player player;
    [SerializeField] private PlayerConsumableController _consumableController;

    protected abstract int SlotCount { get; }
    protected abstract int GetInventorySlotIndex(int panelSlotIndex);

    protected T[] slots;

    protected virtual void Start()
    {
        if (player.Inventory != null)
        {
            player.Inventory.OnItemChanged += HandleItemChanged;
        }
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
        {
            player.Inventory.OnItemChanged -= HandleItemChanged;
        }

        if (slots != null)
        {
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] == null)
                    continue;
                slots[i].OnPointerClicked -= HandleSlotClicked;
                slots[i].OnPointerEntered -= HandleSlotEnter;
            }
        }
    }

    public abstract void RefreshSlot(int index);

    protected virtual void HandleItemChanged(int index) => RefreshSlot(index);

    protected virtual void HandleSlotClicked(Slot slot, PointerEventData eventData) => ItemInteractionController.Instance?.OnSlotPointerClicked(slot, eventData);

    protected virtual void HandleSlotEnter(Slot slot, PointerEventData eventData) => ItemInteractionController.Instance?.OnSlotPointerEnter(slot, eventData);

    protected void RefreshCooldownOverlayForPanelSlot(int panelSlotIndex)
    {
        if (slots == null || panelSlotIndex < 0 || panelSlotIndex >= slots.Length)
            return;

        var slot = slots[panelSlotIndex];
        if (slot == null || player == null || _consumableController == null)
        {
            slot?.SetConsumableCooldownOverlayFill(0f);
            return;
        }

        var inventorySlotIndex = GetInventorySlotIndex(panelSlotIndex);
        if (inventorySlotIndex < 0 || inventorySlotIndex >= player.Inventory.Capacity)
        {
            slot.SetConsumableCooldownOverlayFill(0f);
            return;
        }

        var item = player.Inventory.GetItemAt(inventorySlotIndex);
        if (item.IsEmpty || !_consumableController.TryGetConsumableCooldown01(item.Item, out var cooldown01))
        {
            slot.SetConsumableCooldownOverlayFill(0f);
            return;
        }

        slot.SetConsumableCooldownOverlayFill(cooldown01);
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
