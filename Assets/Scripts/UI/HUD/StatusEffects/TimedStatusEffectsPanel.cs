using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Displays active timed item status effects in the HUD.
/// </summary>
[DisallowMultipleComponent]
public sealed class TimedStatusEffectsPanel : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerStatusEffectController _itemStatController;
    [SerializeField] private Transform _entryParent;
    [SerializeField] private ItemStatusEffectEntryView _entryPrefab;

    [Header("Config")]
    [SerializeField] private List<ItemStatusEffectData> _buffData = new();
    [SerializeField, Min(0.02f)] private float _refreshIntervalSeconds = 0.1f;

    private readonly Dictionary<ItemStatusEffectType, ItemStatusEffectData> _dataByStat = new();
    private readonly Dictionary<ItemStatusEffectType, ItemStatusEffectEntryView> _entryByStat = new();
    private readonly Dictionary<ItemStatusEffectType, TimedBuffVisualState> _newestActiveStatusEffectByStat = new();
    private readonly HashSet<ItemStatusEffectType> _visibleStats = new();
    private Coroutine _refreshCoroutine;

    private readonly struct TimedBuffVisualState
    {
        public float RemainingSeconds { get; }
        public float RemainingNormalized { get; }

        public TimedBuffVisualState(float remainingSeconds, float durationSeconds)
        {
            RemainingSeconds = remainingSeconds;
            RemainingNormalized = Mathf.Clamp01(RemainingSeconds / Mathf.Max(0.0001f, durationSeconds));
        }
    }

    private void Awake()
    {
        if (_itemStatController == null)
            _itemStatController = FindFirstObjectByType<PlayerStatusEffectController>();

        RebuildStatusEffectDataLookup();
    }

    private void OnEnable()
    {
        if (_itemStatController != null)
            _itemStatController.TimedStatusEffectsChanged += HandleTimedStatusEffectsChanged;

        RefreshActiveStatusEffectsView();
        UpdateRefreshRoutineState();
    }

    private void OnDisable()
    {
        if (_itemStatController != null)
            _itemStatController.TimedStatusEffectsChanged -= HandleTimedStatusEffectsChanged;

        StopRefreshRoutine();
    }

    private void HandleTimedStatusEffectsChanged()
    {
        RefreshActiveStatusEffectsView();
        UpdateRefreshRoutineState();
    }

    private IEnumerator RefreshWhileActiveCoroutine()
    {
        var wait = new WaitForSeconds(_refreshIntervalSeconds);

        while (_newestActiveStatusEffectByStat.Count > 0)
        {
            RefreshActiveStatusEffectsView();
            yield return wait;
        }

        _refreshCoroutine = null;
    }

    private void UpdateRefreshRoutineState()
    {
        if (_newestActiveStatusEffectByStat.Count > 0)
        {
            if (_refreshCoroutine == null)
                _refreshCoroutine = StartCoroutine(RefreshWhileActiveCoroutine());

            return;
        }

        StopRefreshRoutine();
    }

    private void StopRefreshRoutine()
    {
        if (_refreshCoroutine == null)
            return;

        StopCoroutine(_refreshCoroutine);
        _refreshCoroutine = null;
    }

    private void RebuildStatusEffectDataLookup()
    {
        _dataByStat.Clear();

        for (int i = 0; i < _buffData.Count; i++)
        {
            var data = _buffData[i];
            if (data == null)
                continue;

            _dataByStat[data.StatType] = data;
        }
    }

    private void RefreshActiveStatusEffectsView()
    {
        RebuildNewestActiveStatusEffectByStat();
        _visibleStats.Clear();

        foreach (var pair in _newestActiveStatusEffectByStat)
        {
            var statType = pair.Key;
            var activeBuff = pair.Value;

            if (!_dataByStat.TryGetValue(statType, out var data) || data == null)
                continue;

            var entry = GetOrCreateEntry(statType);
            if (entry == null)
                continue;

            entry.gameObject.SetActive(true);
            entry.SetVisual(data.Icon, activeBuff.RemainingNormalized);
            _visibleStats.Add(statType);
        }

        foreach (var pair in _entryByStat)
        {
            if (_visibleStats.Contains(pair.Key))
                continue;

            pair.Value.gameObject.SetActive(false);
        }
    }

    private ItemStatusEffectEntryView GetOrCreateEntry(ItemStatusEffectType statType)
    {
        if (_entryByStat.TryGetValue(statType, out var entry) && entry != null)
            return entry;

        if (_entryParent == null || _entryPrefab == null)
            return null;

        entry = Instantiate(_entryPrefab, _entryParent);
        entry.gameObject.SetActive(false);
        _entryByStat[statType] = entry;
        return entry;
    }

    private void RebuildNewestActiveStatusEffectByStat()
    {
        _newestActiveStatusEffectByStat.Clear();

        if (_itemStatController == null)
            return;

        var activeTimedStats = _itemStatController.ActiveTimedStatusEffects;
        if (activeTimedStats == null)
            return;

        float now = Time.time;
        for (int i = 0; i < activeTimedStats.Count; i++)
        {
            var activeTimedStat = activeTimedStats[i];
            if (activeTimedStat == null)
                continue;

            var remaining = activeTimedStat.ExpiresAt - now;
            if (remaining <= 0f)
                continue;

            var statusEffects = activeTimedStat.StatusEffects;
            if (statusEffects == null || statusEffects.Count == 0)
                continue;

            var visualState = new TimedBuffVisualState(remaining, activeTimedStat.DurationSeconds);
            for (int j = 0; j < statusEffects.Count; j++)
            {
                var effectType = statusEffects[j].StatusEffectType;
                if (_newestActiveStatusEffectByStat.TryGetValue(effectType, out var existingState) &&
                    visualState.RemainingSeconds <= existingState.RemainingSeconds)
                    continue;

                _newestActiveStatusEffectByStat[effectType] = visualState;
            }
        }
    }
}
