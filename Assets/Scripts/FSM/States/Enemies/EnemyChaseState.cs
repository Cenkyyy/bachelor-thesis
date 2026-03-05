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
            Set(ActorStateId.Patrol, forceReset: true);
            return;
        }

        var canSee = enemyCore.CanSeeTarget(out _);
        if (canSee)
        {
            enemyCore.ResetLostSightTimer();
        }
        else if (enemyCore.TickLostSight(UnityEngine.Time.deltaTime))
        {
            Set(ActorStateId.Patrol, forceReset: true);
            return;
        }

        if (enemyCore.IsOutsideLeash())
        {
            Set(ActorStateId.Patrol, forceReset: true);
            return;
        }

        if (enemyCore.IsTargetInAttackRange())
        {
            Set(ActorStateId.Attack, forceReset: true);
            return;
        }

        enemyCore.MoveToUsingPath(enemyCore.Target.position);
    }

    public override void OnExit()
    {
        enemyCore.StopMovement();
    }
}
