using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Hosts a finite state machine for a prefab by binding child State components and driving the active state each frame.
/// </summary>
public abstract class StateMachineCore : MonoBehaviour
{
    [Header("State Machine")]
    [SerializeField] private State _initialState;

    private readonly Dictionary<StateId, State> _stateByIdLookup = new();
    private StateMachine _stateMachine;

    public State CurrentState => _stateMachine?.CurrentState;

    protected virtual void Start()
    {
        StartCoroutine(InitializeStateMachineCoroutine());
    }

    protected virtual void Update()
    {
        CurrentState?.DoBranch();
    }

    protected virtual void FixedUpdate()
    {
        CurrentState?.FixedDoBranch();
    }

    /// <summary>
    /// Requests a transition to a state registered on this state machine by its serialized state id.
    /// </summary>
    public bool RequestState(StateId stateId, bool forceReset = false)
    {
        if (_stateMachine == null)
            return false;

        return _stateByIdLookup.TryGetValue(stateId, out var state) &&
               _stateMachine.Set(state, forceReset);
    }

    private IEnumerator InitializeStateMachineCoroutine()
    {
        yield return null;

        SetUpStateInstances();
        _stateMachine.Set(_initialState, forceReset: true);
    }

    private void SetUpStateInstances()
    {
        _stateMachine = new StateMachine();
        _stateByIdLookup.Clear();

        var childStates = GetComponentsInChildren<State>(true);
        for (var i = 0; i < childStates.Length; i++)
        {
            var state = childStates[i];
            state.Bind(this);

            if (state.StateId == StateId.None)
                continue;

            if (_stateByIdLookup.ContainsKey(state.StateId))
                continue;

            _stateByIdLookup.Add(state.StateId, state);
        }
    }
}
