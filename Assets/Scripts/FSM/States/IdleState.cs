using UnityEngine;

public sealed class IdleState : State
{
    [SerializeField] private float _minIdle = 0.7f;
    [SerializeField] private float _maxIdle = 2.0f;

    [Header("Transitions")]
    [SerializeField] private State _patrolState;
    [SerializeField] private State _chaseState;

    private AgentCore _core;
    private float _wait;

    public override void OnEnter()
    {
        base.OnEnter();
        _core = (AgentCore)core;
        _core.Stop();
        _wait = Random.Range(_minIdle, _maxIdle);
    }

    public override void Do()
    {
        if (_core.CanSeeTarget(out _))
        { 
            Set(_chaseState, true);
            return;
        }

        _wait -= Time.deltaTime;
        if (_wait <= 0f)
        {
            Set(_patrolState, true);
        }
    }

    public override void OnExit() => _core.Stop();
}
