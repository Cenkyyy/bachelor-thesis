using System;
using UnityEngine;

[Serializable]
public class EntityRuntimeData
{
    public int MaxHealth { get; protected set; }
    public int CurrentHealth { get; protected set; }
    public bool IsDead => CurrentHealth <= 0;

    public event Action<int, int> OnHealthChanged;
    public event Action OnDied;

    public virtual void InitializeFrom(EntityData data)
    {
        if (data == null)
            throw new ArgumentNullException(nameof(data));

        MaxHealth = data.MaxHealth;
        CurrentHealth = MaxHealth;

        OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);
    }

    public bool TakeDamage(int amount)
    {
        if (amount <= 0 || IsDead)
            return false;

        CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
        OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);

        if (CurrentHealth > 0)
            return false;

        OnDied?.Invoke();
        return true;
    }
}
