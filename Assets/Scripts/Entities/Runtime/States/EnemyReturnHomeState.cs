/// <summary>
/// Moves an enemy back into its home area before patrol resumes.
/// </summary>
public class EnemyReturnHomeState : EnemyStateBase
{
    public EnemyReturnHomeState(EnemyCore enemyCore) : base(enemyCore)
    {
    }

    public bool ShouldPatrol { get; private set; }

    public override void OnEnter()
    {
        base.OnEnter();
        ShouldPatrol = false;
        enemyCore.SetRunningAnimation(false);
    }

    public override void Do()
    {
        ShouldPatrol = enemyCore.IsInsideHomeRadius();
    }

    public override void FixedDo()
    {
        if (ShouldPatrol)
        {
            enemyCore.StopMovement();
            return;
        }

        enemyCore.MoveToUsingPath(enemyCore.HomePoint);
    }

    public override void OnExit()
    {
        enemyCore.StopMovement();
    }
}
