using System.Collections;
using UnityEngine;

/// <summary>
/// Runs the player death sequence, creates death drops, teleports to the spawn point, and restores respawn stats.
/// </summary>
[DisallowMultipleComponent]
public sealed class PlayerRespawnController : MonoBehaviour
{
    [Header("Player")]
    [SerializeField] private Player _player;
    [SerializeField] private Transform _playerTransform;
    [SerializeField] private Rigidbody2D _playerBody;
    [SerializeField] private DeathDropController _deathDropController;
    [SerializeField] private PlayerItemCooldownController _itemCooldownController;
    [SerializeField] private PlayerRegenerationController _regenerationController;
    [SerializeField] private PlayerStatusEffectController _statusEffectController;
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

    private OverlayFadeService _fadeService;
    private Coroutine _deathSequenceRoutine;

    public bool IsDefeated { get; private set; }

    private void Awake()
    {
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
        if (_player.Data != null)
            _player.Data.OnHealthChanged += HandleHealthChanged;
    }

    private void OnDisable()
    {
        if (_player.Data != null)
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

        _deathDropController.CreateDeathChestFromBackpack(_player, _playerTransform.position);
        _playerTransform.position = SpawnPointPlacementUtility.ResolveNearestFreePosition(_player.Data.SpawnPoint, _respawnObstacleMask, _respawnProbeRadius, _searchRadius);

        _playerBody.linearVelocity = Vector2.zero;
        _playerBody.angularVelocity = 0f;

        _itemCooldownController.ClearAllCooldowns();
        _statusEffectController.ClearTimedStatusEffects();
        _regenerationController.ResetTimers();
        _player.Data.Heal(_player.Data.MaxHealth);
        _player.Data.RecoverMana(_player.Data.MaxMana);
        _player.Data.RestoreHunger();

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
        while (true)
        {
            var playerPosition = _playerTransform.position;
            _decorationChunkGenerator.SpawnMissingChunksAround(playerPosition, _respawnGenerationRadiusInChunks, _respawnGenerationChunksPerFrame);

            if (_decorationChunkGenerator.AreChunksSpawnedAround(playerPosition, _respawnGenerationRadiusInChunks))
                yield break;

            yield return null;
        }
    }

    private void SetDeathFadeVisible(bool visible)
    {
        if (_deathFadeRoot.activeSelf == visible)
            return;

        _deathFadeRoot.SetActive(visible);
    }
}
