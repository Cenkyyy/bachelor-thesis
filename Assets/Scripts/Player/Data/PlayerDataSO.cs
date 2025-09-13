using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Default stats and starting items of a player to be read from.
/// </summary>
[CreateAssetMenu(fileName = "PlayerData", menuName = "Game/Player Data")]
public class PlayerDataSO : ScriptableObject
{
    [Header(UIStrings.PlayerData_Health__Title)]
    public int baseMaxHealth = 100;

    [Header(UIStrings.PlayerData_Mana__Title)]
    public int baseMaxMana = 100;

    [Header(UIStrings.PlayerData_XP__Title)]
    public int baseMaxXP = 100;
    public int maxLevel = 200;

    [Header(UIStrings.PlayerData_Hunger__Title)]
    public int baseMaxHunger = 100;

    [Header(UIStrings.PlayerData_StartingItems__Title)]
    public List<InventoryItem> startingHotbarItems = new List<InventoryItem>();
    public List<InventoryItem> startingBackpackItems = new List<InventoryItem>();
}
