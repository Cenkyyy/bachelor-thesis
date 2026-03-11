using System;
using UnityEngine;

/// <summary>
/// Central game-time system that advances an in-game day, raises time events,
/// and provides a continuous brightness value (0..1) for visuals.
/// </summary>
[DisallowMultipleComponent]
public sealed class DayNightSystem : MonoBehaviour
{
    public static DayNightSystem Instance { get; private set; }

    private const int MinutesPerHour = 60;
    private const int HoursPerDay = 24;
    private const int MinutesPerDay = HoursPerDay * MinutesPerHour;

    [Header("Time")]
    [SerializeField, Min(1f)] private float _secondsPerGameDay = 600f; // 10 min per day
    [SerializeField, Range(0f, 1f)] private float _initialTime01 = 0.25f; // ~06:00
    [SerializeField, Min(1)] private int _startDay = 1;

    [Header("Day / Night Window")]
    [SerializeField, Range(0f, 1f)] private readonly float _dayStartTime01 = 6f / HoursPerDay;
    [SerializeField, Range(0f, 1f)] private readonly float _nightStartTime01 = 18f / HoursPerDay;

    [Header("Brightness (for visuals)")]
    [SerializeField, Range(0f, 1f)] private float _minBrightnessAtMidnight = 0.15f;
    [SerializeField, Range(0f, 1f)] private float _twilightBoost = 0.08f;
    [SerializeField, Range(0f, 1f)] private readonly float _sunrise01 = 0.25f; // ~06:00
    [SerializeField, Range(0f, 1f)] private readonly float _sunset01 = 0.75f;  // ~18:00

    public float Time01 { get; private set; }
    public int CurrentDay { get; private set; }
    public int Hour { get; private set; }
    public int Minute { get; private set; }
    public float Brightness { get; private set; }

    /// <summary>Raised when the minute changes: args(hour, minute).</summary>
    public event Action<int, int> OnMinuteChanged;
    /// <summary>Raised when a new day begins: args(day).</summary>
    public event Action<int> OnDayChanged;
    /// <summary>Raised whenever the brightness value changes: args(brightness).</summary>
    public event Action<float> OnBrightnessChanged;

    private int _prevMinute = -1;
    private float _prevBrightness = -1f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        CurrentDay = Mathf.Max(1, _startDay);
        Time01 = Mathf.Repeat(_initialTime01, 1f);
        RecomputeHhMm();
        RecomputeNightFlag();
        RecomputeBrightness(forceInvoke: true);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void Update()
    {
        if (GameStateManager.IsGamePaused)
            return;

        var dt = Time.deltaTime;
        if (dt <= 0f)
            return;

        Time01 += dt / _secondsPerGameDay;
        if (Time01 >= 1f)
        {
            Time01 -= 1f;
            CurrentDay++;
            OnDayChanged?.Invoke(CurrentDay);
        }

        RecomputeHhMm();
        RecomputeNightFlag();
        RecomputeBrightness(forceInvoke: false);
    }

    private void RecomputeHhMm()
    {
        float totalMinutesF = Time01 * MinutesPerDay;
        int totalMinutes = Mathf.FloorToInt(totalMinutesF);
        int hh = totalMinutes / MinutesPerHour;
        int mm = totalMinutes % MinutesPerHour;

        if (mm != _prevMinute)
        {
            Hour = hh;
            Minute = mm;
            _prevMinute = mm;
            OnMinuteChanged?.Invoke(Hour, Minute);
        }
        else
        {
            Hour = hh;
            Minute = mm;
        }
    }

    private void RecomputeNightFlag()
    {
        bool isNight = Time01 >= _nightStartTime01 || Time01 < _dayStartTime01;
        NightTimeFlag.Set(isNight);
    }

    private void RecomputeBrightness(bool forceInvoke)
    {
        // Cosine daylight profile centered on noon (0.5).
        // cos(0.5) = -1 -> map to 1 at noon; 0 at midnight. Then lerp to keep some moonlight.
        float dayWave = 0.5f + 0.5f * Mathf.Cos((Time01 - 0.5f) * Mathf.PI * 2f); // noon=1, midnight=0
        float baseBrightness = Mathf.Lerp(_minBrightnessAtMidnight, 1f, dayWave);

        // Gentle twilight lift around sunrise/sunset to avoid harsh transitions.
        float twilight = 0f;
        twilight += Gaussian01(Time01, _sunrise01, 0.03f);
        twilight += Gaussian01(Time01, _sunset01, 0.03f);
        float brightness = Mathf.Clamp01(baseBrightness + _twilightBoost * twilight);

        Brightness = brightness;

        if (forceInvoke || !Mathf.Approximately(brightness, _prevBrightness))
        {
            _prevBrightness = brightness;
            OnBrightnessChanged?.Invoke(brightness);
        }
    }

    // Adds a soft bump around sunrise/sunset.
    private static float Gaussian01(float x, float mean, float sigma)
    {
        float d = Mathf.DeltaAngle(x * 360f, mean * 360f) / 180f * Mathf.PI;
        return Mathf.Exp(-(d * d) / (2f * sigma * sigma));
    }

    public float GetHudTimeBarPosition01() => GetHudTimeBarPosition01(Time01);
    public float GetHudTimeBarPosition01(float time01) => Mathf.Repeat(time01 - _dayStartTime01, 1f);

    public string GetTimeString() => $"{Hour:00}:{Minute:00}";
}
