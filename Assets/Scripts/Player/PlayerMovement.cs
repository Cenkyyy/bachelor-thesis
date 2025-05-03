using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] Rigidbody2D body;
    [SerializeField] Animator animator;

    [SerializeField] float speed;

    private Vector2 _input;

    // Update is called once per frame
    void Update()
    {
        CheckInput();
        UpdateAnimator();
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

    private void UpdateAnimator()
    {
        bool isWalking = _input != Vector2.zero;

        animator.SetBool("isWalking", isWalking);

        // Update current position
        animator.SetFloat("InputX", _input.x);
        animator.SetFloat("InputY", _input.y);

        // Record the last position when the player moved
        if (isWalking)
        {
            animator.SetFloat("LastInputX", _input.x);
            animator.SetFloat("LastInputY", _input.y);
        }
    }
}
