using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public abstract class AgentCore : StateMachineCore
{
    [Header("Movement")]
    [SerializeField] protected float moveSpeed = 2.5f;
    [SerializeField] protected float arrivalEps = 0.12f;

    [Header("Vision")]
    [SerializeField] protected float visionRadius = 6f;
    [SerializeField] protected LayerMask obstacleMask;

    protected Rigidbody2D body;
    protected Transform target;

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

        var hit = Physics2D.Raycast(from, d.normalized, d.magnitude, obstacleMask);
        dirToTarget = d.normalized;
        return !hit.collider;
    }

    public void MoveTowards(Vector2 worldTarget)
    {
        var d = worldTarget - (Vector2)transform.position;
        if (d.sqrMagnitude <= arrivalEps * arrivalEps)
        {
            body.linearVelocity = Vector2.zero;
            return;
        }

        body.linearVelocity = d.normalized * moveSpeed;
    }

    public bool ArrivedAt(Vector2 worldTarget) => Vector2.Distance(transform.position, worldTarget) <= arrivalEps;

    public void Stop() => body.linearVelocity = Vector2.zero;
}
