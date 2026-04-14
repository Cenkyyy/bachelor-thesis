using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public abstract class EntityCore : StateMachineCore
{
    [Header("Movement")]
    [SerializeField] protected float moveSpeed = 2.5f;
    [SerializeField] protected float arrivalEps = 0.12f;

    [Header("Push Resistance")]
    [SerializeField, Range(0f, 1f)] protected float entityPushMultiplier = 0.2f;
    [SerializeField] protected float entityPushCastExtraDistance = 0.02f;
    [SerializeField] protected LayerMask entityPushTargetMask;

    [Header("Vision")]
    [SerializeField] protected float visionRadius = 6f;
    [SerializeField] protected LayerMask obstacleMask;

    protected Rigidbody2D body;
    protected Transform target;

    private readonly RaycastHit2D[] _movementCastHits = MovementPushResistanceUtils.CreateCastBuffer();
    private readonly List<Vector2> _currentPath = new();
    private int _pathIndex = -1;
    private float _nextAllowedRepathTime;
    private Vector2 _lastRepathTarget;
    private bool _hasLastRepathTarget;

    public virtual EntityData Data => null;
    public virtual EntityRuntimeData RuntimeData => null;

    protected override void Start()
    {
        body = GetComponent<Rigidbody2D>();
        base.Start();
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    public bool HasTarget => target != null;
    public Transform Target => target;

    public bool CanSeeTarget(out Vector2 dirToTarget)
    {
        if (target == null)
        {
            dirToTarget = Vector2.zero;
            return false;
        }

        var from = (Vector2)transform.position;
        var to = (Vector2)target.position;
        var d = to - from;

        if (d.sqrMagnitude > visionRadius * visionRadius)
        {
            dirToTarget = Vector2.zero;
            return false;
        }

        dirToTarget = d.normalized;
        var hits = Physics2D.RaycastAll(from, d.normalized, d.magnitude, obstacleMask);
        for (var i = 0; i < hits.Length; i++)
        {
            var hitTransform = hits[i].transform;
            if (hitTransform == null)
            {
                continue;
            }

            if (hitTransform == target || hitTransform.IsChildOf(target))
            {
                continue;
            }

            return false;
        }

        return true;
    }

    public void MoveTowards(Vector2 worldTarget)
    {
        var d = worldTarget - (Vector2)transform.position;
        if (d.sqrMagnitude <= arrivalEps * arrivalEps)
        {
            body.linearVelocity = Vector2.zero;
            return;
        }

        var direction = d.normalized;
        var speedMultiplier = ShouldReduceEntityPush(direction, d.magnitude) ? entityPushMultiplier : 1f;
        body.linearVelocity = direction * (moveSpeed * Mathf.Clamp01(speedMultiplier));
    }

    public bool ArrivedAt(Vector2 worldTarget) => Vector2.Distance(transform.position, worldTarget) <= arrivalEps;

    public void Stop() => body.linearVelocity = Vector2.zero;

    public virtual void StopMovement()
    {
        Stop();
        HandleMovementDirection(Vector2.zero);
    }

    protected void MoveToUsingPath(
        Vector2 worldTarget,
        float pathProbeRadius,
        float repathIntervalSeconds,
        float pathNodeStep,
        int maxPathIterations,
        LayerMask dynamicObstacleMask,
        bool hasDetourTarget = false,
        Vector2 detourTarget = default)
    {
        var targetShifted = _hasLastRepathTarget && (worldTarget - _lastRepathTarget).sqrMagnitude > (pathNodeStep * pathNodeStep);
        if (_pathIndex < 0 || !_hasLastRepathTarget || targetShifted || Time.time >= _nextAllowedRepathTime)
        {
            RebuildPath(worldTarget, pathProbeRadius, repathIntervalSeconds, pathNodeStep, maxPathIterations, dynamicObstacleMask);
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
            HandleMovementDirection(nextWaypoint - (Vector2)transform.position);
            return;
        }

        if (!CanMoveDirectlyTo(worldTarget, pathProbeRadius))
        {
            if (hasDetourTarget && TryMoveToDetour(detourTarget, pathProbeRadius))
            {
                return;
            }

            StopMovement();
            return;
        }

        MoveTowards(worldTarget);
        HandleMovementDirection(worldTarget - (Vector2)transform.position);
    }

    protected virtual void HandleMovementDirection(Vector2 desiredDirection)
    {
    }

    private bool ShouldReduceEntityPush(Vector2 direction, float distanceToTarget)
    {
        if (entityPushMultiplier >= 0.999f || body == null)
        {
            return false;
        }

        var castDistance = Mathf.Max(0f, Mathf.Min(distanceToTarget, moveSpeed * Time.fixedDeltaTime) + entityPushCastExtraDistance);
        return MovementPushResistanceUtils.ShouldReducePush(body, direction, castDistance, _movementCastHits, entityPushTargetMask);
    }

    private void RebuildPath(Vector2 worldTarget, float pathProbeRadius, float repathIntervalSeconds, float pathNodeStep, int maxPathIterations, LayerMask dynamicObstacleMask)
    {
        _nextAllowedRepathTime = Time.time + Mathf.Max(0.05f, repathIntervalSeconds);
        _lastRepathTarget = worldTarget;
        _hasLastRepathTarget = true;
        _pathIndex = 0;

        var success = GridAStarPathfinder.TryBuildPath(
            startWorld: transform.position,
            goalWorld: worldTarget,
            nodeStep: Mathf.Max(0.1f, pathNodeStep),
            obstacleMask: obstacleMask,
            probeRadius: Mathf.Max(0.01f, pathProbeRadius),
            maxIterations: Mathf.Max(100, maxPathIterations),
            output: _currentPath,
            additionalBlockedMask: dynamicObstacleMask,
            allowPartialPath: true,
            ignoredRoot: transform
        );

        if (!success)
        {
            _currentPath.Clear();
            _pathIndex = -1;
        }
    }

    private bool CanMoveDirectlyTo(Vector2 worldTarget, float pathProbeRadius)
    {
        var from = (Vector2)transform.position;
        var toTarget = worldTarget - from;
        if (toTarget.sqrMagnitude <= arrivalEps * arrivalEps)
        {
            return true;
        }

        var hit = Physics2D.CircleCast(from, pathProbeRadius, toTarget.normalized, toTarget.magnitude, obstacleMask);
        return hit.collider == null;
    }

    private bool TryMoveToDetour(Vector2 worldTarget, float pathProbeRadius)
    {
        var from = (Vector2)transform.position;
        var toTarget = worldTarget - from;
        if (toTarget.sqrMagnitude <= 0.0001f)
        {
            return false;
        }

        var lateral = Vector2.Perpendicular(toTarget.normalized);
        var probeDistance = Mathf.Max(pathProbeRadius * 3f, arrivalEps * 2f);

        var detourA = from + lateral * probeDistance;
        var detourB = from - lateral * probeDistance;

        var aBlocked = Physics2D.OverlapCircle(detourA, pathProbeRadius, obstacleMask) != null;
        var bBlocked = Physics2D.OverlapCircle(detourB, pathProbeRadius, obstacleMask) != null;

        if (aBlocked && bBlocked)
        {
            return false;
        }

        var aDistance = (detourA - worldTarget).sqrMagnitude;
        var bDistance = (detourB - worldTarget).sqrMagnitude;
        var chooseA = !aBlocked && (bBlocked || aDistance <= bDistance);
        var detour = chooseA ? detourA : detourB;

        MoveTowards(detour);
        HandleMovementDirection(detour - from);
        return true;
    }
}
