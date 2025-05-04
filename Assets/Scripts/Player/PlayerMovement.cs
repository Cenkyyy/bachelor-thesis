using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] Rigidbody2D body;
    [SerializeField] float speed;

    private Vector2 _input;

    public void SetInput(Vector2 input)
    {
        _input = input;
    }

    private void FixedUpdate()
    {
        UpdatePosition();
    }

    private void UpdatePosition()
    {
        // Calculate delta - the distance to move the frame based on input and speed
        Vector2 delta = _input * speed * Time.fixedDeltaTime;
        body.MovePosition(body.position + delta);
    }
}
