using UnityEngine;

public class BackpackPresenter : InventoryPresenterBase<Slot>
{
    [SerializeField] private KeyCode toggleKey = KeyCode.E;

    protected override int SlotCount => playerInventory.Inventory.InventorySize;

    public bool IsInventoryOpen => slotParent != null && slotParent.gameObject.activeSelf;

    protected override void Start()
    {
        base.Start();

        // temporarly set the inventory panel active to instantiate slots
        bool wasActive = slotParent.gameObject.activeSelf;
        slotParent.gameObject.SetActive(true);

        int offset = playerInventory.Inventory.HotbarSize;
        slots = new Slot[SlotCount];
        for (int i = 0; i < SlotCount; i++)
        {
            slots[i] = Instantiate(slotPrefab, slotParent);
            slots[i].Bind(i + offset, playerInventory.Inventory.GetItemAt(i + offset));

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

    public void CloseInventory()
    {
        if (slotParent != null && IsInventoryOpen)
        {
            slotParent.gameObject.SetActive(false);
        }
    }

    public override void RefreshSlot(int backpackIndex)
    {
        int slotIndex = backpackIndex - playerInventory.Inventory.HotbarSize;
        if (slotIndex >= 0 && slotIndex < slots.Length)
        {
            slots[slotIndex].Refresh(playerInventory.Inventory.GetItemAt(backpackIndex));
        }
    }
}
