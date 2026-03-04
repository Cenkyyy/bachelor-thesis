using UnityEngine;

[DisallowMultipleComponent]
public sealed class EnemyDamageable : MonoBehaviour, IDamageable
{
    [Header("References")]
    [SerializeField] private EnemyCore _enemyCore;
    [SerializeField] private EnemyDeathRewards _deathRewards;

    [Header("Debug")]
    [SerializeField] private int _currentHealth;
    [SerializeField] private int _maxHealth;
    [SerializeField] private bool _isDead;

    public bool CanReceiveDamage => _enemyCore != null && _enemyCore.Data != null && _enemyCore.RuntimeData != null && !_enemyCore.RuntimeData.IsDead;

    private void Awake()
    {
        if (_enemyCore == null)
            _enemyCore = GetComponent<EnemyCore>() ?? GetComponentInParent<EnemyCore>();

        if (_deathRewards == null)
            _deathRewards = GetComponent<EnemyDeathRewards>() ?? GetComponentInParent<EnemyDeathRewards>();
    }

    private void OnEnable()
    {
        if (_enemyCore == null)
            return;

        _enemyCore.RuntimeData.OnHealthChanged += HandleHealthChanged;
        _enemyCore.RuntimeData.OnDied += HandleDied;
        HandleHealthChanged(_enemyCore.RuntimeData.CurrentHealth, _enemyCore.RuntimeData.MaxHealth);
    }

    private void OnDisable()
    {
        if (_enemyCore == null)
            return;

        _enemyCore.RuntimeData.OnHealthChanged -= HandleHealthChanged;
        _enemyCore.RuntimeData.OnDied -= HandleDied;
    }

    public void ReceiveDamage(int amount, object source = null)
    {
        if (!CanReceiveDamage || amount <= 0)
            return;

        _enemyCore.RuntimeData.TakeDamage(amount);
        if (_enemyCore.RuntimeData.IsDead)
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

        if (_enemyCore != null)
            _enemyCore.RequestState(ActorStateId.Dead);
    }
}
