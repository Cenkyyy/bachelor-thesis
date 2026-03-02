using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlayerRespawnController : MonoBehaviour
{
    [SerializeField] private Player _player;
    [SerializeField] private Transform _playerTransform;
    [Tooltip("Optional explicit default spawn point. If empty, current player position at Start is used")]
    [SerializeField] private Transform _defaultSpawnPoint;

    public event Action OnDefeated;
    public event Action OnRespawned;

    public bool IsDefeated { get; private set; }

    private void Start()
    {
        var initialSpawnPoint = _defaultSpawnPoint != null ? _defaultSpawnPoint.position : _playerTransform.position;
        _player.Data.SetSpawnPoint(initialSpawnPoint);
    }

    private void OnEnable()
    {
        if (_player != null)
            _player.Data.OnHealthChanged += HandleHealthChanged;
    }

    private void OnDisable()
    {
        if (_player != null)
            _player.Data.OnHealthChanged -= HandleHealthChanged;
    }

    public void Respawn()
    {
        if (!IsDefeated)
            return;

        _playerTransform.position = _player.Data.SpawnPoint;

        if (_playerTransform.TryGetComponent<Rigidbody2D>(out var body))
        {
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
        }

        _player.Data.Heal(_player.Data.MaxHealth);
        _player.Data.RecoverMana(_player.Data.MaxMana);

        // TODO: reset other player stats, remove some percentage of inventory items, etc

        IsDefeated = false;
        OnRespawned?.Invoke();
    }

    public void ReturnToMenu()
    {
        if (SceneLoader.Instance != null)
        {
            SceneLoader.Instance.LoadMenu();
            return;
        }
    }

    private void HandleHealthChanged(int currentHealth, int maxHealth)
    {
        if (IsDefeated || currentHealth > 0)
            return;

        IsDefeated = true;
        OnDefeated?.Invoke();
    }
}
