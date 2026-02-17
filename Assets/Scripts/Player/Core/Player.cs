using UnityEngine;

/// <summary>
/// Composes player data (stats), item storage (inventory), and combat words.
/// </summary>
public class Player : MonoBehaviour
{
    [SerializeField] private PlayerDataBaseStats _playerData;

    [SerializeField] private int _hotbarSize = 8;
    [SerializeField] private int _inventorySize = 24;

    [Header("Combat")]
    [SerializeField] private SpellWordInventory _spellWordInventory;

    /// <summary> Runtime player stats (health, mana, xp, hunger...). </summary>
    public PlayerData Data { get; private set; } = new PlayerData();

    /// <summary> Runtime player inventory (hotbar + backpack). </summary>
    public PlayerInventory Inventory { get; private set; }

    /// <summary> Runtime player equipment (strictly typed equipment slots). </summary>
    public EquipmentInventory Equipment { get; private set; }

    /// <summary> Runtime unlocked combat words owned by player. </summary>
    public SpellWordInventory SpellWords => _spellWordInventory;

    private void Awake()
    {
        // initialize player's stats from defaults
        Data.InitializeFrom(_playerData);

        // initialize player's inventory with specified sizes and fill with starting items
        Inventory = new PlayerInventory(_hotbarSize, _inventorySize);
        Inventory.InitializeFrom(_playerData);

        Equipment = new EquipmentInventory();
    }
}
