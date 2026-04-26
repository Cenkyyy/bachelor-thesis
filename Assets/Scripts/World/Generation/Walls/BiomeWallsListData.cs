using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "World/Walls/Biome Walls List Data")]
public sealed class BiomeWallsListData : ScriptableObject
{
    [field: Header("Biome")]
    [field: SerializeField] public BiomeType Biome { get; private set; }

    [Header("Walls")]
    [SerializeField] private List<WallData> _walls = new();
    public IReadOnlyList<WallData> Walls => _walls;

    [field: Header("Cluster Shape")]
    [field: SerializeField, Min(1)] public int MinClusterSizeTiles { get; private set; } = 20;
    [field: SerializeField, Min(1)] public int MaxClusterSizeTiles { get; private set; } = 50;
    [field: SerializeField, Range(0f, 1f)] public float BranchingChance { get; private set; } = 0.35f;
    [field: SerializeField, Range(0f, 1f)] public float ExpansionChance { get; private set; } = 0.8f;

    [field: Header("Cluster Ores")]
    [field: SerializeField, Range(0f, 1f)] public float OreClusterSpawnChance { get; private set; } = 0.3f;
    [field: SerializeField, Min(0)] public int MinOresPerCluster { get; private set; }
    [field: SerializeField, Min(0)] public int MaxOresPerCluster { get; private set; } = 2;

    private void OnValidate()
    {
        if (MinClusterSizeTiles < 1)
            MinClusterSizeTiles = 1;

        if (MaxClusterSizeTiles < MinClusterSizeTiles)
            MaxClusterSizeTiles = MinClusterSizeTiles;

        BranchingChance = Mathf.Clamp01(BranchingChance);
        ExpansionChance = Mathf.Clamp01(ExpansionChance);
        OreClusterSpawnChance = Mathf.Clamp01(OreClusterSpawnChance);

        if (MinOresPerCluster < 0)
            MinOresPerCluster = 0;

        if (MaxOresPerCluster < MinOresPerCluster)
            MaxOresPerCluster = MinOresPerCluster;
    }
}
