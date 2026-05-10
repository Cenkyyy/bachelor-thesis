using System.Collections.Generic;

/// <summary>
/// Stores ordered FSM transition rules and resolves the first valid transition target.
/// </summary>
public sealed class StateTransitionTable
{
    private readonly List<StateTransition> _transitions = new();

    public void Add(StateId fromStateId, StateId toStateId, StateTransitionCondition condition, bool forceReset = false)
    {
        _transitions.Add(new StateTransition(fromStateId, toStateId, condition, forceReset));
    }

    public void AddAny(StateId toStateId, StateTransitionCondition condition, bool forceReset = false)
    {
        _transitions.Add(StateTransition.FromAnyState(toStateId, condition, forceReset));
    }

    public bool TryGetNextState(StateId currentStateId, out StateId nextStateId, out bool forceReset)
    {
        for (var i = 0; i < _transitions.Count; i++)
        {
            var transition = _transitions[i];
            if (!transition.CanTransitionFrom(currentStateId) || !transition.IsConditionMet())
                continue;

            nextStateId = transition.ToStateId;
            forceReset = transition.ForceReset;
            return true;
        }

        nextStateId = StateId.None;
        forceReset = false;
        return false;
    }
}
