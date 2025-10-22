using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Default stats and starting items of a player to be read from.
/// </summary>
[CreateAssetMenu(fileName = "PlayerData", menuName = "Game/Player Data")]
public class PlayerDataSO : ScriptableObject
{
    public int baseMaxHealth = 100;

    public int baseMaxMana = 100;

    public int baseMaxXP = 100;
    public int maxLevel = 200;

    public int baseMaxMemoryXP = 100;
    public int memoryMaxLevel = 200;

    public int baseMaxHunger = 100;

    public List<InventoryItem> startingHotbarItems = new List<InventoryItem>();
    public List<InventoryItem> startingBackpackItems = new List<InventoryItem>();
}
