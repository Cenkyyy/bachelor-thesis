using System;
using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlayerRespawnController : MonoBehaviour
{
    [Header("Player")]
    [SerializeField] private Player _player;
    [SerializeField] private Transform _playerTransform;
    [SerializeField] private DeathDropController _deathDropController;
    [Tooltip("Optional explicit default spawn point. If empty, current player position at Start is used")]
    [SerializeField] private Transform _defaultSpawnPoint;

    [Header("Respawn Placement")]
    [SerializeField] private LayerMask _respawnObstacleMask = ~0;
    [SerializeField, Min(0.01f)] private float _respawnProbeRadius = 0.25f;
    [SerializeField, Min(1)] private int _searchRadius = 3;

    [Header("Death Fade")]
    [SerializeField] private GameObject _deathFadeRoot;
    [SerializeField] private CanvasGroup _deathFadeCanvasGroup;
    [SerializeField, Min(0f)] private float _fadeInDuration = 0.6f;
    [SerializeField, Min(0f)] private float _fadeOutDuration = 0.6f;
    [SerializeField, Min(0.001f)] private float _maxFadeDeltaTime = 0.05f;

    public event Action OnDefeated;
    public event Action OnRespawned;

    public bool IsDefeated { get; private set; }

    private OverlayFadeService _fadeService;
    private Coroutine _deathSequenceRoutine;

    private void Awake()
    {
        if (_deathFadeCanvasGroup == null && _deathFadeRoot != null)
            _deathFadeCanvasGroup = _deathFadeRoot.GetComponent<CanvasGroup>();

        _fadeService = new OverlayFadeService(_deathFadeCanvasGroup, _maxFadeDeltaTime);
        SetDeathFadeVisible(false);
        _fadeService.SetAlpha(0f);
    }

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

        if (_deathSequenceRoutine != null)
            StopCoroutine(_deathSequenceRoutine);

        _deathSequenceRoutine = null;
        SetDeathFadeVisible(false);
        GameStateManager.ReleasePauseLock(this);
    }

    public void Respawn()
    {
        if (!IsDefeated)
            return;

        _playerTransform.position = GetSafeRespawnPosition(_player.Data.SpawnPoint);

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

    private Vector3 GetSafeRespawnPosition(Vector3 spawnPoint)
    {
        // in case the spawn point has obstacle on it (e.g. death chest, spawn the player to the closest free tile)
        if (!HasObstacleAt(spawnPoint))
            return spawnPoint;

        // iterate through all radii starting from 1 to max search radius
        // and check if the position has an obstacle on it, if not, spawn the player there
        for (var radius = 1; radius <= _searchRadius; radius++)
        {
            for (var y = -radius; y <= radius; y++)
            {
                for (var x = -radius; x <= radius; x++)
                {
                    if (x == 0 && y == 0)
                        continue;

                    var candidate = spawnPoint + new Vector3(x, y, 0f);
                    if (!HasObstacleAt(candidate))
                        return candidate;
                }
            }
        }

        return spawnPoint;
    }

    private bool HasObstacleAt(Vector3 worldPoint)
    {
        return Physics2D.OverlapCircle(worldPoint, _respawnProbeRadius, _respawnObstacleMask) != null;
    }

    private void HandleHealthChanged(int currentHealth, int maxHealth)
    {
        if (IsDefeated || currentHealth > 0)
            return;

        IsDefeated = true;
        OnDefeated?.Invoke();

        if (_deathSequenceRoutine == null)
            _deathSequenceRoutine = StartCoroutine(RunDeathRespawnSequence());
    }

    private IEnumerator RunDeathRespawnSequence()
    {
        PanelManager.Instance.CloseCurrentMajorPanel(force: true);
        GameStateManager.AcquirePauseLock(this);
        
        SetDeathFadeVisible(true);
        yield return _fadeService.FadeIn(_fadeInDuration);

        _deathDropController.CreateDeathChestFromBackpack(_player, _playerTransform.position);
        Respawn();

        yield return _fadeService.FadeOut(_fadeOutDuration);
        SetDeathFadeVisible(false);

        GameStateManager.ReleasePauseLock(this);
        _deathSequenceRoutine = null;
    }

    private void SetDeathFadeVisible(bool visible)
    {
        if (_deathFadeRoot == null)
            return;

        if (_deathFadeRoot.activeSelf == visible)
            return;

        _deathFadeRoot.SetActive(visible);
    }
}
