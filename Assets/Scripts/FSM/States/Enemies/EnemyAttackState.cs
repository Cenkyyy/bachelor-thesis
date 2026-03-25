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
        TryApplyAttackDamage();

        var totalCommit = enemyCore.AttackWindupSeconds + enemyCore.AttackHitWindowSeconds + enemyCore.AttackRecoverySeconds;

        if (time < totalCommit)
        {
            enemyCore.StopMovement();
            return;
        }

        if (!enemyCore.HasTarget)
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

        Set(EntityStateId.Chase, forceReset: true);
    }

    public override void OnExit()
    {
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

        _hasAppliedDamage = enemyCore.TryDealDamageToCurrentTarget(enemyCore.AttackDamage);
    }
}
