using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public abstract class AgentCore : StateMachineCore
{
    [Header("Movement")]
    [SerializeField] protected float moveSpeed = 2.5f;
    [SerializeField] protected float arrivalEps = 0.12f;

    [Header("Push Resistance")]
    [SerializeField, Range(0f, 1f)] protected float actorPushMultiplier = 0.2f;
    [SerializeField] protected float actorPushCastExtraDistance = 0.02f;
    [SerializeField] protected LayerMask actorPushTargetMask;

    [Header("Vision")]
    [SerializeField] protected float visionRadius = 6f;
    [SerializeField] protected LayerMask obstacleMask;

    protected Rigidbody2D body;
    protected Transform target;

    private readonly RaycastHit2D[] _movementCastHits = MovementPushResistanceUtils.CreateCastBuffer();

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
        var speedMultiplier = ShouldReduceActorPush(direction, d.magnitude) ? actorPushMultiplier : 1f;
        body.linearVelocity = direction * (moveSpeed * Mathf.Clamp01(speedMultiplier));
    }

    public bool ArrivedAt(Vector2 worldTarget) => Vector2.Distance(transform.position, worldTarget) <= arrivalEps;

    public void Stop() => body.linearVelocity = Vector2.zero;

    private bool ShouldReduceActorPush(Vector2 direction, float distanceToTarget)
    {
        if (actorPushMultiplier >= 0.999f || body == null)
        {
            return false;
        }

        var castDistance = Mathf.Max(0f, Mathf.Min(distanceToTarget, moveSpeed * Time.fixedDeltaTime) + actorPushCastExtraDistance);
        return MovementPushResistanceUtils.ShouldReducePush(body, direction, castDistance, _movementCastHits, actorPushTargetMask);
    }
}
