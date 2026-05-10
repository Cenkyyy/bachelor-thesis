using UnityEngine;

/// <summary>
/// Moves ranged enemies away from the target for a short time before the next shot decision.
/// </summary>
public class EnemyRepositionState : EnemyStateBase
{
    private float _duration;
    private Vector2 _targetPosition;

    public EnemyRepositionState(EnemyCore enemyCore) : base(enemyCore)
    {
    }

    public bool ShouldAttack { get; private set; }
    public bool ShouldInvestigate { get; private set; }

    public override void OnEnter()
    {
        base.OnEnter();
        _duration = enemyCore.SampleRangedRepositionDuration();
        _targetPosition = enemyCore.SampleRangedRepositionTarget();
        ShouldAttack = false;
        ShouldInvestigate = false;
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
