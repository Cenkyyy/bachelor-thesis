using UnityEngine;

public sealed class ChaseState : State
{
    [SerializeField] private float loseSightGrace = 1.0f;

    [Header("Transitions")]
    [SerializeField] private State _patrolState;
    [SerializeField] private State _attackState;

    [SerializeField] private float _attackTriggerRange = 0.9f;

    private AgentCore _core;
    private float _lostTimer;

    public override void OnEnter()
    {
        base.OnEnter();
        _core = (AgentCore)core;
        _lostTimer = 0f;
    }

    public override void FixedDo()
    {
        if (_core.CanSeeTarget(out _))
        {
            if (_attackState != null)
            {
                var dist = Vector2.Distance(_core.transform.position, _core.Target.position);
                if (dist <= _attackTriggerRange)
                {
                    Set(_attackState, true);
                    return;
                }
            }

            _lostTimer = 0f;
            _core.MoveTowards(_core.Target.position);
        }
        else
        {
            _lostTimer += Time.fixedDeltaTime;
            if (_lostTimer >= loseSightGrace)
            {
                Set(_patrolState, true);
                return;
            }
            _core.Stop();
        }
    }

    public override void OnExit() => _core.Stop();
}
