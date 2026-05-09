public class EnemyAttackState : EnemyStateBase
{
    private bool _hasAppliedDamage;

    public override void OnEnter()
    {
        base.OnEnter();
        _hasAppliedDamage = false;
        enemyCore.StopMovement();
        enemyCore.TriggerAttackAnimation();
    }

    public override void Do()
    {
        if (enemyCore.IsRanged && enemyCore.HasTarget)
        {
            enemyCore.FaceTargetWhileKiting();
        }

        TryApplyAttackDamage();

        var totalCommit = enemyCore.AttackWindupSeconds + enemyCore.AttackHitWindowSeconds + enemyCore.AttackRecoverySeconds;

        if (time < totalCommit)
        {
            enemyCore.StopMovement();
            return;
        }

        if (!enemyCore.HasTarget)
        {
            Set(StateId.Patrol, forceReset: true);
            return;
        }

        if (enemyCore.IsOutsideLeash() && !enemyCore.IsTargetWithinDetectionRadius())
        {
            Set(StateId.Patrol, forceReset: true);
            return;
        }

        if (enemyCore.IsTargetInAttackRange())
        {
            Set(StateId.Attack, forceReset: true);
            return;
        }

        Set(StateId.Chase, forceReset: true);
    }

    public override void FixedDo()
    {
        if (!enemyCore.IsRanged || !enemyCore.HasTarget)
        {
            return;
        }

        var recoveryStart = enemyCore.AttackWindupSeconds + enemyCore.AttackHitWindowSeconds;
        if (time < recoveryStart || time >= recoveryStart + enemyCore.AttackRecoverySeconds)
        {
            return;
        }

        if (enemyCore.IsTargetTooCloseForRanged())
        {
            enemyCore.MoveToUsingPath(enemyCore.GetRangedKitePosition());
        }
        else
        {
            enemyCore.StopMovement();
        }
    }

    public override void OnExit()
    {
        enemyCore.ClearFacingOverride();
        enemyCore.StopMovement();
    }

    private void TryApplyAttackDamage()
    {
        if (_hasAppliedDamage)
        {
            return;
        }

        if (time < enemyCore.AttackWindupSeconds)
        {
            return;
        }

        if (time > enemyCore.AttackWindupSeconds + enemyCore.AttackHitWindowSeconds)
        {
            _hasAppliedDamage = true;
            return;
        }

        _hasAppliedDamage = enemyCore.IsRanged
            ? enemyCore.TryShootProjectileAtCurrentTarget()
            : enemyCore.TryDealDamageToCurrentTarget(enemyCore.AttackDamage);
    }
}
