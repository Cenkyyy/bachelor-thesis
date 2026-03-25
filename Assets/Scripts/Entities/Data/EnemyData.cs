using UnityEngine;

[CreateAssetMenu(menuName = "Entities/Enemy Data")]
public class EnemyData : EntityData
{
    [field: Header("Identity")]
    [field: SerializeField] public EnemySpecies Species { get; private set; } = EnemySpecies.Troll;
    [field: SerializeField] public EnemyArchetype Archetype { get; private set; } = EnemyArchetype.Bruiser;
    [field: SerializeField] public EnemyRoleTag RoleTags { get; private set; } = EnemyRoleTag.Bruiser;

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

    protected override void OnValidate()
    {
        base.OnValidate();

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
    }
}
