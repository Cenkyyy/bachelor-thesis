using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlayerRegenerationController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Player _player;

    [Header("Health Regeneration Rules")]
    [SerializeField, Range(0f, 1f)] private float _minimumHungerPercentForHealthRegen = 0.5f;
    [SerializeField, Min(0f)] private float _healthRegenDelayAfterDamageSeconds = 4f;

    private float _healthRemainder;
    private float _manaRemainder;
    private float _nextAllowedHealthRegenTime;
    private float _healthTickTimer;
    private float _manaTickTimer;
    private int _lastKnownHealth = -1;

    private void Awake()
    {
        if (_player == null)
            _player = GetComponent<Player>();
    }

    private void OnEnable()
    {
        if (_player?.Data != null)
        {
            _player.Data.OnHealthChanged += HandleHealthChanged;
            _lastKnownHealth = _player.Data.CurrentHealth;
        }
    }

    private void OnDisable()
    {
        if (_player?.Data != null)
            _player.Data.OnHealthChanged -= HandleHealthChanged;

        _lastKnownHealth = -1;
    }

    private void Update()
    {
        if (_player?.Data == null || GameStateManager.IsGamePaused)
            return;

        TickManaRegeneration();
        TickHealthRegeneration();
    }

    private void TickManaRegeneration()
    {
        var regenPerSecond = Mathf.Max(0f, _player.Data.ManaRegeneration);
        if (regenPerSecond <= 0f || _player.Data.CurrentMana >= _player.Data.MaxMana)
        {
            _manaTickTimer = 0f;
            return;
        }

        _manaTickTimer += Time.deltaTime;

        while (_manaTickTimer >= 1f)
        {
            _manaTickTimer -= 1f;
            var recoveredMana = ConvertRegenPerSecondToWholeAmount(regenPerSecond, ref _manaRemainder);
            if (recoveredMana > 0)
                _player.Data.RecoverMana(recoveredMana);

            if (_player.Data.CurrentMana >= _player.Data.MaxMana)
                break;
        }
    }

    private void TickHealthRegeneration()
    {
        if (_player.Data.CurrentHealth <= 0 || _player.Data.CurrentHealth >= _player.Data.MaxHealth)
        {
            _healthTickTimer = 0f;
            return;
        }

        if (!CanRegenerateHealth())
        {
            // Health regeneration is intentionally not banked while blocked (e.g. post-hit cooldown).
            // Time still passes for timed buffs, but missed healing is not applied later.
            _healthTickTimer = 0f;
            return;
        }

        var regenPerSecond = Mathf.Max(0f, _player.Data.HealthRegeneration);
        if (regenPerSecond <= 0f)
            return;

        _healthTickTimer += Time.deltaTime;

        while (_healthTickTimer >= 1f)
        {
            _healthTickTimer -= 1f;
            var recoveredHealth = ConvertRegenPerSecondToWholeAmount(regenPerSecond, ref _healthRemainder);
            if (recoveredHealth > 0)
                _player.Data.Heal(recoveredHealth);

            if (_player.Data.CurrentHealth >= _player.Data.MaxHealth)
                break;
        }
    }

    private bool CanRegenerateHealth()
    {
        if (Time.time < _nextAllowedHealthRegenTime)
            return false;

        if (_player.Data.MaxHunger <= 0)
            return false;

        var hungerPercent = (float)_player.Data.CurrentHunger / _player.Data.MaxHunger;
        return hungerPercent >= _minimumHungerPercentForHealthRegen;
    }

    private void HandleHealthChanged(int currentHealth, int _)
    {
        if (_lastKnownHealth >= 0 && currentHealth < _lastKnownHealth)
            _nextAllowedHealthRegenTime = Time.time + _healthRegenDelayAfterDamageSeconds;

        _lastKnownHealth = currentHealth;
    }

    private static int ConvertRegenPerSecondToWholeAmount(float regenPerSecond, ref float remainder)
    {
        remainder += regenPerSecond;
        int wholeAmount = Mathf.FloorToInt(remainder);
        if (wholeAmount <= 0)
            return 0;

        remainder -= wholeAmount;
        return wholeAmount;
    }
}
