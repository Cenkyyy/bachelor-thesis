using UnityEngine;

/// <summary>
/// Enemy data for enemies that attack with projectiles and reposition around their target.
/// </summary>
[CreateAssetMenu(fileName = "RangedEnemyData", menuName = "Game/Entities/Ranged Enemy Data")]
public sealed class RangedEnemyData : EnemyData
{
    [field: Header("Ranged Combat")]
    [field: SerializeField] public float PreferredRangedDistance { get; private set; } = 4.5f;
    [field: SerializeField] public float RangedRetreatBuffer { get; private set; } = 1f;
    [field: SerializeField] public float RangedRepositionDurationMin { get; private set; } = 0.6f;
    [field: SerializeField] public float RangedRepositionDurationMax { get; private set; } = 1.1f;
    [field: SerializeField] public GameObject ProjectilePrefab { get; private set; }
    [field: SerializeField] public float ProjectileSpeed { get; private set; } = 8f;
    [field: SerializeField] public float ProjectileLifetimeSeconds { get; private set; } = 3f;

    protected override void OnValidate()
    {
        base.OnValidate();

        Archetype = EnemyArchetype.Ranged;
        RoleTags = EnemyRoleTag.Ranged;

        if (PreferredRangedDistance < 0f)
            PreferredRangedDistance = 0f;

        if (RangedRetreatBuffer < 0f)
            RangedRetreatBuffer = 0f;

        if (RangedRepositionDurationMin < 0f)
            RangedRepositionDurationMin = 0f;

        if (RangedRepositionDurationMax < RangedRepositionDurationMin)
            RangedRepositionDurationMax = RangedRepositionDurationMin;

        if (ProjectileSpeed < 0f)
            ProjectileSpeed = 0f;

        if (ProjectileLifetimeSeconds < 0f)
            ProjectileLifetimeSeconds = 0f;
    }
}
