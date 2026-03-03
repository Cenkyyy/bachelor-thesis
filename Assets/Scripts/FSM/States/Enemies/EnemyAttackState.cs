public class EnemyAttackState : EnemyStateBase
{
    public override void OnEnter()
    {
        base.OnEnter();
        enemyCore.StopMovement();
        enemyCore.TriggerAttackAnimation();
    }

    public override void Do()
    {
        var totalCommit = enemyCore.AttackWindupSeconds + enemyCore.AttackRecoverySeconds;
        if (time < totalCommit)
        {
            enemyCore.StopMovement();
            return;
        }

        if (!enemyCore.HasTarget)
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

        Set(ActorStateId.Chase, forceReset: true);
    }

    public override void OnExit()
    {
        enemyCore.StopMovement();
    }
}
