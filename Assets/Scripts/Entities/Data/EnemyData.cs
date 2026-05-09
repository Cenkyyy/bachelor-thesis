using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "EnemyData", menuName = "Game/Entities/Enemy Data")]
public class EnemyData : EntityData
{
    [field: Header("Identity")]
    [field: SerializeField] public ItemBiomeAffinity HomeBiome { get; private set; } = ItemBiomeAffinity.Grassland;
    [field: SerializeField] public EnemySpecies Species { get; private set; } = EnemySpecies.GrasslandTroll;
    [field: SerializeField] public EnemyArchetype Archetype { get; private set; } = EnemyArchetype.Bruiser;
    [field: SerializeField] public EnemyRoleTag RoleTags { get; private set; } = EnemyRoleTag.Bruiser;

    [field: Header("Spawn")]
    [field: SerializeField, Min(0f)] public float SpawnWeight { get; private set; } = 1f;

    [field: Header("Rewards")]
    [field: SerializeField] public int XpReward { get; private set; } = 15;

    [Header("Loot")]
    [SerializeField] private List<EntityLootDrop> _drops = new();
    public IReadOnlyList<EntityLootDrop> Drops => _drops;

    [field: Header("Movement")]
    [field: SerializeField] public float MoveSpeed { get; private set; } = 2.2f;
    [field: SerializeField] public float ArrivalEpsilon { get; private set; } = 0.12f;

    [field: Header("Pathfinding")]
    [field: SerializeField] public float RepathIntervalSeconds { get; private set; } = 0.6f;
    [field: SerializeField] public float PathNodeStep { get; private set; } = 1f;
    [field: SerializeField] public int MaxPathIterations { get; private set; } = 200;

    [field: Header("Perception")]
    [field: SerializeField] public float DetectionRadius { get; private set; } = 6f;
    [field: SerializeField] public float LeashRadius { get; private set; } = 10f;
    [field: SerializeField] public float LostSightGraceSeconds { get; private set; } = 1.2f;

    [field: Header("Patrol")]
    [field: SerializeField] public float HomeRadius { get; private set; } = 5f;
    [field: SerializeField] public float IdleDurationMin { get; private set; } = 0.8f;
    [field: SerializeField] public float IdleDurationMax { get; private set; } = 1.6f;

    [field: Header("Combat")]
    [field: SerializeField] public int AttackDamage { get; private set; } = 15;
    [field: SerializeField] public float AttackRange { get; private set; } = 1.2f;
    [field: SerializeField] public float AttackWindupSeconds { get; private set; } = 0.6f;
    [field: SerializeField] public float AttackHitWindowSeconds { get; private set; } = 0.15f;
    [field: SerializeField] public float AttackRecoverySeconds { get; private set; } = 0.8f;

    [field: Header("Ranged Combat")]
    [field: SerializeField] public float PreferredRangedDistance { get; private set; } = 4.5f;
    [field: SerializeField] public float RangedRetreatBuffer { get; private set; } = 1f;
    [field: SerializeField] public EnemyProjectile ProjectilePrefab { get; private set; }
    [field: SerializeField] public float ProjectileSpeed { get; private set; } = 8f;
    [field: SerializeField] public float ProjectileLifetimeSeconds { get; private set; } = 3f;

    protected override void OnValidate()
    {
        base.OnValidate();

        if (SpawnWeight < 0f)
            SpawnWeight = 0f;

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

        if (DetectionRadius < 0f)
            DetectionRadius = 0f;

        if (LeashRadius < 0f)
            LeashRadius = 0f;

        if (LostSightGraceSeconds < 0f)
            LostSightGraceSeconds = 0f;

        if (HomeRadius < 0f)
            HomeRadius = 0f;

        if (IdleDurationMin < 0f)
            IdleDurationMin = 0f;

        if (IdleDurationMax < IdleDurationMin)
            IdleDurationMax = IdleDurationMin;

        if (AttackDamage < 0)
            AttackDamage = 0;

        if (AttackRange < 0f)
            AttackRange = 0f;

        if (AttackWindupSeconds < 0f)
            AttackWindupSeconds = 0f;

        if (AttackHitWindowSeconds < 0f)
            AttackHitWindowSeconds = 0f;

        if (AttackRecoverySeconds < 0f)
            AttackRecoverySeconds = 0f;

        if (PreferredRangedDistance < 0f)
            PreferredRangedDistance = 0f;

        if (RangedRetreatBuffer < 0f)
            RangedRetreatBuffer = 0f;

        if (ProjectileSpeed < 0f)
            ProjectileSpeed = 0f;

        if (ProjectileLifetimeSeconds < 0f)
            ProjectileLifetimeSeconds = 0f;
    }
}
