using UnityEngine;

public class BackpackController : MonoBehaviour
{
    [SerializeField] Transform backpackPanel;
    [SerializeField] Slot slotPrefab;
    [SerializeField] PlayerInventoryWrapper playerInventory;

    private Slot[] _slots;

    public bool IsInventoryOpen => backpackPanel != null && backpackPanel.gameObject.activeSelf;

    private void Start()
    {
        // temporarly set the inventory panel active to instantiate slots
        backpackPanel.gameObject.SetActive(true);

        _slots = new Slot[playerInventory.Inventory.InventorySize];

        // create hotbar slots
        for (int i = 0; i < playerInventory.Inventory.InventorySize; i++)
        {
            _slots[i] = Instantiate(slotPrefab, backpackPanel.transform).GetComponent<Slot>();
            _slots[i].Bind(i + playerInventory.Inventory.HotbarSize, playerInventory.Inventory.GetItemAt(i + playerInventory.Inventory.HotbarSize));
        }

        backpackPanel.gameObject.SetActive(false);
    }

    private void Update()
    {
        // toggle inventory with 'E' key while the game is not paused
        if (!GameStateManager.IsGamePaused && Input.GetKeyDown(KeyCode.E))
        {
            backpackPanel.gameObject.SetActive(!backpackPanel.gameObject.activeSelf);
        }
    }

    public void CloseInventory()
    {
        if (backpackPanel != null && IsInventoryOpen)
        {
            backpackPanel.gameObject.SetActive(false);
        }
    }

    public void RefreshSlot(int inventoryIndex)
    {
        int slotIndex = inventoryIndex - playerInventory.Inventory.HotbarSize;
        if (slotIndex >= 0 && slotIndex < _slots.Length)
        {
            _slots[slotIndex].Refresh(playerInventory.Inventory.GetItemAt(inventoryIndex));
        }
    }
}
