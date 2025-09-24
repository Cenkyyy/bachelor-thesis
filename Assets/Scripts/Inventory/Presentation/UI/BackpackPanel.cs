using UnityEngine;

public class BackpackPanel : InventoryPanelBase<Slot>
{
    [SerializeField] private KeyCode toggleKey = KeyCode.E;

    protected override int SlotCount => player.Inventory.BackpackSize;
    public bool IsInventoryOpen => slotParent != null && slotParent.gameObject.activeSelf;

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

    private void Update()
    {
        // toggle inventory with 'E' key while the game is not paused
        if (!GameStateManager.IsGamePaused && Input.GetKeyDown(toggleKey))
        {
            slotParent.gameObject.SetActive(!slotParent.gameObject.activeSelf);
        }
    }

    public void OpenInventory()
    {
        if (slotParent != null && !IsInventoryOpen)
        {
            slotParent.gameObject.SetActive(true);
        }
    }

    public void CloseInventory()
    {
        if (slotParent != null && IsInventoryOpen)
        {
            slotParent.gameObject.SetActive(false);
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
