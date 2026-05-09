using UnityEngine;

[DisallowMultipleComponent]
public sealed class EntityDamageable : MonoBehaviour, IDamageable
{
    [Header("References")]
    [SerializeField] private EntityCore _entityCore;
    [SerializeField] private EnemyDeathRewards _deathRewards;

    [Header("Debug")]
    [SerializeField] private int _currentHealth;
    [SerializeField] private int _maxHealth;
    [SerializeField] private bool _isDead;

    public bool CanReceiveDamage => 
        _entityCore != null &&
        _entityCore.Data != null &&
        _entityCore.RuntimeData != null &&
        !_entityCore.RuntimeData.IsDead;

    private void Awake()
    {
        if (_entityCore == null)
            _entityCore = GetComponent<EntityCore>() ?? GetComponentInParent<EntityCore>();

        if (_deathRewards == null)
            _deathRewards = GetComponent<EnemyDeathRewards>() ?? GetComponentInParent<EnemyDeathRewards>();
    }

    private void OnEnable()
    {
        if (_entityCore == null)
            return;

        _entityCore.RuntimeData.OnHealthChanged += HandleHealthChanged;
        _entityCore.RuntimeData.OnDied += HandleDied;
        HandleHealthChanged(_entityCore.RuntimeData.CurrentHealth, _entityCore.RuntimeData.MaxHealth);
    }

    private void OnDisable()
    {
        if (_entityCore == null)
            return;

        _entityCore.RuntimeData.OnHealthChanged -= HandleHealthChanged;
        _entityCore.RuntimeData.OnDied -= HandleDied;
    }

    public void ReceiveDamage(int amount, object source = null)
    {
        if (!CanReceiveDamage || amount <= 0)
            return;

        _entityCore.RuntimeData.TakeDamage(amount);
        if (_entityCore.RuntimeData.IsDead)
            _deathRewards?.HandleEnemyDied(source);
    }

    private void HandleHealthChanged(int currentHealth, int maxHealth)
    {
        _currentHealth = currentHealth;
        _maxHealth = maxHealth;
        _isDead = currentHealth <= 0;
    }

    private void HandleDied()
    {
        _isDead = true;
        _entityCore?.RequestState(StateId.Dead);
    }
}
