using UnityEngine;

/// <summary>
/// Applies entity movement requests to the entity Rigidbody2D.
/// </summary>
[DisallowMultipleComponent]
public sealed class EntityMovementController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Rigidbody2D _body;

    private float _moveSpeed;

    public float ArrivalEpsilon { get; private set; }

    public void SetMoveSpeed(float moveSpeed)
    {
        _moveSpeed = moveSpeed;
    }

    public void SetArrivalEpsilon(float arrivalEpsilon)
    {
        ArrivalEpsilon = arrivalEpsilon;
    }

    public bool ArrivedAt(Vector2 worldTarget)
    {
        return Vector2.Distance(transform.position, worldTarget) <= ArrivalEpsilon;
    }

    public void MoveTowards(Vector2 worldTarget)
    {
        var offset = worldTarget - (Vector2)transform.position;
        if (offset.sqrMagnitude <= ArrivalEpsilon * ArrivalEpsilon)
        {
            Stop();
            return;
        }

        _body.linearVelocity = offset.normalized * _moveSpeed;
    }

    public void Stop()
    {
        _body.linearVelocity = Vector2.zero;
    }
}
