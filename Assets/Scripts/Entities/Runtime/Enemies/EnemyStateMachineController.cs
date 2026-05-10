/// <summary>
/// Owns enemy FSM state instances and evaluates shared transition rules.
/// </summary>
public sealed class EnemyStateMachineController
{
    private readonly EnemyCore _enemyCore;
    private readonly StateMachine _stateMachine = new();
    private readonly EnemyStateMachineRuntimeData _runtimeData = new();
    private readonly StateTransitionTable _transitionTable = new();
    private readonly EnemyIdleState _idleState;
    private readonly EnemyPatrolState _patrolState;
    private readonly EnemyChaseState _chaseState;
    private readonly EnemyInvestigateState _investigateState;
    private readonly EnemyAttackState _attackState;
    private readonly EnemyRepositionState _repositionState;
    private readonly EnemyReturnHomeState _returnHomeState;
    private readonly EnemyDeadState _deadState;

    public EnemyStateMachineController(EnemyCore enemyCore)
    {
        _enemyCore = enemyCore;
        _idleState = new EnemyIdleState(enemyCore);
        _patrolState = new EnemyPatrolState(enemyCore);
        _chaseState = new EnemyChaseState(enemyCore);
        _investigateState = new EnemyInvestigateState(enemyCore);
        _attackState = new EnemyAttackState(enemyCore);
        _repositionState = new EnemyRepositionState(enemyCore);
        _returnHomeState = new EnemyReturnHomeState(enemyCore);
        _deadState = new EnemyDeadState(enemyCore);

        RegisterStates();
        ConfigureTransitions();
    }

    public void Start()
    {
        _stateMachine.Set(StateId.Idle, forceReset: true);
    }

    public void Do()
    {
        _stateMachine.Do();
        RefreshRuntimeData();
        _stateMachine.TryTransition(_transitionTable);
    }

    public void FixedDo()
    {
        _stateMachine.FixedDo();
    }

    public bool RequestState(StateId stateId, bool forceReset = false)
    {
        return _stateMachine.Set(stateId, forceReset);
    }

    private void RegisterStates()
    {
        _stateMachine.Register(StateId.Idle, _idleState);
        _stateMachine.Register(StateId.Patrol, _patrolState);
        _stateMachine.Register(StateId.Chase, _chaseState);
        _stateMachine.Register(StateId.Investigate, _investigateState);
        _stateMachine.Register(StateId.Attack, _attackState);
        _stateMachine.Register(StateId.Reposition, _repositionState);
        _stateMachine.Register(StateId.ReturnHome, _returnHomeState);
        _stateMachine.Register(StateId.Dead, _deadState);
    }

    private void ConfigureTransitions()
    {
        _transitionTable.AddAny(StateId.Dead, IsDead);

        _transitionTable.Add(StateId.Idle, StateId.Attack, IsAttackTransitionRequested, forceReset: true);
        _transitionTable.Add(StateId.Idle, StateId.Chase, IsChaseTransitionRequested, forceReset: true);
        _transitionTable.Add(StateId.Idle, StateId.Investigate, IsInvestigateTransitionRequested, forceReset: true);
        _transitionTable.Add(StateId.Idle, StateId.Patrol, () => _idleState.ShouldPatrol, forceReset: true);

        _transitionTable.Add(StateId.Patrol, StateId.Attack, IsAttackTransitionRequested, forceReset: true);
        _transitionTable.Add(StateId.Patrol, StateId.Chase, IsChaseTransitionRequested, forceReset: true);
        _transitionTable.Add(StateId.Patrol, StateId.Investigate, IsInvestigateTransitionRequested, forceReset: true);
        _transitionTable.Add(StateId.Patrol, StateId.Idle, () => _patrolState.ShouldIdle, forceReset: true);

        _transitionTable.Add(StateId.Chase, StateId.Attack, () => _chaseState.ShouldAttack, forceReset: true);
        _transitionTable.Add(StateId.Chase, StateId.Investigate, () => _chaseState.ShouldInvestigate, forceReset: true);

        _transitionTable.Add(StateId.Attack, StateId.Chase, () => _attackState.ShouldChase, forceReset: true);
        _transitionTable.Add(StateId.Attack, StateId.Investigate, () => _attackState.ShouldInvestigate, forceReset: true);
        _transitionTable.Add(StateId.Attack, StateId.Reposition, () => _attackState.ShouldReposition, forceReset: true);
        _transitionTable.Add(StateId.Attack, StateId.Attack, () => _attackState.ShouldRestartAttack, forceReset: true);

        _transitionTable.Add(StateId.Reposition, StateId.Attack, () => _repositionState.ShouldAttack, forceReset: true);
        _transitionTable.Add(StateId.Reposition, StateId.Investigate, () => _repositionState.ShouldInvestigate, forceReset: true);

        _transitionTable.Add(StateId.Investigate, StateId.Attack, () => _investigateState.ShouldAttack, forceReset: true);
        _transitionTable.Add(StateId.Investigate, StateId.Chase, () => _investigateState.ShouldChase, forceReset: true);
        _transitionTable.Add(StateId.Investigate, StateId.Reposition, () => _investigateState.ShouldReposition, forceReset: true);
        _transitionTable.Add(StateId.Investigate, StateId.ReturnHome, () => _investigateState.ShouldReturnHome, forceReset: true);

        _transitionTable.Add(StateId.ReturnHome, StateId.Attack, IsAttackTransitionRequested, forceReset: true);
        _transitionTable.Add(StateId.ReturnHome, StateId.Chase, IsChaseTransitionRequested, forceReset: true);
        _transitionTable.Add(StateId.ReturnHome, StateId.Investigate, IsInvestigateTransitionRequested, forceReset: true);
        _transitionTable.Add(StateId.ReturnHome, StateId.Patrol, () => _returnHomeState.ShouldPatrol, forceReset: true);
    }

    private void RefreshRuntimeData()
    {
        if (_stateMachine.CurrentStateId != StateId.Idle && _stateMachine.CurrentStateId != StateId.Patrol && _stateMachine.CurrentStateId != StateId.ReturnHome)
        {
            _runtimeData.ClearDetectedCombatState();
            return;
        }

        if (!_enemyCore.TryDetectTarget() || !_enemyCore.CanSeeCurrentTarget())
        {
            _runtimeData.ClearDetectedCombatState();
            return;
        }

        var nextStateId = ResolveDetectedCombatState();
        _runtimeData.SetDetectedCombatState(nextStateId);
    }

    private StateId ResolveDetectedCombatState()
    {
        if (_enemyCore.CanAttackCurrentTarget())
            return StateId.Attack;

        return _enemyCore.IsRanged ? StateId.Investigate : StateId.Chase;
    }

    private bool IsDead()
    {
        return _enemyCore.RuntimeData.IsDead;
    }

    private bool IsAttackTransitionRequested()
    {
        return _runtimeData.DetectedCombatStateId == StateId.Attack;
    }

    private bool IsChaseTransitionRequested()
    {
        return _runtimeData.DetectedCombatStateId == StateId.Chase;
    }

    private bool IsInvestigateTransitionRequested()
    {
        return _runtimeData.DetectedCombatStateId == StateId.Investigate;
    }
}
