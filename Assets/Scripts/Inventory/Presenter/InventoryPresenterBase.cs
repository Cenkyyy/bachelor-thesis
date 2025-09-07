using UnityEngine;
using UnityEngine.EventSystems;

public abstract class InventoryPresenterBase<T> : MonoBehaviour where T : Slot
{
    [Header("View")]
    [SerializeField] protected T slotPrefab;
    [SerializeField] protected Transform slotParent;

    [Header("Model")]
    [SerializeField] protected Player player;

    protected T[] slots;
    protected abstract int SlotCount { get; }

    public abstract void RefreshSlot(int index);

    protected virtual void Start()
    {
        if (player.Inventory != null)
        {
            player.Inventory.OnItemChanged += HandleItemChanged;
        }
    }

    protected virtual void OnDestroy()
    {
        if (player.Inventory != null)
        {
            player.Inventory.OnItemChanged -= HandleItemChanged;
        }
    }

    protected virtual void HandleItemChanged(int index) => RefreshSlot(index);

    protected virtual void HandleSlotClicked(Slot slot, PointerEventData eventData) => ItemInteractionPresenter.Instance?.OnSlotPointerClicked(slot, eventData);

    protected virtual void HandleSlotEnter(Slot slot, PointerEventData eventData) => ItemInteractionPresenter.Instance?.OnSlotPointerEnter(slot, eventData);
}