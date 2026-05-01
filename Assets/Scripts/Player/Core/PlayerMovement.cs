using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private Rigidbody2D _body;

    [Header("Movement")]
    [SerializeField] private float _speed = 4f;
    [SerializeField] private float _externalSpeedMultiplier = 1f;
    [SerializeField] private float _itemSpeedMultiplier = 1f;

    private Vector2 _input;

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
        Vector2 delta = direction * distance;
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
}
