using UnityEngine;

/// <summary>
/// Handles target detection, target tracking, and line-of-sight checks for runtime entities.
/// </summary>
[DisallowMultipleComponent]
public sealed class EntityPerceptionController : MonoBehaviour
{
    [Header("Detection")]
    [SerializeField] private LayerMask _detectionMask;
    [SerializeField] private string _targetTag = "Player";

    [Header("Vision")]
    [SerializeField] private LayerMask _visionBlockerMask;

    private readonly Collider2D[] _detectionResults = new Collider2D[8];
    private readonly RaycastHit2D[] _visionObstacleHits = new RaycastHit2D[16];
    private ContactFilter2D _detectionFilter = new();
    private ContactFilter2D _solidVisionBlockerFilter = new();
    private ContactFilter2D _triggerVisionBlockerFilter = new();
    private Transform _target;
    private float _nextRetargetTime;
    private float _detectionRadius;
    private float _retargetIntervalSeconds;

    public Transform Target => _target;
    public bool HasTarget => _target != null;
    public Vector2 LastKnownTargetPosition { get; private set; }
    public bool HasLastKnownTargetPosition { get; private set; }

    private void Awake()
    {
        ConfigureDetectionFilter();
        ConfigureVisionObstacleFilter();
    }

    public void SetDetectionRadius(float detectionRadius)
    {
        _detectionRadius = detectionRadius;
    }

    public void SetRetargetInterval(float retargetIntervalSeconds)
    {
        _retargetIntervalSeconds = retargetIntervalSeconds;
    }

    public void SetTarget(Transform target)
    {
        _target = target;

        if (_target == null)
            return;

        LastKnownTargetPosition = _target.position;
        HasLastKnownTargetPosition = true;
    }

    public void UpdateLastKnownTargetPosition()
    {
        if (_target == null)
            return;

        LastKnownTargetPosition = _target.position;
        HasLastKnownTargetPosition = true;
    }

    public bool TryDetectTarget()
    {
        if (Time.time < _nextRetargetTime)
            return HasTarget;

        _nextRetargetTime = Time.time + _retargetIntervalSeconds;

        var count = Physics2D.OverlapCircle((Vector2)transform.position, _detectionRadius, _detectionFilter, _detectionResults);
        Transform bestTarget = null;
        var bestDistanceSqr = float.MaxValue;

        for (var i = 0; i < count; i++)
        {
            var collider = _detectionResults[i];
            if (collider == null || !collider.CompareTag(_targetTag))
                continue;

            var distanceSqr = ((Vector2)collider.transform.position - (Vector2)transform.position).sqrMagnitude;
            if (distanceSqr >= bestDistanceSqr)
                continue;

            bestTarget = collider.transform;
            bestDistanceSqr = distanceSqr;
        }

        if (bestTarget == null)
            return HasTarget;

        _target = bestTarget;
        return true;
    }

    public bool CanSeeTarget(out Vector2 directionToTarget)
    {
        if (_target == null)
        {
            directionToTarget = Vector2.zero;
            return false;
        }

        var from = (Vector2)transform.position;
        var to = (Vector2)_target.position;
        var offset = to - from;

        if (offset.sqrMagnitude > _detectionRadius * _detectionRadius)
        {
            directionToTarget = Vector2.zero;
            return false;
        }

        directionToTarget = offset.normalized;
        return !HasVisionBlocker(from, directionToTarget, offset.magnitude, _solidVisionBlockerFilter) &&
               !HasVisionBlocker(from, directionToTarget, offset.magnitude, _triggerVisionBlockerFilter);
    }

    public bool IsTargetWithinDetectionRadius()
    {
        if (!HasTarget)
            return false;

        var distanceSqr = ((Vector2)_target.position - (Vector2)transform.position).sqrMagnitude;
        return distanceSqr <= _detectionRadius * _detectionRadius;
    }

    private bool HasVisionBlocker(Vector2 from, Vector2 directionToTarget, float distance, ContactFilter2D contactFilter)
    {
        var hitCount = Physics2D.Raycast(from, directionToTarget, contactFilter, _visionObstacleHits, distance);
        for (var i = 0; i < hitCount; i++)
        {
            var hitTransform = _visionObstacleHits[i].transform;
            if (hitTransform == null)
                continue;

            if (hitTransform == _target || hitTransform.IsChildOf(_target))
                continue;

            return true;
        }

        return false;
    }

    private void ConfigureDetectionFilter()
    {
        _detectionFilter.useLayerMask = true;
        _detectionFilter.layerMask = _detectionMask;
        _detectionFilter.useTriggers = true;
    }

    private void ConfigureVisionObstacleFilter()
    {
        _solidVisionBlockerFilter.useLayerMask = true;
        _solidVisionBlockerFilter.layerMask = _visionBlockerMask;
        _solidVisionBlockerFilter.useTriggers = false;

        _triggerVisionBlockerFilter.useLayerMask = true;
        _triggerVisionBlockerFilter.layerMask = _visionBlockerMask;
        _triggerVisionBlockerFilter.useTriggers = true;
    }
}
