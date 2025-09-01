using UnityEngine;
using UnityEngine.EventSystems;

public abstract class SlotControllerBase<T> : MonoBehaviour where T : Slot
{
    [SerializeField] protected T slotPrefab;
    [SerializeField] protected Transform slotParent; // should represent the panel where slots will be instantiated
    [SerializeField] protected PlayerInventoryWrapper playerInventory;

    protected abstract int SlotCount { get; }

    protected T[] slots;

    protected virtual void Start()
    {
        if (slotPrefab == null || slotParent == null || playerInventory == null)
            return;

        CreateAndBindSlots();
    }

    protected virtual void CreateAndBindSlots(int offset = 0)
    {
        slots = new T[SlotCount];
        for (int i = 0; i < SlotCount; i++)
        {

            slots[i] = Instantiate(slotPrefab, slotParent);
            slots[i].Bind(i + offset, playerInventory.Inventory.GetItemAt(i + offset));
            SubscribeSlotEvents(slots[i]);
        }
    }

    void OnSlotClickedForward(Slot s, PointerEventData ev)
    {
        if (s is T typed)
            HandleSlotClicked(typed, ev);
    }

    void OnSlotEnterForward(Slot s)
    {
        if (s is T typed) 
            HandleSlotEnter(typed);
    }

    public virtual void RefreshSlot(int inventoryIndex) { }

    protected virtual void SubscribeSlotEvents(Slot slot)
    {
        slot.OnPointerClicked += HandleSlotClicked;
        slot.OnPointerEntered += HandleSlotEnter;
    }

    protected virtual void HandleSlotClicked(Slot slot, PointerEventData eventData) => ItemInteractionManager.Instance?.OnSlotPointerClicked(slot, eventData);
    protected virtual void HandleSlotEnter(Slot slot) => ItemInteractionManager.Instance?.OnSlotPointerEnter(slot);

    protected virtual void OnDestroy()
    {
        if (slots == null) 
            return;
        foreach (var slot in slots)
        {
            if (slot == null) 
                continue;
            slot.OnPointerClicked -= OnSlotClickedForward;
            slot.OnPointerEntered -= OnSlotEnterForward;
        }
    }
}