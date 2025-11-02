using UnityEngine;

public abstract class State : MonoBehaviour
{
    public bool IsComplete { get; protected set; } = false;

    public float startTime { get; private set; }
    public float time => Time.time - startTime;

    public StateMachine machine;
    public StateMachine parent;

    protected StateMachineCore core;

    public virtual void OnEnter() { }
    public virtual void Do() { }
    public virtual void FixedDo() { }
    public virtual void OnExit() { }
    
    protected void Set(State newState, bool forceReset = false)
    {
        parent.Set(newState, forceReset);
    }

    public void SetCore(StateMachineCore core)
    {
        this.core = core;
        if (machine == null)
        {
            machine = new StateMachine();
        }
    }

    public void DoBranch()
    {
        Do();
        machine?.state?.DoBranch();
    }

    public void FixedDoBranch()
    {
        FixedDo();
        machine?.state?.FixedDoBranch();
    }

    public void Initialize()
    {
        IsComplete = false;
        startTime = Time.time;
    }
}