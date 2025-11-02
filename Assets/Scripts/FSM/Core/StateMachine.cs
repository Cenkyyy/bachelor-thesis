public sealed class StateMachine
{
    public State state { get; private set; }

    public void Set(State newState, bool forceReset = false)
    {
        if (state != newState || forceReset)
        {
            state?.OnExit();
            state = newState;
            state.parent = this;
            state.Initialize();
            state.OnEnter();
        }
    }
}