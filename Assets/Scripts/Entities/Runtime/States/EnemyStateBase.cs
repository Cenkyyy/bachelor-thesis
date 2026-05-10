using UnityEngine;

/// <summary>
/// Base class for enemy states owned by EnemyStateMachineController.
/// </summary>
public abstract class EnemyStateBase : IState
{
    protected readonly EnemyCore enemyCore;

    private float _startTime;

    protected float TimeInState => Time.time - _startTime;

    protected EnemyStateBase(EnemyCore enemyCore)
    {
        this.enemyCore = enemyCore;
    }

    public virtual void OnEnter()
    {
        _startTime = Time.time;
        enemyCore.SetDeadAnimation(false);
        enemyCore.SetRunningAnimation(false);
    }

    public virtual void Do()
    {
    }

    public virtual void FixedDo()
    {
    }

    public virtual void OnExit()
    {
    }
}
