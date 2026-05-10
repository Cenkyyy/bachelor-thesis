using UnityEngine;

/// <summary>
/// Updates enemy animator movement, facing, attack, running, and death parameters.
/// </summary>
[DisallowMultipleComponent]
public sealed class EnemyAnimationController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator _animator;
    [SerializeField] private SpriteRenderer _renderer;

    [Header("Sprites")]
    [SerializeField] private bool _mirrorSideSprites = true;

    [Header("Parameter Names")]
    [SerializeField] private string _isMovingParam = "IsMoving";
    [SerializeField] private string _moveXParam = "MoveX";
    [SerializeField] private string _moveYParam = "MoveY";
    [SerializeField] private string _lastMoveXParam = "LastMoveX";
    [SerializeField] private string _lastMoveYParam = "LastMoveY";
    [SerializeField] private string _isRunningParam = "IsRunning";
    [SerializeField] private string _attackTriggerParam = "Attack";
    [SerializeField] private string _isDeadParam = "IsDead";

    private Vector2 _lastMoveDirection = Vector2.down;
    private bool _hasFacingOverride;
    private Vector2 _facingOverride = Vector2.down;

    public void SetMoving(Vector2 moveDirection)
    {
        var isMoving = moveDirection.sqrMagnitude > Mathf.Epsilon;
        if (isMoving)
            _lastMoveDirection = moveDirection.normalized;

        var facingDirection = _hasFacingOverride ? _facingOverride : _lastMoveDirection;

        _animator.SetBool(_isMovingParam, isMoving);
        _animator.SetFloat(_moveXParam, moveDirection.x);
        _animator.SetFloat(_moveYParam, moveDirection.y);
        _animator.SetFloat(_lastMoveXParam, facingDirection.x);
        _animator.SetFloat(_lastMoveYParam, facingDirection.y);

        UpdateSideFlip(facingDirection);
    }

    public void SetFacingOverride(Vector2 direction)
    {
        if (direction.sqrMagnitude < Mathf.Epsilon)
            return;

        _facingOverride = direction.normalized;
        _hasFacingOverride = true;
    }

    public void ClearFacingOverride()
    {
        _hasFacingOverride = false;
    }

    public void TriggerAttack()
    {
        _animator.SetTrigger(_attackTriggerParam);
    }

    public void SetDead(bool isDead)
    {
        _animator.SetBool(_isDeadParam, isDead);
    }

    public void SetRunning(bool isRunning)
    {
        _animator.SetBool(_isRunningParam, isRunning);
    }

    private void UpdateSideFlip(Vector2 direction)
    {
        if (!_mirrorSideSprites)
            return;

        if (Mathf.Abs(direction.x) <= Mathf.Abs(direction.y))
            return;

        _renderer.flipX = direction.x < 0f;
    }
}
