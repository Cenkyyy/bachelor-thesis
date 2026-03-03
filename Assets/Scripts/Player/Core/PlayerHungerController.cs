using UnityEngine;

public sealed class PlayerHungerController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Player _player;
    [SerializeField] private PlayerMovement _movement;

    [Header("Travel Hunger Drain")]
    [SerializeField, Min(0.1f)] private float _tilesPerHungerPoint = 10f;
    [SerializeField, Min(0.01f)] private float _worldUnitsPerTile = 1f;
    [SerializeField, Min(0f)] private float _maxCountedTravelPerFrame = 1.5f;

    [Header("Exhaustion Slow")]
    [SerializeField, Range(0f, 1f)] private float _slowThresholdPercent = 0.25f;
    [SerializeField, Range(0f, 1f)] private float _slowSpeedMultiplier = 0.75f;

    [Header("Starvation")]
    [SerializeField, Min(0.1f)] private float _starvationTickIntervalSeconds = 2f;
    [SerializeField, Min(1)] private int _starvationDamagePerTick = 1;
    [SerializeField, Range(0f, 1f)] private float _starvationMinHealthPercent = 0.25f;

    private Vector2 _lastPosition;
    private float _distanceBuffer;
    private float _starvationTickTimer;

    private float DistancePerHungerPoint => _tilesPerHungerPoint * _worldUnitsPerTile;

    private void Awake()
    {
        if (_player == null)
            _player = GetComponent<Player>();

        if (_movement == null)
            _movement = GetComponent<PlayerMovement>();
    }

    private void OnEnable()
    {
        _lastPosition = transform.position;
        _distanceBuffer = 0f;
        _starvationTickTimer = 0f;

        if (_player != null)
            _player.Data.OnHungerChanged += HandleHungerChanged;

        ApplyExhaustionSlow();
    }

    private void OnDisable()
    {
        if (_player != null)
            _player.Data.OnHungerChanged -= HandleHungerChanged;

        if (_movement != null)
            _movement.SetExternalSpeedMultiplier(1f);
    }

    private void Update()
    {
        if (_player == null)
            return;

        TickTravelDrain();
        TickStarvationDamage();
    }

    private void TickTravelDrain()
    {
        var currentPosition = (Vector2)transform.position;
        var traveled = Vector2.Distance(currentPosition, _lastPosition);
        _lastPosition = currentPosition;

        if (traveled <= 0f)
            return;

        if (_maxCountedTravelPerFrame > 0f && traveled > _maxCountedTravelPerFrame)
            return;

        _distanceBuffer += traveled;
        var hungerStepDistance = Mathf.Max(0.01f, DistancePerHungerPoint);
        var hungerToConsume = Mathf.FloorToInt(_distanceBuffer / hungerStepDistance);
        if (hungerToConsume <= 0)
            return;

        _distanceBuffer -= hungerToConsume * hungerStepDistance;
        _player.Data.ConsumeHunger(hungerToConsume);
    }

    private void TickStarvationDamage()
    {
        if (_player.Data.CurrentHunger > 0)
        {
            _starvationTickTimer = 0f;
            return;
        }

        _starvationTickTimer += Time.deltaTime;
        if (_starvationTickTimer < _starvationTickIntervalSeconds)
            return;

        _starvationTickTimer = 0f;

        var minHealth = Mathf.CeilToInt(_player.Data.MaxHealth * _starvationMinHealthPercent);
        if (_player.Data.CurrentHealth <= minHealth)
            return;

        var maxDamageUntilThreshold = _player.Data.CurrentHealth - minHealth;
        var damage = Mathf.Min(_starvationDamagePerTick, maxDamageUntilThreshold);
        if (damage > 0)
            _player.Data.TakeDamage(damage);
    }

    private void HandleHungerChanged(int currentHunger, int maxHunger)
    {
        ApplyExhaustionSlow();
    }

    private void ApplyExhaustionSlow()
    {
        if (_movement == null || _player == null)
            return;

        var speedMultiplier = _player.Data.CurrentHunger < _slowThresholdPercent ? _slowSpeedMultiplier : 1f;
        _movement.SetExternalSpeedMultiplier(speedMultiplier);
    }
}
