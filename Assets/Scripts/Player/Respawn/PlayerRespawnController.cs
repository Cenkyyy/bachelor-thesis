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

    [Header("Respawn Reveal Readiness")]
    [SerializeField] private DecorationChunkGenerator _decorationChunkGenerator;
    [SerializeField, Min(0)] private int _respawnGenerationRadiusInChunks = 1;
    [SerializeField, Min(1)] private int _respawnGenerationChunksPerFrame = 2;

    public bool IsDefeated { get; private set; }

    private OverlayFadeService _fadeService;
    private Coroutine _deathSequenceRoutine;

    private void Awake()
    {
        if (_decorationChunkGenerator == null)
            _decorationChunkGenerator = FindFirstObjectByType<DecorationChunkGenerator>();

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

        // create death chest and transfer the player's backpack items there
        _deathDropController.CreateDeathChestFromBackpack(_player, _playerTransform.position);

        // teleport player to his last spawn point
        _playerTransform.position = SpawnPointPlacementUtility.ResolveNearestFreePosition(_player.Data.SpawnPoint, _respawnObstacleMask, _respawnProbeRadius, _searchRadius);

        if (_playerTransform.TryGetComponent<Rigidbody2D>(out var body))
        {
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
        }

        // recover player's stats
        _player.Data.Heal(_player.Data.MaxHealth);
        _player.Data.RecoverMana(_player.Data.MaxMana);

        // TODO: reset other player stats

        IsDefeated = false;
    }

    private void HandleHealthChanged(int currentHealth, int maxHealth)
    {
        if (IsDefeated || currentHealth > 0)
            return;

        IsDefeated = true;

        if (_deathSequenceRoutine == null)
            _deathSequenceRoutine = StartCoroutine(RunDeathRespawnSequence());
    }

    private IEnumerator RunDeathRespawnSequence()
    {
        PanelManager.Instance.CloseCurrentMajorPanel(force: true);
        GameStateManager.AcquirePauseLock(this);
        
        SetDeathFadeVisible(true);
        yield return _fadeService.FadeIn(_fadeInDuration);

        Respawn();

        yield return WaitForRespawnDecorationsToBeGenerated();

        yield return _fadeService.FadeOut(_fadeOutDuration);
        SetDeathFadeVisible(false);

        GameStateManager.ReleasePauseLock(this);
        _deathSequenceRoutine = null;
    }

    private IEnumerator WaitForRespawnDecorationsToBeGenerated()
    {
        if (_playerTransform == null)
            yield break;

        while (true)
        {
            bool decorationsReady = true;
            var playerPosition = _playerTransform.position;

            if (_decorationChunkGenerator != null)
            {
                _decorationChunkGenerator.SpawnMissingChunksAround(playerPosition, _respawnGenerationRadiusInChunks, _respawnGenerationChunksPerFrame);
                decorationsReady = _decorationChunkGenerator.AreChunksSpawnedAround(playerPosition, _respawnGenerationRadiusInChunks);
            }

            if (decorationsReady)
                yield break;

            yield return null;
        }
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
