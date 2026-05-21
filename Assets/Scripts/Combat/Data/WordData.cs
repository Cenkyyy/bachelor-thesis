using UnityEngine;

public abstract class WordData : ScriptableObject
{
    [field: SerializeField] public string DisplayName { get; private set; }
    [field: SerializeField] public int MemoryCost { get; private set; }

    public abstract WordCategory Category { get; }
    public abstract bool IsValid { get; }
}
