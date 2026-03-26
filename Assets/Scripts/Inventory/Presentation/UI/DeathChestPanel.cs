using UnityEngine;
using UnityEngine.EventSystems;

public sealed class DeathChestPanel : MonoBehaviour, IMajorPanel
{
    [Header("View")]
    [SerializeField] private Slot _slotPrefab;
    [SerializeField] private Transform _slotParent;

    public IInventory Inventory { get; private set; }
    public bool IsOpen => _slotParent.gameObject.activeSelf;
    public PanelId Id => PanelId.DeathChest;
    public bool PausesGame => false;
    public bool BlocksGameplayInput => true;

    private Slot[] _slots;

    public void Bind(IInventory inventory)
    {
        if (Inventory == inventory && _slots != null)
        {
            RefreshAllSlots();
            return;
        }

        if (Inventory != null)
            Inventory.OnItemChanged -= RefreshSlot;

        Inventory = inventory;

        ClearSlots();
        BuildSlots();
        RefreshAllSlots();

        if (Inventory != null)
            Inventory.OnItemChanged += RefreshSlot;
    }

    public void Open()
    {
        _slotParent.gameObject.SetActive(true);
        RefreshAllSlots();
    }

    public void Close() 
    { 
        _slotParent.gameObject.SetActive(false);
    }

    private void BuildSlots()
    {
        if (Inventory == null || _slotPrefab == null || _slotParent == null)
            return;

        _slots = new Slot[Inventory.Capacity];
        for (int i = 0; i < _slots.Length; i++)
        {
            _slots[i] = Instantiate(_slotPrefab, _slotParent);
            _slots[i].Bind(Inventory, i, Inventory.GetItemAt(i));
            _slots[i].OnPointerClicked += HandleSlotClicked;
            _slots[i].OnPointerEntered += HandleSlotEnter;
            _slots[i].OnPointerExited += HandleSlotExit;
            _slots[i].OnSlotDisabled += HandleSlotDisabled;
        }
    }

    private void ClearSlots()
    {
        if (_slots == null)
            return;

        foreach (var slot in _slots)
        {
            if (slot == null)
                continue;

            slot.OnPointerClicked -= HandleSlotClicked;
            slot.OnPointerEntered -= HandleSlotEnter;
            slot.OnPointerExited -= HandleSlotExit;
            slot.OnSlotDisabled -= HandleSlotDisabled;
            Destroy(slot.gameObject);
        }
        _slots = null;
    }

    private void OnDestroy()
    {
        if (Inventory != null)
            Inventory.OnItemChanged -= RefreshSlot;
        
        ClearSlots();
    }

    private void HandleSlotClicked(Slot slot, PointerEventData evt) =>
        ItemInteractionController.Instance?.OnSlotPointerClicked(slot, evt);

    private void HandleSlotEnter(Slot slot, PointerEventData evt)
    {
        ItemInteractionController.Instance?.OnSlotPointerEnter(slot, evt);
        ItemTooltipController.Instance?.OnSlotPointerEnter(slot, evt);
    }

    private void HandleSlotExit(Slot slot, PointerEventData evt)
    {
        ItemTooltipController.Instance?.OnSlotPointerExit(slot, evt);
    }

    private void HandleSlotDisabled(Slot slot)
    {
        ItemTooltipController.Instance?.OnSlotDisabled(slot);
    }

    public void RefreshSlot(int index)
    {
        if (_slots == null || index < 0 || index >= _slots.Length) 
            return;
        
        _slots[index].Refresh(Inventory.GetItemAt(index));
    }

    private void RefreshAllSlots()
    {
        if (_slots == null || Inventory == null)
            return;

        for (int i = 0; i < _slots.Length; i++)
            _slots[i].Refresh(Inventory.GetItemAt(i));
    }
}
