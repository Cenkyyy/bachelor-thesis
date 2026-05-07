using System.Collections;
using UnityEngine;

/// <summary>
/// Composes player data (stats), item storage (inventory), and combat words.
/// </summary>
[DisallowMultipleComponent]
public sealed class Player : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private PlayerData _playerData;

    [Header("Inventory")]
    [SerializeField] private int _hotbarSize = 8;
    [SerializeField] private int _inventorySize = 24;

    [Header("Combat")]
    [SerializeField] private SpellWordInventory _spellWordInventory;

    /// <summary> Runtime player stats (health, mana, xp, hunger...). </summary>
    public PlayerRuntimeData Data { get; private set; } = new PlayerRuntimeData();

    /// <summary> Runtime player inventory (hotbar + backpack). </summary>
    public PlayerInventory Inventory { get; private set; }

    /// <summary> Runtime player equipment (strictly typed equipment slots). </summary>
    public EquipmentInventory Equipment { get; private set; }

    /// <summary> Runtime unlocked combat words owned by player. </summary>
    public SpellWordInventory SpellWords => _spellWordInventory;

    private void Awake()
    {
        Inventory = new PlayerInventory(_hotbarSize, _inventorySize);
        Equipment = new EquipmentInventory();

        StartCoroutine(InitializeRuntimeDataCoroutine());
    }

    private IEnumerator InitializeRuntimeDataCoroutine()
    {
        yield return null;

        if (_playerData == null)
            yield break;

        Data.InitializeFrom(_playerData);
        Inventory.InitializeFrom(_playerData);
    }
}
