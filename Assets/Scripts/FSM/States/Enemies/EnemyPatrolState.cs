public class EnemyPatrolState : EnemyStateBase
{
    public override void OnEnter()
    {
        base.OnEnter();

        if (!enemyCore.EnsurePatrolTarget())
        {
            Set(EntityStateId.Idle, forceReset: true);
        }
    }

    public override void Do()
    {
        if (enemyCore.TryDetectTarget() && (enemyCore.CanSeeTarget(out _) || enemyCore.IsTargetWithinDetectionRadius()))
        {
            var next = enemyCore.IsTargetInAttackRange() ? EntityStateId.Attack : EntityStateId.Chase;
            Set(next, forceReset: true);
            return;
        }

        if (!enemyCore.EnsurePatrolTarget())
        {
            Set(EntityStateId.Idle, forceReset: true);
            return;
        }

        if (enemyCore.ArrivedAt(enemyCore.CurrentPatrolTarget))
        {
            enemyCore.ClearPatrolTarget();
            Set(EntityStateId.Idle, forceReset: true);
        }
    }

    public override void FixedDo()
    {
        if (!enemyCore.EnsurePatrolTarget())
        {
            enemyCore.StopMovement();
            return;
        }

        enemyCore.MoveToUsingPath(enemyCore.CurrentPatrolTarget);
    }

    public override void OnExit()
    {
        enemyCore.StopMovement();
    }
}
