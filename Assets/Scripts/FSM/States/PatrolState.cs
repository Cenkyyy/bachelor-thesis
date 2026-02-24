using UnityEngine;

public sealed class PatrolState : State
{
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
            Set(ActorStateId.Idle, true);
            return;
        }

        _core.MoveTowards(worldPosition);
    }

    public override void Do()
    {
        if (_core.CanSeeTarget(out _))
        {
            _core.PatrolIndex = _index;
            Set(ActorStateId.Chase, true);
        }
    }

    public override void OnExit() => _core.Stop();
}
