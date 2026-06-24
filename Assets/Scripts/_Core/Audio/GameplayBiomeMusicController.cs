using System;
using UnityEngine;

/// <summary>
/// Controls biome-based exploration music during gameplay. When the player stays in a biome,
/// the matching ambient track starts after a randomized delay and fades in. Tracks play once by default,
/// then use the same randomized delay before another cue can start. On biome changes, current biome music
/// fades out immediately, then the next biome track is scheduled. Music also fades out when the player dies.
/// Music pauses immediately when the game is paused and resumes when unpaused.
/// </summary>
[DisallowMultipleComponent]
public sealed class GameplayBiomeMusicController : MonoBehaviour
{
    [Serializable]
    private struct BiomeMusicTrack
    {
        public WorldBiomeType Biome;
        public AudioClip Clip;
    }

    [Header("References")]
    [SerializeField] private WorldGenerationController _worldGenerator;
    [SerializeField] private Player _player;
    [SerializeField] private Transform _playerTransform;

    [Header("Biome Tracks")]
    [SerializeField] private BiomeMusicTrack[] _biomeTracks;

    [Header("Start Timing")]
    [SerializeField, Min(0.05f)] private float _biomeCheckIntervalSeconds = 0.5f;
    [SerializeField, Min(0f)] private float _minStartDelaySeconds = 12f;
    [SerializeField, Min(0f)] private float _maxStartDelaySeconds = 25f;
    [SerializeField, Min(0f)] private float _fadeInSeconds = 1.5f;

    [Header("Stop Timing")]
    [SerializeField, Min(0f)] private float _minFadeOutSeconds = 1f;
    [SerializeField, Min(0f)] private float _maxFadeOutSeconds = 4f;

    private WorldBiomeType _observedBiome = WorldBiomeType.None;
    private WorldBiomeType _scheduledBiome = WorldBiomeType.None;
    private AudioClip _ownedClip;
    private float _nextBiomeCheckTime;
    private float _scheduledStartTime;
    private bool _ownsActiveMusic;

    private void OnEnable()
    {
        GameStateManager.PauseChanged += HandlePauseChanged;
    }

    private void Start()
    {
        if (_player.Data != null)
            _player.Data.OnHealthChanged += HandlePlayerHealthChanged;
    }

    private void Update()
    {
        if (GameStateManager.IsGamePaused)
            return;

        if (Time.time < _nextBiomeCheckTime)
            return;

        _nextBiomeCheckTime = Time.time + _biomeCheckIntervalSeconds;

        HandleOwnedMusicCompletion();

        if (!TryResolveCurrentBiome(out WorldBiomeType currentBiome) || !TryGetClip(currentBiome, out AudioClip clip))
        {
            ClearScheduledStart();
            return;
        }

        if (currentBiome != _observedBiome)
        {
            _observedBiome = currentBiome;
            FadeOutCurrentBiomeMusic();
            ScheduleBiomeStart(currentBiome);
            return;
        }

        if (_scheduledBiome == WorldBiomeType.None)
            ScheduleBiomeStart(currentBiome);

        if (_scheduledBiome == currentBiome && Time.time >= _scheduledStartTime)
            StartOrSwitchToBiomeMusic(currentBiome, clip);
    }

    private void OnDisable()
    {
        GameStateManager.PauseChanged -= HandlePauseChanged;
        _player.Data.OnHealthChanged -= HandlePlayerHealthChanged;
        StopGameplayMusic();
    }

    public void StopGameplayMusic()
    {
        ClearScheduledStart();

        if (_ownedClip == null || !AudioManager.Instance.IsCurrentMusic(_ownedClip))
            return;

        AudioManager.Instance.FadeOutMusic(UnityEngine.Random.Range(_minFadeOutSeconds, _maxFadeOutSeconds));
        _ownsActiveMusic = false;
        _ownedClip = null;
    }

    private void StartOrSwitchToBiomeMusic(WorldBiomeType biome, AudioClip clip)
    {
        if (AudioManager.Instance.IsPlayingMusic(clip))
        {
            ClearScheduledStart();
            _ownsActiveMusic = true;
            _ownedClip = clip;
            return;
        }

        PlayBiomeMusic(clip);
        ClearScheduledStart();
    }

    private void FadeOutCurrentBiomeMusic()
    {
        if (!_ownsActiveMusic || _ownedClip == null || !AudioManager.Instance.IsCurrentMusic(_ownedClip))
            return;

        AudioManager.Instance.FadeOutMusic(UnityEngine.Random.Range(_minFadeOutSeconds, _maxFadeOutSeconds));
        _ownsActiveMusic = false;
        _ownedClip = null;
    }

    private void PlayBiomeMusic(AudioClip clip)
    {
        AudioManager.Instance.PlayGameplayMusic(clip, shouldFadeIn: true, fadeDuration: _fadeInSeconds, shouldLoop: false);
        _ownsActiveMusic = true;
        _ownedClip = clip;
    }

    private void HandleOwnedMusicCompletion()
    {
        if (!_ownsActiveMusic || _ownedClip == null)
            return;

        if (!AudioManager.Instance.IsCurrentMusic(_ownedClip))
        {
            _ownsActiveMusic = false;
            _ownedClip = null;
            return;
        }

        if (AudioManager.Instance.IsPlayingMusic(_ownedClip))
            return;

        _ownsActiveMusic = false;
        _ownedClip = null;
        ClearScheduledStart();
    }

    private void HandlePlayerHealthChanged(int currentHealth, int maxHealth)
    {
        if (currentHealth > 0)
            return;

        StopGameplayMusic();
    }

    private void HandlePauseChanged(bool isPaused)
    {
        if (isPaused)
        {
            AudioManager.Instance.PauseMusic();
            return;
        }

        AudioManager.Instance.ResumeMusic();
    }

    private bool TryResolveCurrentBiome(out WorldBiomeType biome)
    {
        biome = WorldBiomeType.None;

        if (!_worldGenerator.RuntimeState.IsInitialized)
            return false;

        Vector2Int tile = _worldGenerator.RuntimeState.ResolveTileFromWorld(_playerTransform.position);
        biome = _worldGenerator.CurrentWorldData.GetTile(tile.x, tile.y).Biome;
        return biome != WorldBiomeType.None;
    }

    private bool TryGetClip(WorldBiomeType biome, out AudioClip clip)
    {
        for (int i = 0; i < _biomeTracks.Length; i++)
        {
            if (_biomeTracks[i].Biome != biome)
                continue;

            clip = _biomeTracks[i].Clip;
            return clip != null;
        }

        clip = null;
        return false;
    }

    private void ScheduleBiomeStart(WorldBiomeType biome)
    {
        ScheduleStart(biome, UnityEngine.Random.Range(_minStartDelaySeconds, _maxStartDelaySeconds));
    }

    private void ScheduleStart(WorldBiomeType biome, float delaySeconds)
    {
        _scheduledBiome = biome;
        _scheduledStartTime = Time.time + delaySeconds;
    }

    private void ClearScheduledStart()
    {
        _scheduledBiome = WorldBiomeType.None;
        _scheduledStartTime = float.PositiveInfinity;
    }
}
