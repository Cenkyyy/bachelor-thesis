using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private Rigidbody2D _body;

    [Header("Movement")]
    [SerializeField] private float _speed = 4f;
    [SerializeField] private float _externalSpeedMultiplier = 1f;

    [Header("Push Resistance")]
    [SerializeField, Range(0f, 1f)] private float _actorPushMultiplier = 0.2f;
    [SerializeField] private float _actorPushCastExtraDistance = 0.02f;
    [SerializeField] private LayerMask _actorPushTargetMask;

    private Vector2 _input;
    private readonly RaycastHit2D[] _movementCastHits = MovementPushResistanceUtils.CreateCastBuffer();

    private void Awake()
    {
        if (_body == null)
        {
            _body = GetComponent<Rigidbody2D>();
        }
    }

    private void FixedUpdate()
    {
        UpdatePosition();
    }

    public void SetMovementInput(Vector2 input)
    {
        _input = input;
    }

    private void UpdatePosition()
    {
        if (_input.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        // calculate delta - the distance to move the frame based on input and speed
        var speed = _speed * Mathf.Max(0f, _externalSpeedMultiplier);
        var direction = _input.normalized;
        var distance = speed * Time.fixedDeltaTime;
        var speedMultiplier = ShouldReduceActorPush(direction, distance) ? _actorPushMultiplier : 1f;
        Vector2 delta = direction * (distance * Mathf.Clamp01(speedMultiplier));
        _body.MovePosition(_body.position + delta);
    }

    public void SetExternalSpeedMultiplier(float multiplier)
    {
        _externalSpeedMultiplier = Mathf.Max(0f, multiplier);
    }

    private bool ShouldReduceActorPush(Vector2 direction, float distance)
    {
        if (_actorPushMultiplier >= 0.999f || _body == null)
        {
            return false;
        }

        var castDistance = Mathf.Max(0f, distance + _actorPushCastExtraDistance);
        return MovementPushResistanceUtils.ShouldReducePush(_body, direction, castDistance, _movementCastHits, _actorPushTargetMask);
    }
}
