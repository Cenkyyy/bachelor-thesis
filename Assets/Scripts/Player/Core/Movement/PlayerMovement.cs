using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] Rigidbody2D body;
    [SerializeField] float speed = 4f;

    private Vector2 _input;

    private void Awake()
    {
        if (body == null)
        {
            body = GetComponent<Rigidbody2D>();
        }
    }

    private void FixedUpdate()
    {
        UpdatePosition();
    }

    public void SetInput(Vector2 input)
    {
        _input = input;
    }

    private void UpdatePosition()
    {
        // calculate delta - the distance to move the frame based on input and speed
        Vector2 delta = _input * speed * Time.fixedDeltaTime;
        body.MovePosition(body.position + delta);
    }
}
