using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PlayerData", menuName = "Game/Player Data")]
public class PlayerDataSO : ScriptableObject
{
    [Header("Health")]
    public int maxHealth = 100;
    public int currentHealth = 100;

    [Header("Mana")]
    public int maxMana = 100;
    public int currentMana = 100;

    [Header("XP")]
    public int maxXP = 100;
    public int currentXP = 0;
    public int currentLevel = 1;

    [Header("Hunger")]
    public int maxHunger = 100;
    public int currentHunger = 100;

    [Header("Starting Hotbar items")]
    public List<Item> startingHotbarItems = new List<Item>();
    public List<Item> startingBackpackItems = new List<Item>();

    public void ResetStats()
    {
        currentHealth = maxHealth;
        currentMana = maxMana;
        currentHunger = maxHunger;
        currentXP = 0;
        currentLevel = 1;
    }

    public void TakeDamage(int amount) => currentHealth = Mathf.Max(0, currentHealth - amount);
    public void UseMana(int amount) => currentMana = Mathf.Max(0, currentMana - amount);
    public void EatFood(int amount) => currentHunger = Mathf.Min(maxHunger, currentHunger + amount);
    public void GainXP(int amount)
    {
        currentXP += amount;
        if (currentXP >= maxXP)
        {
            currentXP -= maxXP;
            currentLevel++;
            maxXP += 50;
        }
    }
}
