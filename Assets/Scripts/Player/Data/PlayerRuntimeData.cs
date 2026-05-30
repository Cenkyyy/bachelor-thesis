using System;
using UnityEngine;

/// <summary>
/// Mutable runtime player stats that raise change events on updates.
/// </summary>
[Serializable]
public class PlayerRuntimeData
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

    /// <summary> Memory XP needed to reach the next level. Grows as you level up. </summary>
    public int MaxMemoryXP { get; private set; }

    /// <summary> Current Memory XP progress toward the next level. </summary>
    public int CurrentMemoryXP { get; private set; }

    /// <summary> Maximum attainable memory level (cap). </summary>
    public int MaxMemoryLevel { get; private set; }

    /// <summary> Current memory level. </summary>
    public int CurrentMemoryLevel { get; private set; }

    /// <summary> Maximum hunger value. </summary>
    public int MaxHunger { get; private set; }
    
    /// <summary> Current hunger (0..MaxHunger). </summary>
    public int CurrentHunger { get; private set; }

    /// <summary> World-space point where the player respawns after defeat. </summary>
    public Vector3 SpawnPoint { get; private set; }

    /// <summary> Defensive stat used for incoming damage calculations. </summary>
    public int Defence { get; private set; }

    /// <summary> Health regenerated per second. </summary>
    public float HealthRegeneration { get; private set; }

    /// <summary> Mana regenerated per second. </summary>
    public float ManaRegeneration { get; private set; }

    /// <summary> Flat bonus applied to outgoing spell damage from item bonuses. </summary>
    public float SpellDamageBonus { get; private set; }

    // Events

    /// <summary> Raised after health changes; args: currentHealth, maxHealth. </summary>
    public event Action<int, int> OnHealthChanged;
    /// <summary> Raised when damage is applied; arg: applied damage amount. </summary>
    public event Action<int> OnDamageTaken;
    /// <summary> Raised after mana changes; args: currentMana, maxMana. </summary>
    public event Action<int, int> OnManaChanged;
    /// <summary> Raised after hunger changes; args: currentHunger, maxHunger. </summary>
    public event Action<int, int> OnHungerChanged;
    /// <summary> Raised after XP or level changes; args: currentXP, maxXP, currentLevel. </summary>
    public event Action<int, int, int> OnXPChanged;
    /// <summary> Raised after memory XP or level changes; args: currentMemoryXP, maxMemoryXP, currentMemoryLevel  </summary>
    public event Action<int, int, int> OnMemoryXPChanged;
    /// <summary> Raised after spawn point changes; arg: world-space spawn point. </summary>
    public event Action<Vector3> OnSpawnPointChanged;
    /// <summary> Raised after InitializeFrom() sets all defaults. </summary>
    public event Action OnInitialized;

    private int _baseDefence;
    private float _baseHealthRegeneration;
    private float _baseManaRegeneration;
    private int _maxHealthGainPerLevel;
    private int _maxManaGainPerLevel;
    private float _manaRegenerationGainPerFiveLevels;
    private int _xpGrowthPerLevel;
    private int _baseMaxMemoryXP;
    private int _memoryXpGrowthPerLevel;
    private float _levelManaRegenerationBonus;
    private float _appliedItemManaRegenerationBonus;
    private int _appliedItemMaxHealthBonus;
    private int _appliedItemMaxManaBonus;

    /// <summary>
    /// Initializes all stats from <see cref="PlayerData"/> and raises initial change events.
    /// </summary>
    /// <param name="defaultData">Source of default values.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="defaultData"/> is null.</exception>
    public void InitializeFrom(PlayerData defaultData) 
    {
        if (defaultData == null) 
            throw new ArgumentNullException(nameof(defaultData));

        MaxHealth = defaultData.BaseMaxHealth;
        CurrentHealth = MaxHealth;

        MaxMana = defaultData.BaseMaxMana;
        CurrentMana = MaxMana;

        MaxXP = defaultData.BaseMaxXP;
        CurrentXP = 0;
        MaxLevel = defaultData.MaxLevel;
        CurrentLevel = 0;

        MaxMemoryXP = defaultData.BaseMaxMemoryXP;
        CurrentMemoryXP = 0;
        MaxMemoryLevel = defaultData.MemoryMaxLevel;
        CurrentMemoryLevel = 0;
        _baseMaxMemoryXP = defaultData.BaseMaxMemoryXP;
        _memoryXpGrowthPerLevel = defaultData.MemoryXpGrowthPerLevel;

        MaxHunger = defaultData.BaseMaxHunger;
        CurrentHunger = MaxHunger;

        _baseDefence = defaultData.BaseDefence;
        _baseHealthRegeneration = defaultData.BaseHealthRegeneration;
        _baseManaRegeneration = defaultData.BaseManaRegeneration;
        _maxHealthGainPerLevel = defaultData.MaxHealthGainPerLevel;
        _maxManaGainPerLevel = defaultData.MaxManaGainPerLevel;
        _manaRegenerationGainPerFiveLevels = defaultData.ManaRegenerationGainPerFiveLevels;
        _xpGrowthPerLevel = defaultData.XpGrowthPerLevel;
        _levelManaRegenerationBonus = 0f;
        _appliedItemManaRegenerationBonus = 0f;

        Defence = _baseDefence;
        HealthRegeneration = _baseHealthRegeneration;
        ManaRegeneration = _baseManaRegeneration;
        SpellDamageBonus = 0f;

        SpawnPoint = Vector3.zero;

        OnInitialized?.Invoke();
        OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);
        OnManaChanged?.Invoke(CurrentMana, MaxMana);
        OnHungerChanged?.Invoke(CurrentHunger, MaxHunger);
        OnXPChanged?.Invoke(CurrentXP, MaxXP, CurrentLevel);
        OnMemoryXPChanged?.Invoke(CurrentMemoryXP, MaxMemoryXP, CurrentMemoryLevel);
        OnSpawnPointChanged?.Invoke(SpawnPoint);
    }

    // Health API

    /// <summary>
    /// Applies a signed health change. Positive values heal, negative values deal direct damage.
    /// </summary>
    /// <param name="amount">Signed health change to apply.</param>
    public void ApplyHealthDelta(int amount)
    {
        if (amount > 0)
        {
            Heal(amount);
            return;
        }

        if (amount < 0)
            TakeDamage(Mathf.Abs(amount));
    }

    /// <summary>
    /// Applies damage, clamping to zero, and raises OnHealthChanged event.
    /// </summary>
    /// <param name="amount">Damage to be taken.</param>
    public void TakeDamage(int amount)
    {
        if (amount <= 0)
            return;

        var previousHealth = CurrentHealth;
        CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
        var actualDamageTaken = previousHealth - CurrentHealth;

        if (actualDamageTaken > 0)
            OnDamageTaken?.Invoke(actualDamageTaken);

        OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);
    }

    /// <summary>
    /// Heals by <paramref name="amount"/> up to MaxHealth and raises OnHealthChanged event.
    /// </summary>
    /// <param name="amount">Amount to be healed.</param>
    public void Heal(int amount)
    {
        if (amount <= 0)
            return;

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
        if (amount <= 0)
            return;

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

    // Item stat bonuses API

    /// <summary>
    /// Applies aggregated item modifiers on top of initialized base combat stats.
    /// </summary>
    public void ApplyCombatItemModifiers(float defenceAdditive, float healthRegenAdditive, float manaRegenAdditive, float maxHealthAdditive, float maxManaAdditive, float spellDamageAdditive)
    {
        Defence = Mathf.RoundToInt(_baseDefence + defenceAdditive);
        HealthRegeneration = _baseHealthRegeneration + healthRegenAdditive;
        ManaRegeneration = _baseManaRegeneration + _levelManaRegenerationBonus + manaRegenAdditive;
        _appliedItemManaRegenerationBonus = manaRegenAdditive;
        SpellDamageBonus = spellDamageAdditive;

        int targetHealthBonus = Mathf.RoundToInt(maxHealthAdditive);
        if (targetHealthBonus != _appliedItemMaxHealthBonus)
        {
            int healthDelta = targetHealthBonus - _appliedItemMaxHealthBonus;
            _appliedItemMaxHealthBonus = targetHealthBonus;
            ModifyMaxHealth(healthDelta, healWithIncrease: false);
        }

        int targetManaBonus = Mathf.RoundToInt(maxManaAdditive);
        if (targetManaBonus != _appliedItemMaxManaBonus)
        {
            int manaDelta = targetManaBonus - _appliedItemMaxManaBonus;
            _appliedItemMaxManaBonus = targetManaBonus;
            ModifyMaxMana(manaDelta, restoreWithIncrease: false);
        }
    }

    // Hunger API

    /// <summary>
    /// Increases hunger by <paramref name="amount"/> up to MaxHunger and raises OnHungerChanged event.
    /// </summary>
    /// <param name="amount">Amount to be increased by.</param>
    public void EatFood(int amount)
    {
        if (amount <= 0) 
            return;

        CurrentHunger = Mathf.Min(MaxHunger, CurrentHunger + amount);
        OnHungerChanged?.Invoke(CurrentHunger, MaxHunger);
    }

    /// <summary>
    /// Restores hunger to its current maximum value and raises OnHungerChanged event.
    /// </summary>
    public void RestoreHunger()
    {
        CurrentHunger = MaxHunger;
        OnHungerChanged?.Invoke(CurrentHunger, MaxHunger);
    }

    /// <summary>
    /// Decreases hunger by <paramref name="amount"/>, clamping to zero, and raises OnHungerChanged event.
    /// </summary>
    /// <param name="amount">Amount to be decreased by.</param>
    public void ConsumeHunger(int amount)
    {
        if (amount <= 0)
            return;

        CurrentHunger = Mathf.Max(0, CurrentHunger - amount);
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

            MaxXP += _xpGrowthPerLevel;
            ModifyMaxHealth(_maxHealthGainPerLevel, healWithIncrease: false);
            ModifyMaxMana(_maxManaGainPerLevel, restoreWithIncrease: false);

            var manaRegenMilestones = CurrentLevel / 5;
            _levelManaRegenerationBonus = manaRegenMilestones * _manaRegenerationGainPerFiveLevels;
            ManaRegeneration = _baseManaRegeneration + _levelManaRegenerationBonus + _appliedItemManaRegenerationBonus;
        }

        if (CurrentLevel >= MaxLevel)
        {
            CurrentXP = 0;
        }

        OnXPChanged?.Invoke(CurrentXP, MaxXP, CurrentLevel);
    }

    public void GainMemoryXP(int amount)
    {
        if (amount <= 0) return;

        CurrentMemoryXP += amount;

        while (CurrentMemoryXP >= MaxMemoryXP && CurrentMemoryLevel < MaxMemoryLevel)
        {
            CurrentMemoryXP -= MaxMemoryXP;
            CurrentMemoryLevel++;
            MaxMemoryXP += _memoryXpGrowthPerLevel;
        }

        if (CurrentMemoryLevel >= MaxMemoryLevel)
            CurrentMemoryXP = 0;

        OnMemoryXPChanged?.Invoke(CurrentMemoryXP, MaxMemoryXP, CurrentMemoryLevel);
    }

    public bool TrySpendMemoryLevels(int levels)
    {
        if (levels <= 0) return true;
        if (CurrentMemoryLevel < levels) return false;

        CurrentMemoryLevel -= levels;

        MaxMemoryXP = _baseMaxMemoryXP + _memoryXpGrowthPerLevel * CurrentMemoryLevel;

        CurrentMemoryXP = Mathf.Min(CurrentMemoryXP, MaxMemoryXP - 1);

        OnMemoryXPChanged?.Invoke(CurrentMemoryXP, MaxMemoryXP, CurrentMemoryLevel);
        return true;
    }

    // Spawn Point API

    public void SetSpawnPoint(Vector3 spawnPoint)
    {
        SpawnPoint = spawnPoint;
        OnSpawnPointChanged?.Invoke(SpawnPoint);
    }
}
