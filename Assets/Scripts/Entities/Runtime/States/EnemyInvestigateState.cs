using UnityEngine;

/// <summary>
/// Searches around the last visible target position before the enemy gives up.
/// </summary>
public class EnemyInvestigateState : EnemyStateBase
{
    private float _investigationDuration;
    private float _idleDuration;
    private Vector2 _investigationTarget;
    private bool _hasReachedTarget;

    public EnemyInvestigateState(EnemyCore enemyCore) : base(enemyCore)
    {
    }

    public bool ShouldAttack { get; private set; }
    public bool ShouldChase { get; private set; }
    public bool ShouldReposition { get; private set; }
    public bool ShouldReturnHome { get; private set; }

    public override void OnEnter()
    {
        base.OnEnter();
        _investigationDuration = enemyCore.SampleInvestigationDuration();
        _idleDuration = enemyCore.SampleIdleDuration();
        _investigationTarget = enemyCore.HasLastKnownTargetPosition ? enemyCore.LastKnownTargetPosition : enemyCore.HomePoint;
        _hasReachedTarget = false;
        ShouldAttack = false;
        ShouldChase = false;
        ShouldReposition = false;
        ShouldReturnHome = false;
        enemyCore.SetRunningAnimation(true);
    }

    public override void Do()
    {
        ShouldAttack = false;
        ShouldChase = false;
        ShouldReposition = false;
        ShouldReturnHome = false;

        enemyCore.TryDetectTarget();
        if (enemyCore.HasTarget && enemyCore.CanSeeCurrentTarget())
        {
            _investigationTarget = enemyCore.LastKnownTargetPosition;

            if (enemyCore.CanAttackCurrentTarget())
            {
                ShouldAttack = true;
                return;
            }

            if (enemyCore.IsRanged)
            {
                ShouldReposition = enemyCore.IsTargetTooCloseForRanged();
                return;
            }

            ShouldChase = true;
            return;
        }

        if (!_hasReachedTarget && enemyCore.ArrivedAt(_investigationTarget))
        {
            _hasReachedTarget = true;
            enemyCore.SetRunningAnimation(false);
            enemyCore.StopMovement();
        }

        var elapsedSearchTime = _hasReachedTarget ? _investigationDuration + _idleDuration : _investigationDuration;
        ShouldReturnHome = TimeInState >= elapsedSearchTime;
    }

    public override void FixedDo()
    {
        if (ShouldReturnHome || _hasReachedTarget)
        {
            enemyCore.StopMovement();
            return;
        }

        enemyCore.MoveToUsingPath(_investigationTarget);
    }

    public override void OnExit()
    {
        enemyCore.SetRunningAnimation(false);
        enemyCore.StopMovement();
    }
}
