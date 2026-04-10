using System;
using UnityEngine;

public class PlayerAnimation : MonoBehaviour
{
    [SerializeField] private Animator _animator;

    private Vector2 _lastMouseAimedDirection = Vector2.down;
    private bool _isWalking = false;

    public Vector2 LastMouseAimedDirection => _lastMouseAimedDirection;
    public PlayerFacingDirection FacingDirection { get; private set; } = PlayerFacingDirection.Down;

    public event Action<PlayerFacingDirection> OnFacingDirectionChanged;

    private void Awake()
    {
        if (_animator == null)
        {
            _animator = GetComponent<Animator>();
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
            _lastMouseAimedDirection = aimDirection;

        var facingDirection = PlayerFacingDirectionUtility.FromVector(_lastMouseAimedDirection);
        if (facingDirection != FacingDirection)
        {
            FacingDirection = facingDirection;
            OnFacingDirectionChanged?.Invoke(FacingDirection);
        }

        _animator.SetBool("isWalking", _isWalking);

        // update animator parameters for aiming
        _animator.SetFloat("InputX", aimDirection.x);
        _animator.SetFloat("InputY", aimDirection.y);

        // record the last direction for idle animations
        _animator.SetFloat("LastInputX", _lastMouseAimedDirection.x);
        _animator.SetFloat("LastInputY", _lastMouseAimedDirection.y);
    }

    private Vector2 GetMouseDirection()
    {
        var mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0f;
        var direction = (mouseWorldPos - transform.position).normalized;
        return direction;
    }
}
