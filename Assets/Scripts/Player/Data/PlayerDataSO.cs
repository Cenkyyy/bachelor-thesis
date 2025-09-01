using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PlayerData", menuName = "Game/Player Data")]
public class PlayerDataSO : ScriptableObject
{
    [Header("Health")]
    public int baseMaxHealth = 100;

    [Header("Mana")]
    public int baseMaxMana = 100;

    [Header("XP")]
    public int baseMaxXP = 100;
    public int maxLevel = 200;

    [Header("Hunger")]
    public int baseMaxHunger = 100;

    [Header("Starting Hotbar items")]
    public List<InventoryItem> startingHotbarItems = new List<InventoryItem>();
    public List<InventoryItem> startingBackpackItems = new List<InventoryItem>();
}
