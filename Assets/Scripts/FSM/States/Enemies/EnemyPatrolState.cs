public class EnemyPatrolState : EnemyStateBase
{
    public override void OnEnter()
    {
        base.OnEnter();

        if (!enemyCore.EnsurePatrolTarget())
        {
            Set(ActorStateId.Idle, forceReset: true);
        }
    }

    public override void Do()
    {
        if (enemyCore.TryDetectTarget() && enemyCore.CanSeeTarget(out _))
        {
            var next = enemyCore.IsTargetInAttackRange() ? ActorStateId.Attack : ActorStateId.Chase;
            Set(next, forceReset: true);
            return;
        }

        if (!enemyCore.EnsurePatrolTarget())
        {
            Set(ActorStateId.Idle, forceReset: true);
            return;
        }

        enemyCore.MoveToUsingPath(enemyCore.CurrentPatrolTarget);

        if (enemyCore.ArrivedAt(enemyCore.CurrentPatrolTarget))
        {
            enemyCore.ClearPatrolTarget();
            Set(ActorStateId.Idle, forceReset: true);
        }
    }

    public override void OnExit()
    {
        enemyCore.StopMovement();
    }
}
