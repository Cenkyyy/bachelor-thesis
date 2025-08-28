using UnityEngine;

public class PlayerAnimation : MonoBehaviour
{
    [SerializeField] Animator animator;

    private Vector2 _lastAimDirection = Vector2.down; // last mouse direction
    private bool _isWalking = false;

    private void Awake()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }
    }

    private void Update()
    {
        UpdateAnimator();
    }

    public void SetWalkingState(bool isWalking)
    {
        _isWalking = isWalking;
    }

    private void UpdateAnimator()
    {
        Vector2 aimDirection = GetMouseDirection();

        // update last aim direction only when mouse moves noticeably
        if (aimDirection.magnitude > 0.01f)
            _lastAimDirection = aimDirection;

        animator.SetBool("isWalking", _isWalking);

        // update animator parameters for aiming
        animator.SetFloat("InputX", aimDirection.x);
        animator.SetFloat("InputY", aimDirection.y);

        // record the last direction for idle animations
        animator.SetFloat("LastInputX", _lastAimDirection.x);
        animator.SetFloat("LastInputY", _lastAimDirection.y);
    }

    private Vector2 GetMouseDirection()
    {
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0f;
        Vector2 direction = (mouseWorldPos - transform.position).normalized;
        return direction;
    }
}
