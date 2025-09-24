using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private Rigidbody2D _body;
    [SerializeField] private float _speed = 4f;

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
        // calculate delta - the distance to move the frame based on input and speed
        Vector2 delta = _input * _speed * Time.fixedDeltaTime;
        _body.MovePosition(_body.position + delta);
    }
}
