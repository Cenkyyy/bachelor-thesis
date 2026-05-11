using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "EnemyData", menuName = "Game/Entities/Enemy Data")]
public class EnemyData : EntityData
{
    [field: Header("Identity")]
    [field: SerializeField] public ItemBiomeAffinity HomeBiome { get; private set; } = ItemBiomeAffinity.Grassland;
    [field: SerializeField] public EnemySpecies Species { get; private set; } = EnemySpecies.GrasslandTroll;
    [field: SerializeField] public EnemyArchetype Archetype { get; protected set; } = EnemyArchetype.Bruiser;
    [field: SerializeField] public EnemyRoleTag RoleTags { get; protected set; } = EnemyRoleTag.Bruiser;

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
    [field: SerializeField] public float PathNodeStep { get; private set; } = 0.5f;
    [field: SerializeField] public int MaxPathIterations { get; private set; } = 200;

    [field: Header("Perception")]
    [field: SerializeField] public float DetectionRadius { get; private set; } = 6f;
    [field: SerializeField] public float RetargetIntervalSeconds { get; private set; } = 0.2f;
    [field: SerializeField] public float LeashRadius { get; private set; } = 10f;

    [field: Header("Investigation")]
    [field: SerializeField] public float InvestigationDurationMin { get; private set; } = 2.5f;
    [field: SerializeField] public float InvestigationDurationMax { get; private set; } = 4f;

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

        if (RetargetIntervalSeconds < 0f)
            RetargetIntervalSeconds = 0f;

        if (LeashRadius < 0f)
            LeashRadius = 0f;

        if (InvestigationDurationMin < 0f)
            InvestigationDurationMin = 0f;

        if (InvestigationDurationMax < InvestigationDurationMin)
            InvestigationDurationMax = InvestigationDurationMin;

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
    }
}
