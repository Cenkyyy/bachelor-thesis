/// <summary>
/// Moves an enemy between sampled points around its home position.
/// </summary>
public class EnemyPatrolState : EnemyStateBase
{
    public EnemyPatrolState(EnemyCore enemyCore) : base(enemyCore)
    {
    }

    public bool ShouldIdle { get; private set; }

    public override void OnEnter()
    {
        base.OnEnter();
        ShouldIdle = false;

        if (!enemyCore.EnsurePatrolTarget())
            ShouldIdle = true;
    }

    public override void Do()
    {
        if (!enemyCore.EnsurePatrolTarget())
        {
            ShouldIdle = true;
            return;
        }

        if (enemyCore.ArrivedAt(enemyCore.CurrentPatrolTarget))
        {
            enemyCore.ClearPatrolTarget();
            ShouldIdle = true;
        }
    }

    public override void FixedDo()
    {
        if (!enemyCore.EnsurePatrolTarget())
        {
            enemyCore.StopMovement();
            return;
        }

        enemyCore.MoveToUsingPath(enemyCore.CurrentPatrolTarget);
    }

    public override void OnExit()
    {
        enemyCore.StopMovement();
    }
}
