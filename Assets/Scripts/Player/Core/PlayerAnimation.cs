using System;
using UnityEngine;

public class PlayerAnimation : MonoBehaviour
{
    private static readonly int IsWalkingHash = Animator.StringToHash("isWalking");
    private static readonly int InputXHash = Animator.StringToHash("InputX");
    private static readonly int InputYHash = Animator.StringToHash("InputY");
    private static readonly int LastInputXHash = Animator.StringToHash("LastInputX");
    private static readonly int LastInputYHash = Animator.StringToHash("LastInputY");

    [SerializeField] private Animator _animator;
    [SerializeField] private Camera _worldCamera;

    private Vector2 _lastMouseAimedDirection = Vector2.down;
    private bool _isWalking = false;

    public Vector2 LastMouseAimedDirection => _lastMouseAimedDirection;
    public PlayerFacingDirection FacingDirection { get; private set; } = PlayerFacingDirection.Down;

    public event Action<PlayerFacingDirection> OnFacingDirectionChanged;

    private void Awake()
    {
        if (_animator == null)
            _animator = GetComponent<Animator>();

        if (_worldCamera == null)
            _worldCamera = Camera.main;
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
        if (_animator == null || _worldCamera == null)
            return;

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

        _animator.SetBool(IsWalkingHash, _isWalking);

        // update animator parameters for aiming
        _animator.SetFloat(InputXHash, aimDirection.x);
        _animator.SetFloat(InputYHash, aimDirection.y);

        // record the last direction for idle animations
        _animator.SetFloat(LastInputXHash, _lastMouseAimedDirection.x);
        _animator.SetFloat(LastInputYHash, _lastMouseAimedDirection.y);
    }

    private Vector2 GetMouseDirection()
    {
        var mouseWorldPos = _worldCamera.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0f;
        var direction = (mouseWorldPos - transform.position).normalized;
        return direction;
    }
}
