using UnityEngine;

public class BackpackController : SlotControllerBase<Slot>
{
    [SerializeField] private KeyCode toggleKey = KeyCode.E;

    public bool IsInventoryOpen => slotParent != null && slotParent.gameObject.activeSelf;

    protected override int SlotCount => playerInventory.Inventory.InventorySize;

    protected override void Start()
    {
        // temporarly set the inventory panel active to instantiate slots
        bool originallyActive = slotParent.gameObject.activeSelf;
        slotParent.gameObject.SetActive(true);

        base.Start();

        for (int i = 0; i < slots.Length; i++)
        {
            slots[i].GetComponent<HotbarSlot>()?.SetToDefault();
        }

        slotParent.gameObject.SetActive(originallyActive);
    }

    protected override void CreateAndBindSlots(int offset)
    {
        base.CreateAndBindSlots(playerInventory.Inventory.HotbarSize);
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
