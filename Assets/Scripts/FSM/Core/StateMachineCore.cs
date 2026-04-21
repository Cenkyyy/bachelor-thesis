using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class StateMachineCore : MonoBehaviour
{
    public StateMachine machine;
    public State initialState;

    private readonly Dictionary<EntityStateId, State> _stateByIdLookup = new();

    protected virtual void Start()
    {
        StartCoroutine(InitializeStateMachineCoroutine());
    }

    private IEnumerator InitializeStateMachineCoroutine()
    {
        yield return null;
        SetUpInstances();
        machine.Set(initialState, forceReset: true);
    }

    protected virtual void Update()
    {
        machine?.state?.DoBranch();
    }

    protected virtual void FixedUpdate()
    {
        machine?.state?.FixedDoBranch();
    }

    public void SetUpInstances()
    {
        machine = new StateMachine();
        _stateByIdLookup.Clear();

        var allChildStates = GetComponentsInChildren<State>(true);
        foreach (var state in allChildStates)
        {
            state.SetCore(this);
            if (state.machine == null)
            {
                state.machine = new StateMachine();
            }

            if (state.StateId == EntityStateId.None)
            {
                continue;
            }

            if (_stateByIdLookup.ContainsKey(state.StateId))
            {
                // Duplicate state IDs are not allowed
                continue;
            }

            _stateByIdLookup.Add(state.StateId, state);
        }
    }

    public bool RequestState(EntityStateId stateId, bool forceReset = false)
    {
        if (machine == null)
            return false;

        if (_stateByIdLookup.TryGetValue(stateId, out var state))
        {
            machine.Set(state, forceReset);
            return true;
        }
        return false;
    }
}
