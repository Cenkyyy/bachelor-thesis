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

    protected const int PatrolSampleMaxAttempts = 12;

    private Vector2 _homePoint;
    private Vector2 _patrolTarget;
    private bool _hasPatrolTarget;
    private IDamageable _cachedDamageableTarget;
    private Transform _cachedDamageableTransform;
    private EnemyStateMachineController _stateMachineController;

    public new EnemyData Data => _data;
    public override EntityRuntimeData RuntimeData { get; } = new();
    public Vector2 HomePoint => _homePoint;
    protected EnemyAnimationController Animation => _animation;
    protected float PathProbeRadius => _pathProbeRadius;

    protected virtual void Awake()
    {
        SetData(_data);
    }

    protected virtual void Start()
    {
        ApplyDataOverrides();
        ResolveHomePoint();

        RuntimeData.InitializeFrom(Data);

        _stateMachineController = new EnemyStateMachineController(this);
        _stateMachineController.Start();
    }

    protected virtual void Update()
    {
        _stateMachineController.Do();
    }

    protected virtual void FixedUpdate()
    {
        _stateMachineController.FixedDo();
    }

    protected virtual void OnDrawGizmosSelected()
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
}
