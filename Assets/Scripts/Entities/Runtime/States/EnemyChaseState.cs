using UnityEngine;

/// <summary>
/// Pursues the visible target until combat range or investigation rules take over.
/// </summary>
public class EnemyChaseState : EnemyStateBase
{
    public EnemyChaseState(EnemyCore enemyCore) : base(enemyCore)
    {
    }

    public bool ShouldAttack { get; private set; }
    public bool ShouldInvestigate { get; private set; }

    public override void OnEnter()
    {
        base.OnEnter();
        enemyCore.SetRunningAnimation(true);
    }

    public override void Do()
    {
        ShouldAttack = false;
        ShouldInvestigate = false;

        if (enemyCore is RangedEnemyCore)
        {
            ShouldInvestigate = true;
            return;
        }

        enemyCore.TryDetectTarget();

        if (!enemyCore.HasTarget)
        {
            ShouldInvestigate = true;
            return;
        }

        var canSee = enemyCore.CanSeeCurrentTarget();
        if (!canSee)
        {
            ShouldInvestigate = true;
            return;
        }

        if (enemyCore.CanAttackCurrentTarget())
            ShouldAttack = true;
    }

    public override void FixedDo()
    {
        if (!enemyCore.HasTarget)
        {
            if (enemyCore.HasLastKnownTargetPosition)
                enemyCore.MoveToUsingPath(enemyCore.LastKnownTargetPosition);
            else
                enemyCore.StopMovement();

            return;
        }

        if (enemyCore.CanSeeCurrentTarget())
        {
            enemyCore.MoveToUsingPath(enemyCore.Target.position);
            return;
        }

        if (enemyCore.HasLastKnownTargetPosition)
        {
            enemyCore.MoveToUsingPath(enemyCore.LastKnownTargetPosition);
            return;
        }

        enemyCore.StopMovement();
    }

    public override void OnExit()
    {
        enemyCore.ClearFacingOverride();
        enemyCore.SetRunningAnimation(false);
        enemyCore.StopMovement();
    }
}
