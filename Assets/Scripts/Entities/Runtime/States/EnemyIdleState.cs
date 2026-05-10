/// <summary>
/// Holds an enemy in place before it returns to patrol.
/// </summary>
public class EnemyIdleState : EnemyStateBase
{
    private float _idleDuration;

    public EnemyIdleState(EnemyCore enemyCore) : base(enemyCore)
    {
    }

    public bool ShouldPatrol => TimeInState >= _idleDuration;

    public override void OnEnter()
    {
        base.OnEnter();
        enemyCore.StopMovement();
        enemyCore.ClearPatrolTarget();
        _idleDuration = enemyCore.SampleIdleDuration();
    }

    public override void OnExit()
    {
        enemyCore.StopMovement();
    }
}
