/// <summary>
/// Executes the enemy attack timing window and decides the post-attack transition.
/// </summary>
public class EnemyAttackState : EnemyStateBase
{
    private bool _hasAppliedDamage;

    public EnemyAttackState(EnemyCore enemyCore) : base(enemyCore)
    {
    }

    public bool ShouldChase { get; private set; }
    public bool ShouldInvestigate { get; private set; }
    public bool ShouldReposition { get; private set; }
    public bool ShouldRestartAttack { get; private set; }

    public override void OnEnter()
    {
        base.OnEnter();
        _hasAppliedDamage = false;
        ShouldChase = false;
        ShouldInvestigate = false;
        ShouldReposition = false;
        ShouldRestartAttack = false;
        enemyCore.StopMovement();
        enemyCore.TriggerAttackAnimation();
    }

    public override void Do()
    {
        if (enemyCore.RuntimeData.IsDead)
            return;

        if (enemyCore.IsRanged && enemyCore.HasTarget)
            enemyCore.FaceTargetWhileKiting();

        TryApplyAttackDamage();

        var totalCommit = enemyCore.Data.AttackWindupSeconds + enemyCore.Data.AttackHitWindowSeconds + enemyCore.Data.AttackRecoverySeconds;
        if (TimeInState < totalCommit)
        {
            enemyCore.StopMovement();
            return;
        }

        if (!enemyCore.HasTarget)
        {
            ShouldInvestigate = true;
            return;
        }

        if (enemyCore.IsOutsideLeash() && !enemyCore.CanSeeCurrentTarget())
        {
            ShouldInvestigate = true;
            return;
        }

        if (enemyCore.IsRanged)
        {
            ShouldReposition = enemyCore.IsTargetTooCloseForRanged() || enemyCore.CanAttackCurrentTarget();
            ShouldInvestigate = !ShouldReposition;
            return;
        }

        if (enemyCore.CanAttackCurrentTarget())
        {
            ShouldRestartAttack = true;
            return;
        }

        ShouldChase = enemyCore.CanSeeCurrentTarget();
        ShouldInvestigate = !ShouldChase;
    }

    public override void FixedDo()
    {
        if (enemyCore.RuntimeData.IsDead)
            return;

        enemyCore.StopMovement();
    }

    public override void OnExit()
    {
        enemyCore.ClearFacingOverride();
        enemyCore.StopMovement();
    }

    private void TryApplyAttackDamage()
    {
        if (_hasAppliedDamage)
            return;

        if (enemyCore.RuntimeData.IsDead)
            return;

        if (TimeInState < enemyCore.Data.AttackWindupSeconds)
            return;

        if (TimeInState > enemyCore.Data.AttackWindupSeconds + enemyCore.Data.AttackHitWindowSeconds)
        {
            _hasAppliedDamage = true;
            return;
        }

        _hasAppliedDamage = enemyCore.IsRanged
            ? enemyCore.TryShootProjectileAtCurrentTarget()
            : enemyCore.TryDealDamageToCurrentTarget(enemyCore.Data.AttackDamage);
    }
}
