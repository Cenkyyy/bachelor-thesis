using System;
using UnityEngine;

/// <summary>
/// Mutable runtime player stats that raise change events on updates.
/// </summary>
[System.Serializable]
public class PlayerData
{
    // Properties

    /// <summary> Maximum health at the current level/equipment state. </summary>
    public int MaxHealth { get; private set; }

    /// <summary> Current health (0..MaxHealth). </summary>
    public int CurrentHealth { get; private set; }

    /// <summary> Maximum mana at the current level/equipment state. </summary>
    public int MaxMana { get; private set; }
    
    /// <summary> Current mana (0..MaxMana). </summary>
    public int CurrentMana { get; private set; }

    /// <summary> XP needed to reach the next level. Grows as you level up. </summary>
    public int MaxXP { get; private set; }
    
    /// <summary> Current XP progress toward the next level. </summary>
    public int CurrentXP { get; private set; }
    
    /// <summary> Maximum attainable player level (cap). </summary>
    public int MaxLevel { get; private set; }
    
    /// <summary> Current level. </summary>
    public int CurrentLevel { get; private set; }

    /// <summary> Maximum hunger value. </summary>
    public int MaxHunger { get; private set; }
    
    /// <summary> Current hunger (0..MaxHunger). </summary>
    public int CurrentHunger { get; private set; }

    // Events

    /// <summary> Raised after health changes; args: currentHealth, maxHealth. </summary>
    public event Action<int, int> OnHealthChanged;
    /// <summary> Raised after mana changes; args: currentMana, maxMana. </summary>
    public event Action<int, int> OnManaChanged;
    /// <summary> Raised after hunger changes; args: currentHunger, maxHunger. </summary>
    public event Action<int, int> OnHungerChanged;
    /// <summary> Raised after XP or level changes; args: currentXP, maxXP, currentLevel. </summary>
    public event Action<int, int, int> OnXPChanged;
    /// <summary> Raised after InitializeFrom() sets all defaults. </summary>
    public event Action OnInitialized;

    /// <summary>
    /// Initializes all stats from <see cref="PlayerDataSO"/> and raises initial change events.
    /// </summary>
    /// <param name="defaultData">Source of default values.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="defaultData"/> is null.</exception>
    public void InitializeFrom(PlayerDataSO defaultData) 
    {
        if (defaultData == null) 
            throw new ArgumentNullException(nameof(defaultData));

        MaxHealth = defaultData.baseMaxHealth;
        CurrentHealth = MaxHealth;

        MaxMana = defaultData.baseMaxMana;
        CurrentMana = MaxMana;

        MaxXP = defaultData.baseMaxXP;
        CurrentXP = 0;
        MaxLevel = defaultData.maxLevel;
        CurrentLevel = 1;

        MaxHunger = defaultData.baseMaxHunger;
        CurrentHunger = MaxHunger;

        OnInitialized?.Invoke();
        OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);
        OnManaChanged?.Invoke(CurrentMana, MaxMana);
        OnHungerChanged?.Invoke(CurrentHunger, MaxHunger);
        OnXPChanged?.Invoke(CurrentXP, MaxXP, CurrentLevel);
    }

    // Health API

    /// <summary>
    /// Applies damage, clamping to zero, and raises OnHealthChanged event.
    /// </summary>
    /// <param name="amount">Damage to be taken.</param>
    public void TakeDamage(int amount)
    {
        CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
        OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);
    }

    /// <summary>
    /// Heals by <paramref name="amount"/> up to MaxHealth and raises OnHealthChanged event.
    /// </summary>
    /// <param name="amount">Amount to be healed.</param>
    public void Heal(int amount)
    {
        CurrentHealth = Mathf.Min(MaxHealth, CurrentHealth + amount);
        OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);
    }

    /// <summary>
    /// Modifies MaxHealth by <paramref name="amount"/> (min 1).
    /// Optionally heals back to full health if <paramref name="healWithIncrease"/> is true.
    /// Raises OnHealthChanged event.
    /// </summary>
    /// <param name="amount">Amount to be healed by.</param>
    /// <param name="healWithIncrease">True if should restore to full health.</param>
    public void ModifyMaxHealth(int amount, bool healWithIncrease = false)
    {
        MaxHealth = Mathf.Max(1, MaxHealth + amount);
        if (healWithIncrease)
        {
            CurrentHealth = MaxHealth;
        }
        else
        {
            CurrentHealth = Mathf.Min(CurrentHealth, MaxHealth);
        }
        OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);
    }

    // Mana API

    /// <summary>
    /// Spends mana, clamping to zero, and raises OnManaChanged event.
    /// </summary>
    /// <param name="amount">Mana to be spend.</param>
    public void UseMana(int amount)
    {
        CurrentMana = Mathf.Max(0, CurrentMana - amount);
        OnManaChanged?.Invoke(CurrentMana, MaxMana);
    }

    /// <summary>
    /// Recovers mana up to MaxMana and raises OnManaChanged event.
    /// </summary>
    /// <param name="amount">Mana to be recovered</param>
    public void RecoverMana(int amount)
    {
        CurrentMana = Mathf.Min(MaxMana, CurrentMana + amount);
        OnManaChanged?.Invoke(CurrentMana, MaxMana);
    }

    /// <summary>
    /// Modifies MaxMana by <paramref name="amount"/> (min 1).
    /// Optionally restores back to full health if <paramref name="restoreWithIncrease"/> is true.
    /// Raises OnManaChanged event.
    /// </summary>
    /// <param name="amount">Amount to be restored.</param>
    /// <param name="restoreWithIncrease">True if should restore to full mana.</param>
    public void ModifyMaxMana(int amount, bool restoreWithIncrease)
    {
        MaxMana = Mathf.Max(1, MaxMana + amount);
        if (restoreWithIncrease)
        {
            CurrentMana = MaxMana;
        }
        else
        {
            CurrentMana = Mathf.Min(CurrentMana, MaxMana);
        }
        OnManaChanged?.Invoke(CurrentMana, MaxMana);
    }

    // Hunger API

    /// <summary>
    /// Increases hunger by <paramref name="amount"/> up to MaxHunger and raises OnHungerChanged event.
    /// </summary>
    /// <param name="amount">Amount to be increased by.</param>
    public void EatFood(int amount)
    {
        CurrentHunger = Mathf.Min(MaxHunger, CurrentHunger + amount);
        OnHungerChanged?.Invoke(CurrentHunger, MaxHunger);
    }

    /// <summary>
    /// Modifies MaxHealthby <paramref name="amount"/> (min 1).
    /// Raises OnHungerChanged event.
    /// </summary>
    /// <param name="amount">Amount to be increased by.</param>
    public void ModifyMaxHunger(int amount)
    {
        MaxHunger = Mathf.Max(1, MaxHunger + amount);
        CurrentHunger = Mathf.Min(CurrentHunger, MaxHunger);
        OnHungerChanged?.Invoke(CurrentHunger, MaxHunger);
    }

    // XP / Leveling API

    /// <summary>
    /// Gains XP by <paramref name="amount"/>, levels up if enough XP is reached.
    /// Each level up increases MaxXP, MaxHealth and MaxMana.
    /// Raises OnXPChanged event.
    /// </summary>
    /// <param name="amount">Amount to gain.</param>
    public void GainXP(int amount)
    {
        CurrentXP += amount;

        while (CurrentXP >= MaxXP && CurrentLevel < MaxLevel)
        {
            CurrentXP -= MaxXP;
            CurrentLevel++;

            // TODO: Modify next level stats upgrades
            MaxXP += 50;
            ModifyMaxHealth(10, healWithIncrease: false);
            ModifyMaxMana(5, restoreWithIncrease: false);
        }

        if (CurrentLevel >= MaxLevel)
        {
            CurrentXP = 0;
        }

        OnXPChanged?.Invoke(CurrentXP, MaxXP, CurrentLevel);
    }
}