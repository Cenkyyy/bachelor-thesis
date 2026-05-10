/// <summary>
/// Stores temporary decision data used while evaluating enemy state transitions.
/// </summary>
public sealed class EnemyStateMachineRuntimeData
{
    public StateId DetectedCombatStateId { get; private set; } = StateId.None;

    public void SetDetectedCombatState(StateId stateId)
    {
        DetectedCombatStateId = stateId;
    }

    public void ClearDetectedCombatState()
    {
        DetectedCombatStateId = StateId.None;
    }
}
