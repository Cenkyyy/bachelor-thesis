/// <summary>
/// Runtime holder for one active state and the transition lifecycle around it.
/// </summary>
public sealed class StateMachine
{
    public State CurrentState { get; private set; }

    public bool Set(State newState, bool forceReset = false)
    {
        if (newState == null)
            return false;

        if (CurrentState == newState && !forceReset)
            return false;

        CurrentState?.OnExit();
        CurrentState = newState;
        CurrentState.SetParent(this);
        CurrentState.InitializeState();
        CurrentState.OnEnter();
        return true;
    }
}
