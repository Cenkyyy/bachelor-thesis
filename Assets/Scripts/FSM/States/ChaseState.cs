using UnityEngine;

public sealed class ChaseState : State
{
    [SerializeField] private float loseSightGrace = 1.0f;

    [Header("Transitions")]
    [SerializeField] private State _patrolState;

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
