using System.Collections.Generic;
using UnityEngine;

public class EntityData : ScriptableObject
{
    [field: Header("Identity")]
    [field: SerializeField] public BiomeAffinity HomeBiome { get; private set; } = BiomeAffinity.Grassland;

    [field: Header("Spawn")]
    [field: SerializeField] public EntityCore Prefab { get; private set; }
    [field: SerializeField, Min(0f)] public float SpawnWeight { get; private set; } = 1f;

    [field: Header("Core Stats")]
    [field: SerializeField] public int MaxHealth { get; private set; } = 40;
    [field: SerializeField] public int XpReward { get; private set; } = 15;

    [Header("Loot")]
    [SerializeField] private List<EntityLootDrop> _drops = new();
    public IReadOnlyList<EntityLootDrop> Drops => _drops;

    [field: Header("Movement")]
    [field: SerializeField] public float MoveSpeed { get; private set; } = 2.2f;
    [field: SerializeField] public float ArrivalEpsilon { get; private set; } = 0.12f;

    [field: Header("Pathfinding")]
    [field: SerializeField] public float RepathIntervalSeconds { get; private set; } = 0.25f;
    [field: SerializeField] public float PathNodeStep { get; private set; } = 0.5f;
    [field: SerializeField] public int MaxPathIterations { get; private set; } = 1200;

    protected virtual void OnValidate()
    {
        if (SpawnWeight < 0f)
            SpawnWeight = 0f;

        if (MaxHealth < 1)
            MaxHealth = 1;

        if (XpReward < 0)
            XpReward = 0;

        if (MoveSpeed < 0f)
            MoveSpeed = 0f;

        if (ArrivalEpsilon < 0.01f)
            ArrivalEpsilon = 0.01f;

        if (RepathIntervalSeconds < 0.05f)
            RepathIntervalSeconds = 0.05f;

        if (PathNodeStep < 0.1f)
            PathNodeStep = 0.1f;

        if (MaxPathIterations < 100)
            MaxPathIterations = 100;
    }
}
