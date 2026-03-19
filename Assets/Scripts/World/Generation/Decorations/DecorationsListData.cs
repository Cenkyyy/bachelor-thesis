using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "World/Decorations/Decorations List Data")]
public sealed class DecorationsListData : ScriptableObject
{
    [SerializeField] private List<DecorationListEntry> _entries = new List<DecorationListEntry>();

    public IReadOnlyList<DecorationListEntry> Entries => _entries;
}

[Serializable]
public sealed class DecorationListEntry
{
    [field: Header("Identity")]
    [field: SerializeField] public string DecorationId { get; private set; }

    [field: Header("References")]
    [field: SerializeField] public GameObject Prefab { get; private set; }
    [field: SerializeField] public MineableNodeData NodeData { get; private set; }

    [field: Header("Classification")]
    [field: SerializeField] public DecorationCategory Category { get; private set; } = DecorationCategory.Tree;
    [field: SerializeField] public DecorationSize Size { get; private set; } = DecorationSize.Small;

    [Header("Biome Rules")]
    [SerializeField] private List<BiomeType> _allowedBiomes = new List<BiomeType>();
    public IReadOnlyList<BiomeType> AllowedBiomes => _allowedBiomes;

    [field: Header("Spawn Tuning")]
    [field: SerializeField, Min(0f)] public float SpawnWeight { get; private set; } = 1f;
    [field: SerializeField, Min(0.1f)] public float MinimumSpacingTiles { get; private set; } = 1f;

    [field: Header("Cluster Tuning")]
    [field: SerializeField] public bool AllowCluster { get; private set; }
    [field: SerializeField, Min(1)] public int MinClusterCount { get; private set; } = 1;
    [field: SerializeField, Min(1)] public int MaxClusterCount { get; private set; } = 1;
    [field: SerializeField, Min(0f)] public float ClusterRadiusTiles { get; private set; }
}
