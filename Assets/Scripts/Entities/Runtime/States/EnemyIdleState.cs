public class EnemyIdleState : EnemyStateBase
{
    private float _idleDuration;

    public override void OnEnter()
    {
        base.OnEnter();
        enemyCore.StopMovement();
        enemyCore.ClearPatrolTarget();
        _idleDuration = enemyCore.SampleIdleDuration();
    }

    public override void Do()
    {
        if (enemyCore.TryDetectTarget() && (enemyCore.CanSeeTarget(out _) || enemyCore.IsTargetWithinDetectionRadius()))
        {
            var next = enemyCore.IsTargetInAttackRange() ? StateId.Attack : StateId.Chase;
            Set(next, forceReset: true);
            return;
        }

        if (time >= _idleDuration)
        {
            Set(StateId.Patrol, forceReset: true);
        }
    }

    public override void OnExit()
    {
        enemyCore.StopMovement();
    }
}
