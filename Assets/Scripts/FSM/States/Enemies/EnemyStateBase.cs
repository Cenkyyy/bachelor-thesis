public abstract class EnemyStateBase : State
{
    protected EnemyCore enemyCore;

    public override void OnEnter()
    {
        base.OnEnter();
        enemyCore = (EnemyCore)core;
        enemyCore.SetDeadAnimation(false);
    }
}
