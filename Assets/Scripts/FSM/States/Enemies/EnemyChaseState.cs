public class EnemyChaseState : EnemyStateBase
{
    public override void OnEnter()
    {
        base.OnEnter();
        enemyCore.ResetLostSightTimer();
    }

    public override void Do()
    {
        enemyCore.TryDetectTarget();

        if (!enemyCore.HasTarget)
        {
            Set(EntityStateId.Patrol, forceReset: true);
            return;
        }

        var canSee = enemyCore.CanSeeTarget(out _);
        if (canSee)
        {
            enemyCore.ResetLostSightTimer();
        }
        else if (enemyCore.TickLostSight(UnityEngine.Time.deltaTime))
        {
            Set(EntityStateId.Patrol, forceReset: true);
            return;
        }

        if (enemyCore.IsOutsideLeash())
        {
            Set(EntityStateId.Patrol, forceReset: true);
            return;
        }

        if (enemyCore.IsTargetInAttackRange())
        {
            Set(EntityStateId.Attack, forceReset: true);
            return;
        }

        enemyCore.MoveToUsingPath(enemyCore.Target.position);
    }

    public override void OnExit()
    {
        enemyCore.StopMovement();
    }
}
