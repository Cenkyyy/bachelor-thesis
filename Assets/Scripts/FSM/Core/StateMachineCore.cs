
using UnityEngine;

public abstract class StateMachineCore : MonoBehaviour
{
    public StateMachine machine;
    public State initialState;

    protected virtual void Start()
    {
        SetUpInstances();
        machine.Set(initialState, forceReset: true);
    }

    protected virtual void Update()
    {
        machine.state?.DoBranch();
    }

    protected virtual void FixedUpdate()
    {
        machine.state?.FixedDoBranch();
    }

    public void SetUpInstances()
    {
        machine = new StateMachine();

        var allChildStates = GetComponentsInChildren<State>(true);
        foreach (var state in allChildStates)
        {
            state.SetCore(this);
            if (state.machine == null)
            {
                state.machine = new StateMachine();
            }
        }
    }
}