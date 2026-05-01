using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class AudioManager : MonoBehaviour
{
    private const string MasterVolumePrefKey = "Audio.MasterVolume";

    public static AudioManager Instance { get; private set; }

    [Header("Music")]
    [SerializeField] private AudioSource _musicSource;
    [SerializeField] private AudioClip _menuMusicClip;

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

    public void PlayMenuMusic()
    {
        PlayMusic(_menuMusicClip, true);
    }

    public void StopMusic()
    {
        StopFadeCoroutineIfRunning();
        _musicSource.Stop();
        _musicSource.volume = _musicSourceDefaultVolume;
    }

    public void FadeOutAllAudio(float fadeDuration)
    {
        FadeAllAudio(0f, fadeDuration, true);
    }

    public void FadeInAllAudio(float fadeDuration)
    {
        if (_musicSource.isPlaying && fadeDuration > 0f)
            _musicSource.volume = 0f;

        FadeAllAudio(_musicSourceDefaultVolume, fadeDuration, false);
    }

    private void PlayMusic(AudioClip clip, bool shouldLoop)
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
        _musicSource.volume = _musicSourceDefaultVolume;
        _musicSource.Play();
    }

    private void FadeAllAudio(float targetVolume, float fadeDuration, bool stopMusicOnComplete)
    {
        StopFadeCoroutineIfRunning();
        _fadeCoroutine = StartCoroutine(FadeAllAudioCoroutine(targetVolume, fadeDuration, stopMusicOnComplete));
    }

    private IEnumerator FadeAllAudioCoroutine(float targetVolume, float fadeDuration, bool stopMusicOnComplete)
    {
        if (!_musicSource.isPlaying)
            yield break;

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
