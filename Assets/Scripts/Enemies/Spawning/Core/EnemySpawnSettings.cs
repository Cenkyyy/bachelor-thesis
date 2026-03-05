using System;
using UnityEngine;

[Serializable]
public sealed class EnemySpawnSettings
{
    [field: Header("Spawning cycle settings")]
    [field: SerializeField, Min(0.5f)] public float SpawnIntervalSeconds { get; private set; } = 1.5f;
    [field: SerializeField, Min(1)] public int AttemptsPerCycle { get; private set; } = 3;

    [field: Header("Max alive enemies")]
    [field: SerializeField, Min(1)] public int MaxAliveEnemies { get; private set; } = 20;

    [field: Header("Spawning/Despawning radii distances")]
    [field: SerializeField, Min(1f)] public float SpawnRadius { get; private set; } = 16f;
    [field: SerializeField, Min(0f)] public float MinSpawnDistance { get; private set; } = 7f;
    [field: SerializeField, Min(1f)] public float DespawnRadius { get; private set; } = 24f;

    [field: Header("Spawn point validation settings")]
    [field: SerializeField, Min(0f)] public float MinSpacingFromEnemies { get; private set; } = 2f;
    [field: SerializeField, Min(0.05f)] public float WalkableProbeRadius { get; private set; } = 0.2f;
    [field: SerializeField, Min(1)] public int MaxSamplesPerAttempt { get; private set; } = 6;
}
