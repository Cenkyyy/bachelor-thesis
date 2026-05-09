using UnityEngine;

public class EntityData : ScriptableObject
{
    [field: Header("Prefab")]
    [field: SerializeField] public EntityCore Prefab { get; private set; }

    [field: Header("Core Stats")]
    [field: SerializeField] public int MaxHealth { get; private set; } = 40;

    protected virtual void OnValidate()
    {
        if (MaxHealth < 1)
            MaxHealth = 1;
    }
}
