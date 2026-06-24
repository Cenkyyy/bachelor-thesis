using UnityEngine;

public sealed class RangedEnemyCore : EnemyCore
{
    [Header("Ranged Combat")]
    [SerializeField] private Transform _projectileSpawnPoint;
    [SerializeField] private Transform _projectileSpawnUp;
    [SerializeField] private Transform _projectileSpawnDown;
    [SerializeField] private Transform _projectileSpawnLeft;
    [SerializeField] private Transform _projectileSpawnRight;
    [SerializeField] private Transform _projectileParent;

    private RangedEnemyData RangedData => (RangedEnemyData)Data;

    public float SampleRangedRepositionDuration()
    {
        return Random.Range(RangedData.RangedRepositionDurationMin, RangedData.RangedRepositionDurationMax);
    }

    public void FaceTargetWhileKiting()
    {
        if (!HasTarget)
            return;

        var directionToTarget = (Vector2)Target.position - (Vector2)transform.position;
        Animation.SetFacingOverride(directionToTarget);
    }

    public bool TryShootProjectileAtCurrentTarget()
    {
        if (RuntimeData.IsDead || !HasTarget)
            return false;

        var directionToTarget = ((Vector2)Target.position - (Vector2)transform.position).normalized;
        var spawnPoint = ResolveDirectionalProjectileSpawn(directionToTarget);
        var origin = spawnPoint != null ? (Vector2)spawnPoint.position : (Vector2)transform.position;
        var direction = ((Vector2)Target.position - origin).normalized;

        var projectileObject = Instantiate(RangedData.ProjectilePrefab, origin, Quaternion.identity, _projectileParent);
        var projectile = projectileObject.GetComponent<EnemyProjectile>();
        projectile.Launch(this, origin, direction, Data.AttackDamage, RangedData.ProjectileSpeed, RangedData.ProjectileLifetimeSeconds);
        return true;
    }

    public void SetProjectileParent(Transform projectileParent)
    {
        _projectileParent = projectileParent;
    }

    public bool IsTargetTooCloseForRanged()
    {
        if (!HasTarget)
            return false;

        var desiredDistance = Mathf.Max(0f, RangedData.PreferredRangedDistance - RangedData.RangedRetreatBuffer);
        var sqrDistance = ((Vector2)Target.position - (Vector2)transform.position).sqrMagnitude;
        return sqrDistance < desiredDistance * desiredDistance;
    }

    public Vector2 GetRangedKitePosition()
    {
        if (!HasTarget)
            return transform.position;

        var away = ((Vector2)transform.position - (Vector2)Target.position).normalized;
        if (away.sqrMagnitude < Mathf.Epsilon)
            away = Random.insideUnitCircle.normalized;

        var desiredDistance = Mathf.Max(Data.AttackRange, RangedData.PreferredRangedDistance);
        return (Vector2)Target.position + away * desiredDistance;
    }

    public Vector2 SampleRangedRepositionTarget()
    {
        var reference = HasTarget ? (Vector2)Target.position : LastKnownTargetPosition;
        var away = ((Vector2)transform.position - reference).normalized;
        if (away.sqrMagnitude < Mathf.Epsilon)
            away = Random.insideUnitCircle.normalized;

        var desiredDistance = RangedData.PreferredRangedDistance <= Data.AttackRange ? RangedData.PreferredRangedDistance : Data.AttackRange;
        var bestPosition = reference + away * desiredDistance;
        var bestDistance = (bestPosition - reference).sqrMagnitude;

        for (var i = 0; i < PatrolSampleMaxAttempts; i++)
        {
            var direction = Quaternion.Euler(0f, 0f, Random.Range(-65f, 65f)) * away;
            var sampled = reference + (Vector2)direction.normalized * desiredDistance;
            if (!CanUsePathPosition(sampled, PathProbeRadius))
                continue;

            var distance = (sampled - reference).sqrMagnitude;
            if (distance <= bestDistance)
                continue;

            bestPosition = sampled;
            bestDistance = distance;
        }

        return bestPosition;
    }

    private Transform ResolveDirectionalProjectileSpawn(Vector2 facingDirection)
    {
        if (facingDirection.sqrMagnitude < Mathf.Epsilon)
            return _projectileSpawnPoint != null ? _projectileSpawnPoint : transform;

        Transform directionalRoot;
        if (Mathf.Abs(facingDirection.x) > Mathf.Abs(facingDirection.y))
        {
            directionalRoot = facingDirection.x >= 0f ? _projectileSpawnRight : _projectileSpawnLeft;
        }
        else
        {
            directionalRoot = facingDirection.y >= 0f ? _projectileSpawnUp : _projectileSpawnDown;
        }

        if (directionalRoot != null)
            return directionalRoot;

        return _projectileSpawnPoint != null ? _projectileSpawnPoint : transform;
    }
}
