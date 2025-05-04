using UnityEngine;

public class PlayerAnimation : MonoBehaviour
{
    [SerializeField] Animator animator;

    private Vector2 _input;

    public void SetInput(Vector2 input)
    {
        _input = input;
    }

    void Update()
    {
        UpdateAnimator();
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
