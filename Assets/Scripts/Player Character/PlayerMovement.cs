using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] Rigidbody2D body;

    [SerializeField] float speed;

    private Vector2 _input;

    // Update is called once per frame
    void Update()
    {
        CheckInput();
    }

    private void FixedUpdate()
    {
        UpdatePosition();
    }

    private void CheckInput()
    {
        _input.x = Input.GetAxisRaw("Horizontal");
        _input.y = Input.GetAxisRaw("Vertical");

        _input.Normalize();
    }

    private void UpdatePosition()
    {
        // Calculate delta - the distance to move the frame based on input and speed
        Vector2 delta = _input * speed * Time.fixedDeltaTime;
        
        // Add delta to current position of the body
        body.MovePosition(body.position + delta);
    }
}
