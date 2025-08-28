using UnityEngine;

public class PlayerInventoryWrapper : MonoBehaviour
{
    [Header("Inventory Settings")]
    [SerializeField] int hotbarSize = 8;
    [SerializeField] int inventorySize = 24;

    public PlayerInventory Inventory { get; private set; }

    public void InitializeFromPlayer(PlayerDataSO playerData)
    {
        if (playerData == null)
            return;

        Inventory = new PlayerInventory(hotbarSize, inventorySize);

        // fill hotbar with starting hotbar items (indices 0-7), truncate if more than hotbarSize
        for (int i = 0; i < playerData.startingHotbarItems.Count && i < hotbarSize; i++)
        {
            Inventory.SetItemAt(i, playerData.startingHotbarItems[i]);
            Inventory.SetItemAt(i, new Item(playerData.startingHotbarItems[i].item, playerData.startingHotbarItems[i].amount));
        }

        // fill inventory with starting inventory items (indices 8-31), truncate if more than inventorySize
        for (int i = 0; i < playerData.startingBackpackItems.Count && i < inventorySize; i++)
        {
            Inventory.SetItemAt(i + hotbarSize, new Item(playerData.startingBackpackItems[i].item, playerData.startingBackpackItems[i].amount));
        }
    }
}