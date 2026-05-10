/// <summary>
/// Defines a transition between two FSM states and the condition required to use it.
/// </summary>
public sealed class StateTransition
{
    private readonly StateTransitionCondition _condition;

    public StateId FromStateId { get; }
    public StateId ToStateId { get; }
    public bool IsAnyStateTransition { get; }
    public bool ForceReset { get; }

    public StateTransition(StateId fromStateId, StateId toStateId, StateTransitionCondition condition, bool forceReset)
    {
        FromStateId = fromStateId;
        ToStateId = toStateId;
        _condition = condition;
        ForceReset = forceReset;
    }

    private StateTransition(StateId toStateId, StateTransitionCondition condition, bool forceReset)
    {
        ToStateId = toStateId;
        IsAnyStateTransition = true;
        _condition = condition;
        ForceReset = forceReset;
    }

    public static StateTransition FromAnyState(StateId toStateId, StateTransitionCondition condition, bool forceReset)
    {
        return new StateTransition(toStateId, condition, forceReset);
    }

    public bool CanTransitionFrom(StateId currentStateId)
    {
        return IsAnyStateTransition || FromStateId == currentStateId;
    }

    public bool IsConditionMet()
    {
        return _condition != null && _condition();
    }
}
