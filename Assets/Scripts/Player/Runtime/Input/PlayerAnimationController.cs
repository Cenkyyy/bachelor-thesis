using System;
using UnityEngine;

/// <summary>
/// Updates animator movement and facing parameters from player movement state and mouse aim.
/// </summary>
[DisallowMultipleComponent]
public sealed class PlayerAnimationController : MonoBehaviour
{
    private static readonly int IsWalkingHash = Animator.StringToHash("isWalking");
    private static readonly int InputXHash = Animator.StringToHash("InputX");
    private static readonly int InputYHash = Animator.StringToHash("InputY");
    private static readonly int LastInputXHash = Animator.StringToHash("LastInputX");
    private static readonly int LastInputYHash = Animator.StringToHash("LastInputY");

    [Header("References")]
    [SerializeField] private Animator _animator;
    [SerializeField] private Camera _worldCamera;

    private Vector2 _lastMouseAimedDirection = Vector2.down;
    private bool _isWalking;

    public Vector2 LastMouseAimedDirection => _lastMouseAimedDirection;
    public PlayerFacingDirection FacingDirection { get; private set; } = PlayerFacingDirection.Down;

    public event Action<PlayerFacingDirection> OnFacingDirectionChanged;

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
        var aimDirection = GetMouseDirection();

        if (aimDirection.sqrMagnitude > Mathf.Epsilon)
            _lastMouseAimedDirection = aimDirection;

        var facingDirection = PlayerFacingDirectionUtility.FromVector(_lastMouseAimedDirection);
        if (facingDirection != FacingDirection)
        {
            FacingDirection = facingDirection;
            OnFacingDirectionChanged?.Invoke(FacingDirection);
        }

        var animatorDirection = PlayerFacingDirectionUtility.ToVector(facingDirection);

        _animator.SetBool(IsWalkingHash, _isWalking);
        _animator.SetFloat(InputXHash, animatorDirection.x);
        _animator.SetFloat(InputYHash, animatorDirection.y);
        _animator.SetFloat(LastInputXHash, animatorDirection.x);
        _animator.SetFloat(LastInputYHash, animatorDirection.y);
    }

    private Vector2 GetMouseDirection()
    {
        var mouseWorldPosition = _worldCamera.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPosition.z = 0f;

        return (mouseWorldPosition - transform.position).normalized;
    }
}
