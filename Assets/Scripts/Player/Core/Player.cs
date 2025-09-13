using UnityEngine;

/// <summary>
/// Composes player data (stats) and item storage (inventory) and wires them up from a PlayerDataSO.
/// </summary>
public class Player : MonoBehaviour
{
    [Header(UIStrings.Player_Data__Title)]
    [SerializeField] private PlayerDataSO _playerDataSO;

    [Header(UIStrings.Player_InventorySettings__Title)]
    [SerializeField] private int _hotbarSize = 8;
    [SerializeField] private int _inventorySize = 24;

    /// <summary> Runtime player stats (health, mana, xp, hunger...). </summary>
    public PlayerData Data { get; private set; } = new PlayerData();

    /// <summary> Runtime player inventory (hotbar + backpack). </summary>
    public PlayerInventory Inventory { get; private set; }

    private void Awake()
    {
        // initialize player's stats from defaults
        Data.InitializeFrom(_playerDataSO);

        // initialzie player's inventory with specified sizes and fill with starting items
        Inventory = new PlayerInventory(_hotbarSize, _inventorySize);
        Inventory.InitializeFromSO(_playerDataSO);
    }
}