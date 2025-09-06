using UnityEngine;
using UnityEngine.EventSystems;

public abstract class InventoryPresenterBase<T> : MonoBehaviour where T : Slot
{
    [Header("View")]
    [SerializeField] protected T slotPrefab;
    [SerializeField] protected Transform slotParent;

    [Header("Model")]
    [SerializeField] protected PlayerInventoryWrapper playerInventory;

    protected T[] slots;
    protected abstract int SlotCount { get; }

    public abstract void RefreshSlot(int index);

    protected virtual void Start()
    {
        if (playerInventory.Inventory != null)
        {
            playerInventory.Inventory.OnItemChanged += HandleItemChanged;
        }
    }

    protected virtual void OnDestroy()
    {
        if (playerInventory.Inventory != null)
        {
            playerInventory.Inventory.OnItemChanged -= HandleItemChanged;
        }
    }

    protected virtual void HandleItemChanged(int index)
    {
        RefreshSlot(index);
    }

    protected virtual void HandleSlotClicked(Slot slot, PointerEventData eventData) => ItemInteractionPresenter.Instance?.OnSlotPointerClicked(slot, eventData);

    protected virtual void HandleSlotEnter(Slot slot) => ItemInteractionPresenter.Instance?.OnSlotPointerEnter(slot);
}