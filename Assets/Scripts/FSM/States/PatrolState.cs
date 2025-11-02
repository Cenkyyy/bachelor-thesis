using UnityEngine;

public sealed class PatrolState : State
{
    [Header("Transitions")]
    [SerializeField] private State _idleState;
    [SerializeField] private State _chaseState;

    private AgentCore _core;
    private int _index;

    public override void OnEnter()
    {
        base.OnEnter();
        _core = (AgentCore)core;
        _index = _core.PatrolIndex;
    }

    public override void FixedDo()
    {
        var worldPosition = (Vector2)_core.PatrolPoints[_index].position;

        if (_core.ArrivedAt(worldPosition))
        {
            _core.Stop();
            _core.PatrolIndex = (_index + 1) % _core.PatrolPoints.Length;
            Set(_idleState, true);
            return;
        }

        _core.MoveTowards(worldPosition);
    }

    public override void Do()
    {
        if (_core.CanSeeTarget(out _))
        {
            _core.PatrolIndex = _index;
            Set(_chaseState, true);
        }
    }

    public override void OnExit() => _core.Stop();
}
