using UnityEngine;

public class EnemyCore : EntityCore
{
    [Header("Enemy")]
    [SerializeField] private EnemyData _data;

    [Header("Home")]
    [SerializeField] private Transform _homePointOverride;

    [Header("Detection")]
    [SerializeField] private LayerMask _detectionMask;
    [SerializeField] private string _targetTag = "Player";
    [SerializeField] private float _retargetIntervalSeconds = 0.2f;

    [Header("Animation")]
    [SerializeField] private EnemyAnimationController _animation;

    [Header("Pathfinding")]
    [SerializeField] private float _pathProbeRadius = 0.18f;
    [SerializeField] private LayerMask _dynamicObstacleMask;

    private const int PatrolSampleMaxAttempts = 12;
    private readonly Collider2D[] _detectionResults = new Collider2D[8];
    private ContactFilter2D _detectionFilter = new();

    private Vector2 _homePoint;
    private Vector2 _patrolTarget;
    private bool _hasPatrolTarget;
    private float _nextRetargetTime;
    private float _lostSightTimer;
    private Vector2 _lastKnownTargetPosition;
    private bool _hasLastKnownTargetPosition;
    private IDamageable _cachedDamageableTarget;
    private Transform _cachedDamageableTransform;

    public override EntityData Data => _data;
    public override EntityRuntimeData RuntimeData { get; } = new();
    public EnemySpecies Species => _data != null ? _data.Species : EnemySpecies.Troll;
    public EnemyArchetype Archetype => _data != null ? _data.Archetype : EnemyArchetype.Bruiser;
    public EnemyRoleTag RoleTags => _data != null ? _data.RoleTags : EnemyRoleTag.None;
    public BiomeAffinity HomeBiome => _data != null ? _data.HomeBiome : BiomeAffinity.None;
    public Vector2 HomePoint => _homePoint;

    public float LeashRadius => _data != null ? _data.LeashRadius : 0f;
    public int AttackDamage => _data != null ? _data.AttackDamage : 0;
    public float AttackRange => _data != null ? _data.AttackRange : 0f;
    public float LostSightGraceSeconds => _data != null ? _data.LostSightGraceSeconds : 0f;
    public float HomeRadius => _data != null ? _data.HomeRadius : 0f;
    public float IdleDurationMin => _data != null ? _data.IdleDurationMin : 0.5f;
    public float IdleDurationMax => _data != null ? _data.IdleDurationMax : 1f;
    public float AttackWindupSeconds => _data != null ? _data.AttackWindupSeconds : 0f;
    public float AttackHitWindowSeconds => _data != null ? _data.AttackHitWindowSeconds : 0f;
    public float AttackRecoverySeconds => _data != null ? _data.AttackRecoverySeconds : 0f;

    protected override void Start()
    {
        ApplyDataOverrides();
        ResolveHomePoint();

        if (_animation == null)
        {
            _animation = GetComponentInChildren<EnemyAnimationController>();
        }

        if (_data != null)
        {
            RuntimeData.InitializeFrom(_data);
        }

        ConfigureDetectionFilter();
        base.Start();
    }

    public void SetData(EnemyData data)
    {
        if (data != null)
        {
            _data = data;
        }
    }

    public bool TryDetectTarget()
    {
        if (HasTarget)
        {
            _lastKnownTargetPosition = Target.position;
            _hasLastKnownTargetPosition = true;
        }

        if (Time.time < _nextRetargetTime)
        {
            return HasTarget;
        }

        _nextRetargetTime = Time.time + Mathf.Max(0.05f, _retargetIntervalSeconds);

        var count = Physics2D.OverlapCircle((Vector2)transform.position, visionRadius, _detectionFilter, _detectionResults);
        Transform best = null;
        var bestSqr = float.MaxValue;

        for (var i = 0; i < count; i++)
        {
            var col = _detectionResults[i];
            if (col == null || !IsValidTarget(col))
            {
                continue;
            }

            var sqr = ((Vector2)col.transform.position - (Vector2)transform.position).sqrMagnitude;
            if (sqr < bestSqr)
            {
                best = col.transform;
                bestSqr = sqr;
            }
        }

        if (best != null)
        {
            SetTarget(best);
            _lastKnownTargetPosition = best.position;
            _hasLastKnownTargetPosition = true;
            return true;
        }

        return HasTarget;
    }

    public bool IsTargetInAttackRange()
    {
        if (!HasTarget)
        {
            return false;
        }

        var sqrDistance = ((Vector2)Target.position - (Vector2)transform.position).sqrMagnitude;
        return sqrDistance <= AttackRange * AttackRange;
    }

    public bool IsTargetWithinDetectionRadius()
    {
        if (!HasTarget)
        {
            return false;
        }

        var sqrDistance = ((Vector2)Target.position - (Vector2)transform.position).sqrMagnitude;
        return sqrDistance <= visionRadius * visionRadius;
    }

    public bool IsOutsideLeash()
    {
        if (LeashRadius <= 0f)
        {
            return false;
        }

        var sqrDistance = ((Vector2)transform.position - _homePoint).sqrMagnitude;
        return sqrDistance > LeashRadius * LeashRadius;
    }

    public void ResetLostSightTimer()
    {
        _lostSightTimer = 0f;
    }

    public bool TickLostSight(float deltaTime)
    {
        _lostSightTimer += deltaTime;
        return _lostSightTimer > LostSightGraceSeconds;
    }

    public void MoveToUsingPath(Vector2 worldTarget)
    {
        MoveToUsingPath(
                    worldTarget: worldTarget,
                    pathProbeRadius: _pathProbeRadius,
                    repathIntervalSeconds: _data != null ? _data.RepathIntervalSeconds : 0.25f,
                    pathNodeStep: _data != null ? _data.PathNodeStep : 0.5f,
                    maxPathIterations: _data != null ? _data.MaxPathIterations : 1200,
                    dynamicObstacleMask: _dynamicObstacleMask,
                    hasDetourTarget: _hasLastKnownTargetPosition,
                    detourTarget: _lastKnownTargetPosition
                );
    }

    public float SampleIdleDuration() => Random.Range(IdleDurationMin, IdleDurationMax);

    public bool EnsurePatrolTarget()
    {
        if (_hasPatrolTarget)
        {
            return true;
        }

        if (HomeRadius <= 0f)
        {
            return false;
        }

        for (var i = 0; i < PatrolSampleMaxAttempts; i++)
        {
            var sampled = HomePoint + Random.insideUnitCircle * HomeRadius;
            if (Physics2D.OverlapCircle(sampled, _pathProbeRadius, obstacleMask) != null)
            {
                continue;
            }

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
        _animation?.TriggerAttack();
    }

    public void SetRunningAnimation(bool isRunning)
    {
        _animation?.SetRunning(isRunning);
    }

    public bool TryDealDamageToCurrentTarget(int amount)
    {
        if (!HasTarget || amount <= 0 || !IsTargetInAttackRange())
        {
            return false;
        }

        if (!TryGetCurrentTargetDamageable(out var damageable) || !damageable.CanReceiveDamage)
        {
            return false;
        }

        damageable.ReceiveDamage(amount, this);
        return true;
    }

    public void SetDeadAnimation(bool isDead)
    {
        _animation?.SetDead(isDead);
    }

    private void ConfigureDetectionFilter()
    {
        _detectionFilter.useLayerMask = true;
        _detectionFilter.layerMask = _detectionMask;
        _detectionFilter.useTriggers = true;
    }

    private void ApplyDataOverrides()
    {
        if (_data == null)
        {
            return;
        }

        moveSpeed = _data.MoveSpeed;
        arrivalEps = _data.ArrivalEpsilon;
        visionRadius = _data.DetectionRadius;
    }

    private void ResolveHomePoint()
    {
        _homePoint = _homePointOverride != null ? (Vector2)_homePointOverride.position : (Vector2)transform.position;
    }

    protected override void HandleMovementDirection(Vector2 desiredDirection)
    {
        _animation?.SetMoving(desiredDirection.normalized);
    }

    private void OnDrawGizmosSelected()
    {
        var detection = _data != null ? _data.DetectionRadius : visionRadius;
        var leash = _data != null ? _data.LeashRadius : 0f;
        var home = _data != null ? _data.HomeRadius : 0f;

        // Draw detection radius
        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.45f);
        Gizmos.DrawWireSphere(transform.position, detection);

        if (leash > 0f)
        {
            // Draw leash radius
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.35f);
            Gizmos.DrawWireSphere(_homePointOverride != null ? _homePointOverride.position : transform.position, leash);
        }

        if (home > 0f)
        {
            // Draw home radius
            Gizmos.color = new Color(0.4f, 1f, 0.3f, 0.35f);
            Gizmos.DrawWireSphere(_homePointOverride != null ? _homePointOverride.position : transform.position, home);
        }
    }

    private bool IsValidTarget(Collider2D col)
    {
        return col.CompareTag(_targetTag);
    }

    private bool TryGetCurrentTargetDamageable(out IDamageable damageable)
    {
        damageable = null;

        if (!HasTarget)
        {
            return false;
        }

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
