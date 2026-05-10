using UnityEngine;

public class EnemyCore : EntityCore
{
    [Header("Enemy")]
    [SerializeField] private EnemyData _data;

    [Header("Home")]
    [SerializeField] private Transform _homePointOverride;

    [Header("Animation")]
    [SerializeField] private EnemyAnimationController _animation;

    [Header("Pathfinding")]
    [SerializeField] private float _pathProbeRadius = 0.18f;

    [Header("Ranged Combat")]
    [SerializeField] private Transform _projectileSpawnPoint;
    [SerializeField] private Transform _projectileSpawnUp;
    [SerializeField] private Transform _projectileSpawnDown;
    [SerializeField] private Transform _projectileSpawnLeft;
    [SerializeField] private Transform _projectileSpawnRight;

    private const int PatrolSampleMaxAttempts = 12;

    private Vector2 _homePoint;
    private Vector2 _patrolTarget;
    private bool _hasPatrolTarget;
    private IDamageable _cachedDamageableTarget;
    private Transform _cachedDamageableTransform;
    private EnemyStateMachineController _stateMachineController;

    public new EnemyData Data => _data;
    public override EntityRuntimeData RuntimeData { get; } = new();
    public Vector2 HomePoint => _homePoint;
    public bool IsRanged => Data.Archetype == EnemyArchetype.Ranged || (Data.RoleTags & EnemyRoleTag.Ranged) != 0;

    private void Awake()
    {
        SetData(_data);
    }

    private void Start()
    {
        ApplyDataOverrides();
        ResolveHomePoint();

        RuntimeData.InitializeFrom(Data);

        _stateMachineController = new EnemyStateMachineController(this);
        _stateMachineController.Start();
    }

    private void Update()
    {
        _stateMachineController.Do();
    }

    private void FixedUpdate()
    {
        _stateMachineController.FixedDo();
    }

    private void OnDrawGizmosSelected()
    {
        var detection = Data.DetectionRadius;
        var leash = Data.LeashRadius;
        var home = Data.HomeRadius;

        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.45f);
        Gizmos.DrawWireSphere(transform.position, detection);

        if (leash > 0f)
        {
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.35f);
            Gizmos.DrawWireSphere(_homePointOverride != null ? _homePointOverride.position : transform.position, leash);
        }

        if (home > 0f)
        {
            Gizmos.color = new Color(0.4f, 1f, 0.3f, 0.35f);
            Gizmos.DrawWireSphere(_homePointOverride != null ? _homePointOverride.position : transform.position, home);
        }
    }

    public override void Initialize(EntityData data)
    {
        if (data is EnemyData enemyData)
            SetData(enemyData);
    }

    public override bool RequestState(StateId stateId, bool forceReset = false)
    {
        return _stateMachineController.RequestState(stateId, forceReset);
    }

    public bool TryDetectTarget()
    {
        return Perception.TryDetectTarget();
    }

    public bool IsTargetInAttackRange()
    {
        if (!HasTarget)
            return false;

        var sqrDistance = ((Vector2)Target.position - (Vector2)transform.position).sqrMagnitude;
        return sqrDistance <= Data.AttackRange * Data.AttackRange;
    }

    public bool CanAttackCurrentTarget()
    {
        return IsTargetInAttackRange() && CanSeeCurrentTarget();
    }

    public bool IsTargetWithinDetectionRadius()
    {
        return Perception.IsTargetWithinDetectionRadius();
    }

    public bool CanSeeCurrentTarget()
    {
        var canSee = CanSeeTarget(out _);
        if (canSee)
            Perception.UpdateLastKnownTargetPosition();

        return canSee;
    }

    public bool IsOutsideLeash()
    {
        if (Data.LeashRadius <= 0f)
            return false;

        var sqrDistance = ((Vector2)transform.position - _homePoint).sqrMagnitude;
        return sqrDistance > Data.LeashRadius * Data.LeashRadius;
    }

    public bool IsInsideHomeRadius()
    {
        if (Data.HomeRadius <= 0f)
            return ArrivedAt(HomePoint);

        var sqrDistance = ((Vector2)transform.position - HomePoint).sqrMagnitude;
        return sqrDistance <= Data.HomeRadius * Data.HomeRadius;
    }

    public bool HasLastKnownTargetPosition => Perception.HasLastKnownTargetPosition;
    public Vector2 LastKnownTargetPosition => Perception.LastKnownTargetPosition;

    public void MoveToUsingPath(Vector2 worldTarget)
    {
        MoveToUsingPath(
            worldTarget: worldTarget,
            pathProbeRadius: _pathProbeRadius,
            repathIntervalSeconds: Data.RepathIntervalSeconds,
            pathNodeStep: Data.PathNodeStep,
            maxPathIterations: Data.MaxPathIterations,
            hasDetourTarget: Perception.HasLastKnownTargetPosition,
            detourTarget: Perception.LastKnownTargetPosition
        );
    }

    public float SampleIdleDuration() => Random.Range(Data.IdleDurationMin, Data.IdleDurationMax);

    public float SampleRangedRepositionDuration()
    {
        return Random.Range(Data.RangedRepositionDurationMin, Data.RangedRepositionDurationMax);
    }

    public float SampleInvestigationDuration()
    {
        return Random.Range(Data.InvestigationDurationMin, Data.InvestigationDurationMax);
    }

    public bool EnsurePatrolTarget()
    {
        if (_hasPatrolTarget)
            return true;

        if (Data.HomeRadius <= 0f)
            return false;

        for (var i = 0; i < PatrolSampleMaxAttempts; i++)
        {
            var sampled = HomePoint + Random.insideUnitCircle * Data.HomeRadius;
            if (!CanUsePathPosition(sampled, _pathProbeRadius))
                continue;

            _patrolTarget = sampled;
            _hasPatrolTarget = true;
            return true;
        }

        return false;
    }

    public Vector2 CurrentPatrolTarget => _patrolTarget;

    public void ClearPatrolTarget()
    {
        _hasPatrolTarget = false;
    }

    public void TriggerAttackAnimation()
    {
        _animation.TriggerAttack();
    }

    public void FaceTargetWhileKiting()
    {
        if (!HasTarget)
            return;

        var directionToTarget = (Vector2)Target.position - (Vector2)transform.position;
        _animation.SetFacingOverride(directionToTarget);
    }

    public void ClearFacingOverride()
    {
        _animation.ClearFacingOverride();
    }

    public void SetRunningAnimation(bool isRunning)
    {
        _animation.SetRunning(isRunning);
    }

    public bool TryDealDamageToCurrentTarget(int amount)
    {
        if (RuntimeData.IsDead || amount <= 0 || !CanAttackCurrentTarget())
            return false;

        if (!TryGetCurrentTargetDamageable(out var damageable) || !damageable.CanReceiveDamage)
            return false;

        damageable.ReceiveDamage(amount, this);
        return true;
    }

    public bool TryShootProjectileAtCurrentTarget()
    {
        if (RuntimeData.IsDead || !CanAttackCurrentTarget() || Data.ProjectilePrefab == null)
            return false;

        var directionToTarget = ((Vector2)Target.position - (Vector2)transform.position).normalized;
        var spawnPoint = ResolveDirectionalProjectileSpawn(directionToTarget);
        var origin = spawnPoint != null ? (Vector2)spawnPoint.position : (Vector2)transform.position;
        var direction = ((Vector2)Target.position - origin).normalized;

        var projectile = Instantiate(_data.ProjectilePrefab, origin, Quaternion.identity);
        projectile.Launch(this, origin, direction, Data.AttackDamage, Data.ProjectileSpeed, Data.ProjectileLifetimeSeconds);
        return true;
    }

    public bool IsTargetTooCloseForRanged()
    {
        if (!HasTarget)
            return false;

        var desiredDistance = Mathf.Max(0f, Data.PreferredRangedDistance - Data.RangedRetreatBuffer);
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

        var desiredDistance = Mathf.Max(Data.AttackRange, Data.PreferredRangedDistance);
        return (Vector2)Target.position + away * desiredDistance;
    }

    public Vector2 SampleRangedRepositionTarget()
    {
        var reference = HasTarget ? (Vector2)Target.position : LastKnownTargetPosition;
        var away = ((Vector2)transform.position - reference).normalized;
        if (away.sqrMagnitude < Mathf.Epsilon)
            away = Random.insideUnitCircle.normalized;

        var desiredDistance = Data.PreferredRangedDistance <= Data.AttackRange ? Data.PreferredRangedDistance : Data.AttackRange;
        var bestPosition = reference + away * desiredDistance;
        var bestDistance = (bestPosition - reference).sqrMagnitude;

        for (var i = 0; i < PatrolSampleMaxAttempts; i++)
        {
            var direction = Quaternion.Euler(0f, 0f, Random.Range(-65f, 65f)) * away;
            var sampled = reference + (Vector2)direction.normalized * desiredDistance;
            if (!CanUsePathPosition(sampled, _pathProbeRadius))
                continue;

            var distance = (sampled - reference).sqrMagnitude;
            if (distance <= bestDistance)
                continue;

            bestPosition = sampled;
            bestDistance = distance;
        }

        return bestPosition;
    }

    public void SetDeadAnimation(bool isDead)
    {
        _animation.SetDead(isDead);
    }

    protected override void HandleMovementDirection(Vector2 desiredDirection)
    {
        _animation.SetMoving(desiredDirection.normalized);
    }

    private void SetData(EnemyData data)
    {
        _data = data;
        base.Data = data;
    }

    private void ApplyDataOverrides()
    {
        Movement.SetMoveSpeed(Data.MoveSpeed);
        Movement.SetArrivalEpsilon(Data.ArrivalEpsilon);
        Perception.SetDetectionRadius(Data.DetectionRadius);
        Perception.SetRetargetInterval(Data.RetargetIntervalSeconds);
    }

    private void ResolveHomePoint()
    {
        _homePoint = _homePointOverride != null ? (Vector2)_homePointOverride.position : (Vector2)transform.position;
    }

    private bool TryGetCurrentTargetDamageable(out IDamageable damageable)
    {
        damageable = null;

        if (!HasTarget)
            return false;

        if (_cachedDamageableTransform == Target && _cachedDamageableTarget != null)
        {
            damageable = _cachedDamageableTarget;
            return true;
        }

        _cachedDamageableTransform = Target;
        _cachedDamageableTarget = Target.GetComponentInParent<IDamageable>();
        damageable = _cachedDamageableTarget;

        return damageable != null;
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
