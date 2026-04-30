using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class MainMenuBackgroundSpriteSpawner : MonoBehaviour
{
    [Serializable]
    private sealed class SpawnEntry
    {
        [field: SerializeField] public Sprite Sprite { get; private set; }
        [field: SerializeField, Min(0f)] public float Weight { get; private set; } = 1f;
        [field: SerializeField] public Vector2 ScaleRange { get; private set; } = new(0.85f, 1.15f);
    }

    [Header("References")]
    [SerializeField] private RectTransform _spawnArea;
    [SerializeField] private RectTransform _instanceParent;

    [Header("Spawn")]
    [SerializeField] private Vector2 _spawnIntervalRange = new(0.08f, 0.15f);
    [SerializeField] private Vector2 _spawnYOffsetRange = new(-24f, 12f);
    [SerializeField] private List<SpawnEntry> _spawnEntries = new();

    [Header("Motion")]
    [SerializeField, Min(0.05f)] private float _riseDurationSeconds = 12f;
    [SerializeField, Range(0f, 1f)] private float _fadeStartPercentageY = 0.5f;
    [SerializeField, Range(0f, 1f)] private float _fadeEndPercentageY = 0.75f;
    [SerializeField] private AnimationCurve _riseCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Visual Variety")]
    [SerializeField] private bool _riseVertically = true;
    [SerializeField] private Vector2 _driftXRange = new(-100f, 100f);
    [SerializeField, Range(0f, 1f)] private float _driftIntensity = 0.2f;

    private float _nextSpawnTime;
    private float _totalWeight;

    private void Awake()
    {
        if (_spawnArea == null)
            _spawnArea = transform as RectTransform;
        
        if (_instanceParent == null)
            _instanceParent = _spawnArea;

        RebuildWeightTable();
        ScheduleNextSpawn();
    }

    private void Update()
    {
        if (_spawnArea == null || _instanceParent == null || _spawnEntries.Count == 0)
            return;

        if (Time.unscaledTime < _nextSpawnTime)
            return;

        SpawnOne();
        ScheduleNextSpawn();
    }

    private void SpawnOne()
    {
        // Choose entry to spawn
        var entry = PickEntry();
        if (entry == null || entry.Sprite == null)
            return;

        // Pick a random spawn posiiton within the spawn area
        var spawnAreaRect = _spawnArea.rect;
        var x = UnityEngine.Random.Range(spawnAreaRect.xMin, spawnAreaRect.xMax);
        var y = spawnAreaRect.yMin + UnityEngine.Random.Range(_spawnYOffsetRange.x, _spawnYOffsetRange.y);

        // Create the instance and set its components up
        var itemRoot = new GameObject($"Symbol_{entry.Sprite.name}", typeof(RectTransform), typeof(Image), typeof(MainMenuBackgroundSpriteSymbol));
        var itemTransform = (RectTransform)itemRoot.transform;
        itemTransform.SetParent(_instanceParent, false);
        itemTransform.anchorMin = new Vector2(0.5f, 0.5f);
        itemTransform.anchorMax = new Vector2(0.5f, 0.5f);
        itemTransform.pivot = new Vector2(0.5f, 0.5f);
        itemTransform.anchoredPosition = new Vector2(x, y);

        var image = itemRoot.GetComponent<Image>();
        image.sprite = entry.Sprite;
        image.SetNativeSize();
        image.raycastTarget = false;

        var scale = UnityEngine.Random.Range(entry.ScaleRange.x, entry.ScaleRange.y);
        itemTransform.localScale = new Vector3(scale, scale, 1f);

        // Determine the end position with some horizontal drift for visuals
        var driftX = UnityEngine.Random.Range(_driftXRange.x, _driftXRange.y) * _driftIntensity;
        var startPosition = itemTransform.anchoredPosition;
        var endY = Mathf.Lerp(spawnAreaRect.yMin, spawnAreaRect.yMax, _fadeEndPercentageY);
        var endX = _riseVertically ? startPosition.x + driftX : UnityEngine.Random.Range(spawnAreaRect.xMin, spawnAreaRect.xMax) + driftX;
        var endPosition = new Vector2(endX, endY);

        // Initialize the symbol component to handle the rising and fading motion
        var symbol = itemRoot.GetComponent<MainMenuBackgroundSpriteSymbol>();
        symbol.Initialize(image, startPosition, endPosition, _riseDurationSeconds, _fadeStartPercentageY, _fadeEndPercentageY, _riseCurve, _spawnArea.rect);
    }

    private SpawnEntry PickEntry()
    {
        if (_totalWeight <= 0f)
            return null;

        // Roll in [0, totalWeight] 
        var roll = UnityEngine.Random.value * _totalWeight;
        var cumulative = 0f;

        // Find the first entry where cumulative weight exceeds the roll
        for (var i = 0; i < _spawnEntries.Count; i++)
        {
            var entry = _spawnEntries[i];
            if (entry == null || entry.Sprite == null || entry.Weight <= 0f)
                continue;

            cumulative += entry.Weight;
            if (roll <= cumulative)
                return entry;
        }

        return null;
    }

    private void RebuildWeightTable()
    {
        _totalWeight = 0f;

        for (var i = 0; i < _spawnEntries.Count; i++)
        {
            var entry = _spawnEntries[i];
            if (entry == null || entry.Sprite == null || entry.Weight <= 0f)
                continue;

            _totalWeight += entry.Weight;
        }
    }

    private void ScheduleNextSpawn()
    {
        var delay = UnityEngine.Random.Range(_spawnIntervalRange.x, _spawnIntervalRange.y);
        _nextSpawnTime = Time.unscaledTime + delay;
    }
}
