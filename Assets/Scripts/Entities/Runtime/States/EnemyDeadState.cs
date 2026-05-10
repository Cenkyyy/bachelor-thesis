/// <summary>
/// Stops enemy movement and plays the death animation state.
/// </summary>
public class EnemyDeadState : EnemyStateBase
{
    public EnemyDeadState(EnemyCore enemyCore) : base(enemyCore)
    {
    }

    public override void OnEnter()
    {
        base.OnEnter();
        enemyCore.StopMovement();
        enemyCore.SetDeadAnimation(true);
    }

    public override void Do()
    {
        enemyCore.StopMovement();
    }
}
