using UnityEngine;

public abstract class StatBarViewBase : MonoBehaviour, IStatBar
{
    protected PlayerRuntimeData Data { get; private set; }

    public virtual void Initialize(PlayerRuntimeData data)
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
