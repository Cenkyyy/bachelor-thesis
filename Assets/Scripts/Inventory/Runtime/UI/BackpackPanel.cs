using System.Collections;

/// <summary>
/// Inventory panel that displays and controls the player's backpack slots.
/// </summary>
public class BackpackPanel : InventoryPanelBase<InventorySlotView>, IMajorPanel
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
        StartCoroutine(BuildSlotsCoroutine());
    }

    private IEnumerator BuildSlotsCoroutine()
    {
        yield return null;

        // temporarly set the inventory panel active to instantiate slots
        bool wasActive = slotParent.gameObject.activeSelf;
        slotParent.gameObject.SetActive(true);

        var offset = player.Inventory.HotbarSize;
        slots = new InventorySlotView[SlotCount];
        for (int i = 0; i < SlotCount; i++)
        {
            slots[i] = Instantiate(slotPrefab, slotParent);
            slots[i].Bind(player.Inventory, i + offset, player.Inventory.GetItemAt(i + offset));

            // subscribe to events
            slots[i].OnPointerClicked += HandleSlotClicked;
            slots[i].OnPointerEntered += HandleSlotEnter;
            slots[i].OnPointerExited += HandleSlotExit;
            slots[i].OnSlotDisabled += HandleSlotDisabled;

            if ((i + 1) % slotBuildBatchSize == 0)
                yield return null;
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
        if (slots == null)
            return;

        var slotIndex = backpackIndex - player.Inventory.HotbarSize;
        if (slotIndex >= 0 && slotIndex < slots.Length)
        {
            slots[slotIndex].Refresh(player.Inventory.GetItemAt(backpackIndex));
            RefreshCooldownOverlayForPanelSlot(slotIndex);
        }
    }
}
