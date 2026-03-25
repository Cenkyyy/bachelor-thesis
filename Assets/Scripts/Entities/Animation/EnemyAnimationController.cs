using UnityEngine;

public class EnemyAnimationController : MonoBehaviour
{
    [SerializeField] private Animator _animator;
    [SerializeField] private SpriteRenderer _renderer;

    [SerializeField] private bool _mirrorSideSprites = true;

    [Header("Parameter Names")]
    [SerializeField] private string _isMovingParam = "IsMoving";
    [SerializeField] private string _moveXParam = "MoveX";
    [SerializeField] private string _moveYParam = "MoveY";
    [SerializeField] private string _lastMoveXParam = "LastMoveX";
    [SerializeField] private string _lastMoveYParam = "LastMoveY";
    [SerializeField] private string _attackTriggerParam = "Attack";
    [SerializeField] private string _isDeadParam = "IsDead";

    private Vector2 _lastMoveDirection = Vector2.down;

    private void Awake()
    {
        if (_animator == null)
        {
            _animator = GetComponent<Animator>();
        }

        if (_renderer == null)
        {
            _renderer = GetComponent<SpriteRenderer>();
        }
    }

    public void SetMoving(Vector2 moveDirection)
    {
        var isMoving = moveDirection.sqrMagnitude > 0.0001f;
        if (isMoving)
        {
            _lastMoveDirection = moveDirection.normalized;
        }

        _animator.SetBool(_isMovingParam, isMoving);
        _animator.SetFloat(_moveXParam, moveDirection.x);
        _animator.SetFloat(_moveYParam, moveDirection.y);
        _animator.SetFloat(_lastMoveXParam, _lastMoveDirection.x);
        _animator.SetFloat(_lastMoveYParam, _lastMoveDirection.y);

        UpdateSideFlip(_lastMoveDirection);
    }

    public void TriggerAttack()
    {
        _animator.SetTrigger(_attackTriggerParam);
    }

    public void SetDead(bool isDead)
    {
        _animator.SetBool(_isDeadParam, isDead);
    }

    private void UpdateSideFlip(Vector2 direction)
    {
        if (_renderer == null || !_mirrorSideSprites)
        {
            return;
        }

        if (Mathf.Abs(direction.x) <= Mathf.Abs(direction.y))
        {
            return;
        }

        _renderer.flipX = direction.x < 0f;
    }
}
