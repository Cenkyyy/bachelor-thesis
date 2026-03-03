public class EnemyDeadState : EnemyStateBase
{
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
