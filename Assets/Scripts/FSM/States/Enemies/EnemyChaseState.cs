public class EnemyChaseState : EnemyStateBase
{
    public override void OnEnter()
    {
        base.OnEnter();
        enemyCore.SetRunningAnimation(true);
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

        if (enemyCore.IsRanged)
        {
            enemyCore.FaceTargetWhileKiting();
        }

        var canSee = enemyCore.CanSeeTarget(out _);
        if (canSee)
        {
            enemyCore.ResetLostSightTimer();
        }
        else if (!enemyCore.IsTargetWithinDetectionRadius() && enemyCore.TickLostSight(UnityEngine.Time.deltaTime))
        {
            Set(EntityStateId.Patrol, forceReset: true);
            return;
        }

        if (enemyCore.IsOutsideLeash() && !enemyCore.IsTargetWithinDetectionRadius())
        {
            Set(EntityStateId.Patrol, forceReset: true);
            return;
        }

        if (enemyCore.IsTargetInAttackRange())
        {
            Set(EntityStateId.Attack, forceReset: true);
            return;
        }
    }

    public override void FixedDo()
    {
        if (!enemyCore.HasTarget)
        {
            enemyCore.StopMovement();
            return;
        }

        if (enemyCore.IsRanged && enemyCore.IsTargetTooCloseForRanged())
        {
            enemyCore.MoveToUsingPath(enemyCore.GetRangedKitePosition());
            return;
        }

        enemyCore.MoveToUsingPath(enemyCore.Target.position);
    }

    public override void OnExit()
    {
        enemyCore.ClearFacingOverride();
        enemyCore.SetRunningAnimation(false);
        enemyCore.StopMovement();
    }
}
