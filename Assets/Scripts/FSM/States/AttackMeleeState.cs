using UnityEngine;

public sealed class AttackMeleeState : State
{
    [Header("Attack")]
    [SerializeField] private int _damage = 10;
    [SerializeField] private float _attackRange = 0.9f;
    [SerializeField] private float _cooldown = 0.8f;

    [Header("Transitions (assign)")]
    [SerializeField] private State _chaseState;
    [SerializeField] private State _patrolState;

    private AgentCore _core;
    private Player _player;
    private float _cooldownTimer;

    public override void OnEnter()
    {
        base.OnEnter();
        _core = (AgentCore)core;
        _player = _core.Target.GetComponent<Player>();
        _cooldownTimer = 0f;
        _core.Stop();
    }

    public override void FixedDo()
    {
        _cooldownTimer -= Time.fixedDeltaTime;

        if (_core.CanSeeTarget(out _))
        {
            // player is within range
            var dist = Vector2.Distance(_core.transform.position, _core.Target.position);
            if (dist <= _attackRange)
            {
                if (_cooldownTimer <= 0f)
                {
                    _player.Data.TakeDamage(_damage);
                    _cooldownTimer = _cooldown;
                }
                _core.Stop();
            }
            else // out of range
            {
                Set(_chaseState, true);
            }
        }
        else
        {
            Set(_patrolState, true);
        }
    }

    public override void OnExit()
    {
        _core.Stop();
    }
}
