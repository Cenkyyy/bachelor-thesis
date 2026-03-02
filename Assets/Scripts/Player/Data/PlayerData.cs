using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Default stats and starting items of a player to be read from.
/// </summary>
[CreateAssetMenu(fileName = "PlayerData", menuName = "Game/Player Data")]
public class PlayerData : ScriptableObject
{
    [field: Header("Base Stats")]
    [field: SerializeField] public int BaseMaxHealth { get; private set; } = 100;
    [field: SerializeField] public int BaseMaxMana { get; private set; } = 100;

    [field: Header("Experience")]
    [field: SerializeField] public int BaseMaxXP { get; private set; } = 100;
    [field: SerializeField] public int MaxLevel { get; private set; } = 200;

    [field: Header("Memory Experience")]
    [field: SerializeField] public int BaseMaxMemoryXP { get; private set; } = 100;
    [field: SerializeField] public int MemoryMaxLevel { get; private set; } = 200;

    [field: Header("Needs")]
    [field: SerializeField] public int BaseMaxHunger { get; private set; } = 100;

    [Header("Starting Inventory")]
    [SerializeField] private List<InventoryItem> _startingHotbarItems = new();
    [SerializeField] private List<InventoryItem> _startingBackpackItems = new();

    public IReadOnlyList<InventoryItem> StartingHotbarItems => _startingHotbarItems;
    public IReadOnlyList<InventoryItem> StartingBackpackItems => _startingBackpackItems;
}
