using UnityEngine;

/// <summary>
/// Applies externally provided movement input to the player's Rigidbody2D.
/// </summary>
[DisallowMultipleComponent]
public sealed class PlayerMovement : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Rigidbody2D _body;

    [Header("Movement")]
    [SerializeField] private float _speed = 4f;
    [SerializeField] private float _externalSpeedMultiplier = 1f;
    [SerializeField] private float _itemSpeedMultiplier = 1f;

    private Vector2 _input;

    private void FixedUpdate()
    {
        UpdatePosition();
    }

    public void SetMovementInput(Vector2 input)
    {
        _input = input;
    }

    public void SetExternalSpeedMultiplier(float multiplier)
    {
        _externalSpeedMultiplier = multiplier;
    }

    public void SetItemSpeedMultiplier(float multiplier)
    {
        _itemSpeedMultiplier = multiplier;
    }

    private void UpdatePosition()
    {
        if (_input.sqrMagnitude <= Mathf.Epsilon)
            return;

        var speed = _speed * _externalSpeedMultiplier * _itemSpeedMultiplier;
        var direction = _input.normalized;
        var distance = speed * Time.fixedDeltaTime;
        var delta = direction * distance;

        _body.MovePosition(_body.position + delta);
    }
}
