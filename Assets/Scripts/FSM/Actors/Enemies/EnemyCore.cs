using System.Collections.Generic;
using UnityEngine;

public class EnemyCore : AgentCore
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

    private const int PatrolSampleMaxAttempts = 12;
    private readonly List<Vector2> _currentPath = new();
    private readonly Collider2D[] _detectionResults = new Collider2D[8];
    private ContactFilter2D _detectionFilter = new();

    private Vector2 _homePoint;
    private Vector2 _patrolTarget;
    private bool _hasPatrolTarget;
    private int _pathIndex;
    private float _nextAllowedRepathTime;
    private float _nextRetargetTime;
    private float _lostSightTimer;

    public EnemyData Data => _data;
    public EnemySpecies Species => _data != null ? _data.Species : EnemySpecies.Troll;
    public EnemyArchetype Archetype => _data != null ? _data.Archetype : EnemyArchetype.Bruiser;
    public EnemyRoleTag RoleTags => _data != null ? _data.RoleTags : EnemyRoleTag.None;
    public BiomeAffinity HomeBiome => _data != null ? _data.HomeBiome : BiomeAffinity.None;
    public Vector2 HomePoint => _homePoint;

    public float LeashRadius => _data != null ? _data.LeashRadius : 0f;
    public float AttackRange => _data != null ? _data.AttackRange : 0f;
    public float LostSightGraceSeconds => _data != null ? _data.LostSightGraceSeconds : 0f;
    public float HomeRadius => _data != null ? _data.HomeRadius : 0f;
    public float IdleDurationMin => _data != null ? _data.IdleDurationMin : 0.5f;
    public float IdleDurationMax => _data != null ? _data.IdleDurationMax : 1f;
    public float AttackWindupSeconds => _data != null ? _data.AttackWindupSeconds : 0f;
    public float AttackRecoverySeconds => _data != null ? _data.AttackRecoverySeconds : 0f;

    protected override void Start()
    {
        ApplyDefinitionOverrides();
        ResolveHomePoint();

        if (_animation == null)
        {
            _animation = GetComponentInChildren<EnemyAnimationController>();
        }

        ConfigureDetectionFilter();
        base.Start();
    }

    public bool TryDetectTarget()
    {
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

        SetTarget(best);
        return best != null;
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
        if (Time.time >= _nextAllowedRepathTime)
        {
            RebuildPath(worldTarget);
        }

        if (_pathIndex >= 0 && _pathIndex < _currentPath.Count)
        {
            var waypoint = _currentPath[_pathIndex];
            if (ArrivedAt(waypoint))
            {
                _pathIndex++;
            }
        }

        if (_pathIndex >= 0 && _pathIndex < _currentPath.Count)
        {
            var nextWaypoint = _currentPath[_pathIndex];
            MoveTowards(nextWaypoint);
            SetAnimationMotion(nextWaypoint - (Vector2)transform.position);
            return;
        }

        MoveTowards(worldTarget);
        SetAnimationMotion(worldTarget - (Vector2)transform.position);
    }

    public void StopMovement()
    {
        Stop();
        SetAnimationMotion(Vector2.zero);
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

    private void ApplyDefinitionOverrides()
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

    private void RebuildPath(Vector2 worldTarget)
    {
        if (_data == null)
        {
            _currentPath.Clear();
            _pathIndex = -1;
            _nextAllowedRepathTime = Time.time + 0.25f;
            return;
        }

        _nextAllowedRepathTime = Time.time + _data.RepathIntervalSeconds;
        _pathIndex = 0;

        var success = GridAStarPathfinder.TryBuildPath(
            startWorld: transform.position,
            goalWorld: worldTarget,
            nodeStep: _data.PathNodeStep,
            obstacleMask: obstacleMask,
            probeRadius: _pathProbeRadius,
            maxIterations: _data.MaxPathIterations,
            output: _currentPath
        );

        if (!success)
        {
            _currentPath.Clear();
            _pathIndex = -1;
        }
    }

    private void SetAnimationMotion(Vector2 desiredDirection)
    {
        _animation?.SetMoving(desiredDirection.normalized);
    }

    private bool IsValidTarget(Collider2D col)
    {
        return col.CompareTag(_targetTag);
    }
}
