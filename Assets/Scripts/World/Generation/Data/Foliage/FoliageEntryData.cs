using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "World/Foliage/Foliage Entry")]
public sealed class FoliageEntryData : ScriptableObject
{
    [field: Header("Identity")]
    [field: SerializeField] public string FoliageId { get; private set; }

    [field: Header("References")]
    [field: SerializeField] public GameObject Prefab { get; private set; }

    [Header("Biome Rules")]
    [SerializeField] private List<WorldBiomeType> _allowedBiomes = new List<WorldBiomeType>();
    public IReadOnlyList<WorldBiomeType> AllowedBiomes => _allowedBiomes;

    [field: Header("Spawn Tuning")]
    [field: SerializeField, Min(0f)] public float SpawnWeight { get; private set; } = 1f;
    [field: SerializeField, Min(0f)] public float MinimumSpacingTiles { get; private set; } = 0f;

    [field: Header("Cluster Tuning")]
    [field: SerializeField] public bool AllowCluster { get; private set; } = true;
    [field: SerializeField, Min(1)] public int MinClusterCount { get; private set; } = 2;
    [field: SerializeField, Min(1)] public int MaxClusterCount { get; private set; } = 6;
    [field: SerializeField, Min(0f)] public float ClusterRadiusTiles { get; private set; } = 1.5f;
}
