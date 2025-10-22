using UnityEngine;

public class BackpackPanel : InventoryPanelBase<Slot>
{
    [SerializeField] private CharacterPanel _characterPanel;

    protected override int SlotCount => player.Inventory.BackpackSize;
    public bool IsOpen => slotParent != null && slotParent.gameObject.activeSelf;

    protected override void Start()
    {
        base.Start();

        // temporarly set the inventory panel active to instantiate slots
        bool wasActive = slotParent.gameObject.activeSelf;
        slotParent.gameObject.SetActive(true);

        var offset = player.Inventory.HotbarSize;
        slots = new Slot[SlotCount];
        for (int i = 0; i < SlotCount; i++)
        {
            slots[i] = Instantiate(slotPrefab, slotParent);
            slots[i].Bind(player.Inventory, i + offset, player.Inventory.GetItemAt(i + offset));

            // subscribe to events
            slots[i].OnPointerClicked += HandleSlotClicked;
            slots[i].OnPointerEntered += HandleSlotEnter;
        }

        slotParent.gameObject.SetActive(wasActive);
    }

    public void OpenInventory()
    {
        if (slotParent != null && !IsOpen)
        {
            slotParent.gameObject.SetActive(true);
            _characterPanel?.Open();
        }
    }

    public void Close()
    {
        if (slotParent != null && IsOpen)
        {
            slotParent.gameObject.SetActive(false);
            _characterPanel?.Close();
        }
    }

    public override void RefreshSlot(int backpackIndex)
    {
        var slotIndex = backpackIndex - player.Inventory.HotbarSize;
        if (slotIndex >= 0 && slotIndex < slots.Length)
        {
            slots[slotIndex].Refresh(player.Inventory.GetItemAt(backpackIndex));
        }
    }
}
