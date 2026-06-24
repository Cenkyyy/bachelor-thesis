using System.Collections;
using UnityEngine;

/// <summary>
/// Persistent audio service for master volume, menu and gameplay music, and shared UI sound effects.
/// </summary>
[DisallowMultipleComponent]
public sealed class AudioManager : MonoBehaviour
{
    private const string MasterVolumePrefKey = "Audio.MasterVolume";

    public static AudioManager Instance { get; private set; }

    [Header("Music")]
    [SerializeField] private AudioSource _musicSource;
    [SerializeField] private AudioClip _menuMusicClip;

    [Header("UI SFX")]
    [SerializeField] private AudioSource _uiSfxSource;
    [SerializeField] private AudioClip _defaultUiHoverClip;
    [SerializeField] private AudioClip _defaultUiClickClip;

    [field: Header("Volume")]
    [field: SerializeField, Range(0f, 1f)] public float MasterVolume { get; private set; } = 1f;

    private float _musicSourceDefaultVolume = 1f;
    private Coroutine _fadeCoroutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        _musicSourceDefaultVolume = Mathf.Clamp01(_musicSource.volume);
        MasterVolume = PlayerPrefs.GetFloat(MasterVolumePrefKey, MasterVolume);
        AudioListener.volume = MasterVolume;
    }

    public void SetMasterVolume(float value)
    {
        MasterVolume = Mathf.Clamp01(value);
        AudioListener.volume = MasterVolume;

        PlayerPrefs.SetFloat(MasterVolumePrefKey, MasterVolume);
        PlayerPrefs.Save();
    }

    public void PlayMenuMusic(bool shouldFadeIn, bool shouldFadeOut, float fadeDuration, bool shouldLoop = true)
    {
        if (shouldFadeOut)
        {
            FadeOutMusic(fadeDuration);
            return;
        }

        bool shouldStartFade = shouldFadeIn && ShouldStartNewMusic(_menuMusicClip);
        PlayMusic(_menuMusicClip, shouldLoop, shouldStartFade);

        if (shouldStartFade)
            FadeInMusic(fadeDuration);
    }

    public void PlayGameplayMusic(AudioClip clip, bool shouldFadeIn, float fadeDuration, bool shouldLoop = false)
    {
        bool shouldStartFade = shouldFadeIn && ShouldStartNewMusic(clip);
        PlayMusic(clip, shouldLoop, shouldStartFade);

        if (shouldStartFade)
            FadeInMusic(fadeDuration);
    }

    public bool IsCurrentMusic(AudioClip clip)
    {
        return clip != null && _musicSource != null && _musicSource.clip == clip;
    }

    public bool IsPlayingMusic(AudioClip clip)
    {
        return IsCurrentMusic(clip) && _musicSource != null && _musicSource.isPlaying;
    }

    public void PlayUiHoverSfx()
    {
        PlayUiSfx(_defaultUiHoverClip);
    }

    public void PlayUiClickSfx()
    {
        PlayUiSfx(_defaultUiClickClip);
    }

    public void StopMusic()
    {
        StopFadeCoroutineIfRunning();
        _musicSource.Stop();
        _musicSource.volume = _musicSourceDefaultVolume;
    }

    public void PauseMusic()
    {
        StopFadeCoroutineIfRunning();
        _musicSource.Pause();
    }

    public void ResumeMusic()
    {
        _musicSource.volume = _musicSourceDefaultVolume;
        _musicSource.UnPause();
    }

    public void FadeOutAllAudio(float fadeDuration)
    {
        FadeOutMusic(fadeDuration);
    }

    public void FadeOutMusic(float fadeDuration)
    {
        FadeMusic(0f, fadeDuration, true);
    }

    private void FadeInMusic(float fadeDuration)
    {
        if (_musicSource.isPlaying && fadeDuration > 0f)
            _musicSource.volume = 0f;

        FadeMusic(_musicSourceDefaultVolume, fadeDuration, false);
    }

    private bool ShouldStartNewMusic(AudioClip clip)
    {
        if (clip == null)
            return false;

        return _musicSource.clip != clip || !_musicSource.isPlaying;
    }

    private void PlayMusic(AudioClip clip, bool shouldLoop, bool startSilent)
    {
        if (clip == null)
        {
            _musicSource.Stop();
            return;
        }

        if (_musicSource.clip == clip && _musicSource.isPlaying)
            return;

        _musicSource.clip = clip;
        _musicSource.loop = shouldLoop;
        _musicSource.volume = startSilent ? 0f : _musicSourceDefaultVolume;
        _musicSource.Play();
    }

    private void PlayUiSfx(AudioClip clip)
    {
        if (_uiSfxSource == null || clip == null)
            return;

        _uiSfxSource.Stop();
        _uiSfxSource.clip = clip;
        _uiSfxSource.Play();
    }

    private void FadeMusic(float targetVolume, float fadeDuration, bool stopMusicOnComplete)
    {
        StopFadeCoroutineIfRunning();
        _fadeCoroutine = StartCoroutine(FadeMusicCoroutine(targetVolume, fadeDuration, stopMusicOnComplete));
    }

    private IEnumerator FadeMusicCoroutine(float targetVolume, float fadeDuration, bool stopMusicOnComplete)
    {
        if (!_musicSource.isPlaying)
        {
            _fadeCoroutine = null;
            yield break;
        }

        if (fadeDuration <= 0f)
        {
            _musicSource.volume = targetVolume;

            if (stopMusicOnComplete)
                StopMusic();

            _fadeCoroutine = null;
            yield break;
        }

        float elapsed = 0f;
        float startVolume = _musicSource.volume;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / fadeDuration);
            _musicSource.volume = Mathf.Lerp(startVolume, targetVolume, t);
            yield return null;
        }

        _musicSource.volume = targetVolume;

        if (stopMusicOnComplete)
            StopMusic();

        _fadeCoroutine = null;
    }

    private void StopFadeCoroutineIfRunning()
    {
        if (_fadeCoroutine == null)
            return;

        StopCoroutine(_fadeCoroutine);
        _fadeCoroutine = null;
    }
}
