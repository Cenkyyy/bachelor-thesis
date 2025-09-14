using UnityEngine;
using UnityEngine.EventSystems;

public abstract class InventoryPresenterBase<T> : MonoBehaviour where T : Slot
{
    [Header("View")]
    [SerializeField] protected T slotPrefab;
    [SerializeField] protected Transform slotParent;

    [Header("Model")]
    [SerializeField] protected Player player;

    protected abstract int SlotCount { get; }

    protected T[] slots;
    
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

    protected virtual void HandleItemChanged(int index) => RefreshSlot(index);

    protected virtual void HandleSlotClicked(Slot slot, PointerEventData eventData) => ItemInteractionPresenter.Instance?.OnSlotPointerClicked(slot, eventData);

    protected virtual void HandleSlotEnter(Slot slot, PointerEventData eventData) => ItemInteractionPresenter.Instance?.OnSlotPointerEnter(slot, eventData);
}