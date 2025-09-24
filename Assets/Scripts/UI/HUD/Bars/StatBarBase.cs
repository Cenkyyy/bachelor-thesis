using UnityEngine;

public abstract class StatBarBase : MonoBehaviour, IStatBar
{
    protected PlayerData Data { get; private set; }

    public virtual void Initialize(PlayerData data)
    {
        if (Data == data) 
            return;

        Unsubscribe();
        Data = data;
        Subscribe();
        
        DrawInitial();
    }

    protected virtual void OnEnable() => Subscribe();
    protected virtual void OnDisable() => Unsubscribe();
    protected virtual void OnDestroy() => Unsubscribe();

    protected abstract void Subscribe();
    protected abstract void Unsubscribe();
    protected abstract void DrawInitial();
}