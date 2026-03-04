using System;
using UnityEngine;

[Serializable]
public sealed class EnemyRuntimeData
{
    public int MaxHealth { get; private set; }
    public int CurrentHealth { get; private set; }
    public bool IsDead => CurrentHealth <= 0;

    public event Action<int, int> OnHealthChanged;
    public event Action OnDied;

    public void InitializeFrom(EnemyData data)
    {
        if (data == null)
            throw new ArgumentNullException(nameof(data));

        MaxHealth = Mathf.Max(1, data.MaxHealth);
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
