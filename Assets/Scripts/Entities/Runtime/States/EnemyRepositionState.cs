using UnityEngine;

/// <summary>
/// Moves ranged enemies away from the target for a short time before the next shot decision.
/// </summary>
public class EnemyRepositionState : EnemyStateBase
{
    private readonly RangedEnemyCore _rangedEnemyCore;
    private float _duration;
    private Vector2 _targetPosition;

    public EnemyRepositionState(EnemyCore enemyCore) : base(enemyCore)
    {
        _rangedEnemyCore = enemyCore as RangedEnemyCore;
    }

    public bool ShouldAttack { get; private set; }
    public bool ShouldInvestigate { get; private set; }

    public override void OnEnter()
    {
        base.OnEnter();
        ShouldAttack = false;
        ShouldInvestigate = false;

        if (_rangedEnemyCore == null)
        {
            _duration = 0f;
            _targetPosition = enemyCore.transform.position;
            ShouldInvestigate = true;
            enemyCore.ClearFacingOverride();
            enemyCore.SetRunningAnimation(false);
            enemyCore.StopMovement();
            return;
        }

        _duration = _rangedEnemyCore.SampleRangedRepositionDuration();
        _targetPosition = _rangedEnemyCore.SampleRangedRepositionTarget();
        enemyCore.ClearFacingOverride();
        enemyCore.SetRunningAnimation(true);
    }

    public override void Do()
    {
        if (enemyCore.RuntimeData.IsDead)
            return;

        if (TimeInState < _duration)
            return;

        ShouldAttack = enemyCore.CanAttackCurrentTarget();
        ShouldInvestigate = !ShouldAttack;
    }

    public override void FixedDo()
    {
        if (enemyCore.RuntimeData.IsDead)
            return;

        if (TimeInState >= _duration)
        {
            enemyCore.StopMovement();
            return;
        }

        enemyCore.MoveToUsingPath(_targetPosition);
    }

    public override void OnExit()
    {
        enemyCore.SetRunningAnimation(false);
        enemyCore.StopMovement();
    }
}
