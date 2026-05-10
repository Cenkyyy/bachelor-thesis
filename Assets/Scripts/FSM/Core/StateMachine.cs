using System.Collections.Generic;

/// <summary>
/// Runtime holder for registered FSM states and the transition lifecycle around the active state.
/// </summary>
public sealed class StateMachine
{
    private readonly Dictionary<StateId, IState> _statesById = new();

    public StateId CurrentStateId { get; private set; } = StateId.None;
    public IState ActiveState { get; private set; }

    public bool Register(StateId stateId, IState state)
    {
        if (stateId == StateId.None || state == null || _statesById.ContainsKey(stateId))
            return false;

        _statesById.Add(stateId, state);
        return true;
    }

    public bool Set(StateId stateId, bool forceReset = false)
    {
        return _statesById.TryGetValue(stateId, out var state) &&
               Set(stateId, state, forceReset);
    }

    public void Do()
    {
        ActiveState?.Do();
    }

    public void FixedDo()
    {
        ActiveState?.FixedDo();
    }

    /// <summary>
    /// Evaluates a centralized transition table and switches to the first valid target state.
    /// </summary>
    public bool TryTransition(StateTransitionTable transitionTable)
    {
        return transitionTable != null &&
               transitionTable.TryGetNextState(CurrentStateId, out var nextStateId, out var forceReset) &&
               Set(nextStateId, forceReset);
    }

    private bool Set(StateId stateId, IState newState, bool forceReset)
    {
        if (newState == null)
            return false;

        if (ActiveState == newState && !forceReset)
            return false;

        ActiveState?.OnExit();
        ActiveState = newState;
        CurrentStateId = stateId;

        ActiveState.OnEnter();
        return true;
    }
}
