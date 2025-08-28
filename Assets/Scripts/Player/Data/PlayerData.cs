using System;
using UnityEngine;

[System.Serializable]
public class PlayerData
{
    // Fields
    public int MaxHealth { get; private set; }
    public int CurrentHealth { get; private set; }

    public int MaxMana { get; private set; }
    public int CurrentMana { get; private set; }

    public int MaxXP { get; private set; }
    public int CurrentXP { get; private set; }
    public int MaxLevel { get; private set; }
    public int CurrentLevel { get; private set; }

    public int MaxHunger { get; private set; }
    public int CurrentHunger { get; private set; }

    // Events
    public event Action<int,int> OnHealthChanged; // currentHealth, maxHealth
    public event Action<int, int> OnManaChanged; // currentMana, maxMana
    public event Action<int, int> OnHungerChanged; // currentHunger, maxHunger
    public event Action<int, int, int> OnXPChanged; // currentXP, maxXP, currentLevel
    public event Action OnInitialized; // when initialized from PlayerDataSO

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
        RaiseAll();
    }

    private void RaiseHealth() => OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);
    private void RaiseMana() => OnManaChanged?.Invoke(CurrentMana, MaxMana);
    private void RaiseHunger() => OnHungerChanged?.Invoke(CurrentHunger, MaxHunger);
    private void RaiseXP() => OnXPChanged?.Invoke(CurrentXP, MaxXP, CurrentLevel);
    private void RaiseAll()
    {
        RaiseHealth();
        RaiseMana();
        RaiseHunger();
        RaiseXP();
    }

    // Health API
    public void TakeDamage(int amount)
    {
        CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
        RaiseHealth();
    }

    public void Heal(int amount)
    {
        CurrentHealth = Mathf.Min(MaxHealth, CurrentHealth + amount);
        RaiseHealth();
    }

    public void ModifyMaxHealth(int delta, bool healWithIncrease = false)
    {
        MaxHealth = Mathf.Max(1, MaxHealth + delta);
        if (healWithIncrease)
        {
            CurrentHealth = Mathf.Min(MaxHealth, CurrentHealth + delta);
        }
        else
        {
            CurrentHealth = Mathf.Min(CurrentHealth, MaxHealth);
        }
        RaiseHealth();
    }

    // Mana API
    public void UseMana(int amount)
    {
        CurrentMana = Mathf.Max(0, CurrentMana - amount);
        RaiseMana();
    }

    public void RecoverMana(int amount)
    {
        CurrentMana = Mathf.Min(MaxMana, CurrentMana + amount);
        RaiseMana();
    }

    public void ModifyMaxMana(int delta)
    {
        MaxMana = Mathf.Max(1, MaxMana + delta);
        CurrentMana = Mathf.Min(CurrentMana, MaxMana);
        RaiseMana();
    }

    // Hunger API
    public void EatFood(int amount)
    {
        CurrentHunger = Mathf.Min(MaxHunger, CurrentHunger + amount);
        RaiseHunger();
    }

    public void ModifyMaxHunger(int delta)
    {
        MaxHunger = Mathf.Max(1, MaxHunger + delta);
        CurrentHunger = Mathf.Min(CurrentHunger, MaxHunger);
        RaiseHunger();
    }

    // XP / Leveling API
    public void GainXP(int amount)
    {
        CurrentXP += amount;

        while (CurrentXP >= MaxXP && CurrentLevel < MaxLevel)
        {
            CurrentXP -= MaxXP;
            CurrentLevel++;
            MaxXP += 50;
            ModifyMaxHealth(10, healWithIncrease: false);
            ModifyMaxMana(5);
        }

        if (CurrentLevel >= MaxLevel)
        {
            CurrentXP = 0;
        }
        RaiseXP();
    }
}