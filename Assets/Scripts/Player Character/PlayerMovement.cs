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
        SetVelocity();
    }

    private void CheckInput()
    {
        _input.x = Input.GetAxisRaw("Horizontal");
        _input.y = Input.GetAxisRaw("Vertical");

        _input.Normalize();
    }

    private void SetVelocity()
    {
        body.linearVelocity = _input * speed;
    }
}
