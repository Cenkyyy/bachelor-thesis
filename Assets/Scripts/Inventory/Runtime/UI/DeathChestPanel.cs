using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Displays a bound death chest inventory and forwards slot interactions to shared inventory UI systems.
/// </summary>
public sealed class DeathChestPanel : MonoBehaviour, IMajorPanel
{
    [Header("View")]
    [SerializeField] private InventorySlotView _slotPrefab;
    [SerializeField] private Transform _slotParent;

    [Header("Input")]
    [SerializeField] private InventoryItemInteractionController _itemInteractionController;

    [Header("Tooltip")]
    [SerializeField] private ItemTooltipController _tooltipController;

    public IInventory Inventory { get; private set; }
    public bool IsOpen => _slotParent.gameObject.activeSelf;
    public PanelId Id => PanelId.DeathChest;
    public bool PausesGame => false;
    public bool BlocksGameplayInput => true;

    private InventorySlotView[] _slots;

    private void OnDestroy()
    {
        if (Inventory != null)
            Inventory.OnItemChanged -= RefreshSlot;

        ClearSlots();
    }

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

        _slots = new InventorySlotView[Inventory.Capacity];
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

    private void HandleSlotClicked(InventorySlotView slot, PointerEventData evt)
    {
        _itemInteractionController?.OnSlotPointerClicked(slot, evt);
    }

    private void HandleSlotEnter(InventorySlotView slot, PointerEventData _)
    {
        _itemInteractionController?.OnSlotPointerEnter(slot);
        _tooltipController?.OnSlotPointerEnter(slot);
    }

    private void HandleSlotExit(InventorySlotView slot, PointerEventData _)
    {
        _tooltipController?.OnSlotPointerExit(slot);
    }

    private void HandleSlotDisabled(InventorySlotView slot)
    {
        _tooltipController?.OnSlotDisabled(slot);
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
