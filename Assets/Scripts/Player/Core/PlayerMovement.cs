using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private Rigidbody2D _body;

    [Header("Movement")]
    [SerializeField] private float _speed = 4f;
    [SerializeField] private float _externalSpeedMultiplier = 1f;
    [SerializeField] private float _itemSpeedMultiplier = 1f;

    [Header("Push Resistance")]
    [SerializeField, Range(0f, 1f)] private float _entityPushMultiplier = 0.2f;
    [SerializeField] private float _entityPushCastExtraDistance = 0.02f;
    [SerializeField] private LayerMask _entityPushTargetMask;

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
        var speed = _speed * Mathf.Max(0f, _externalSpeedMultiplier) * Mathf.Max(0f, _itemSpeedMultiplier);
        var direction = _input.normalized;
        var distance = speed * Time.fixedDeltaTime;
        var speedMultiplier = ShouldReduceEntityPush(direction, distance) ? _entityPushMultiplier : 1f;
        Vector2 delta = direction * (distance * Mathf.Clamp01(speedMultiplier));
        _body.MovePosition(_body.position + delta);
    }

    public void SetExternalSpeedMultiplier(float multiplier)
    {
        _externalSpeedMultiplier = Mathf.Max(0f, multiplier);
    }

    public void SetItemSpeedMultiplier(float multiplier)
    {
        _itemSpeedMultiplier = Mathf.Max(0f, multiplier);
    }

    private bool ShouldReduceEntityPush(Vector2 direction, float distance)
    {
        if (_entityPushMultiplier >= 0.999f || _body == null)
        {
            return false;
        }

        var castDistance = Mathf.Max(0f, distance + _entityPushCastExtraDistance);
        return MovementPushResistanceUtils.ShouldReducePush(_body, direction, castDistance, _movementCastHits, _entityPushTargetMask);
    }
}
