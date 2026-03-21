public class BackpackPanel : InventoryPanelBase<Slot>, IMajorPanel
{
    protected override int SlotCount => player.Inventory.BackpackSize;
    protected override int GetInventorySlotIndex(int panelSlotIndex) => panelSlotIndex + player.Inventory.HotbarSize;

    public bool IsOpen => slotParent.gameObject.activeSelf;
    public PanelId Id => PanelId.Inventory;
    public bool PausesGame => false;
    public bool BlocksGameplayInput => true;

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

    public void Open()
    {
        slotParent.gameObject.SetActive(true);
    }

    public void Close()
    {
        slotParent.gameObject.SetActive(false);
    }

    public override void RefreshSlot(int backpackIndex)
    {
        var slotIndex = backpackIndex - player.Inventory.HotbarSize;
        if (slotIndex >= 0 && slotIndex < slots.Length)
        {
            slots[slotIndex].Refresh(player.Inventory.GetItemAt(backpackIndex));
            RefreshCooldownOverlayForPanelSlot(slotIndex);
        }
    }
}
