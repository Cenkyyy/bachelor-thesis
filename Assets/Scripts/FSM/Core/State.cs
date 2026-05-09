using UnityEngine;

/// <summary>
/// Base MonoBehaviour for a state in a finite state machine architecture.
/// </summary>
public abstract class State : MonoBehaviour, IState
{
    [field: SerializeField] public StateId StateId { get; private set; } = StateId.None;

    public bool IsComplete { get; protected set; }
    public float StartTime { get; private set; }
    public float TimeInState => Time.time - StartTime;

    protected StateMachineCore core;
    protected float time => TimeInState;

    private readonly StateMachine _childStateMachine = new();
    private StateMachine _parentStateMachine;

    public virtual void OnEnter() { }
    public virtual void Do() { }
    public virtual void FixedDo() { }
    public virtual void OnExit() { }

    internal void Bind(StateMachineCore ownerCore)
    {
        core = ownerCore;
    }

    internal void SetParent(StateMachine parentStateMachine)
    {
        _parentStateMachine = parentStateMachine;
    }

    internal void InitializeState()
    {
        IsComplete = false;
        StartTime = Time.time;
    }

    public void DoBranch()
    {
        Do();
        _childStateMachine.CurrentState?.DoBranch();
    }

    public void FixedDoBranch()
    {
        FixedDo();
        _childStateMachine.CurrentState?.FixedDoBranch();
    }

    /// <summary>
    /// Requests a transition on the owning root state machine by serialized state id.
    /// </summary>
    protected bool Set(StateId newStateId, bool forceReset = false)
    {
        return core != null && core.RequestState(newStateId, forceReset);
    }
}
