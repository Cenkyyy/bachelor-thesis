using UnityEngine;

/// <summary>
/// Base asset for spell words, containing shared display and shop metadata.
/// </summary>
public abstract class WordData : ScriptableObject
{
    [field: Header("Word")]
    [field: SerializeField] public string DisplayName { get; private set; }

    [field: Header("Shop")]
    [field: SerializeField] public int MemoryCost { get; private set; }

    public abstract WordCategory Category { get; }
    public abstract bool IsValid { get; }

    protected virtual void OnValidate()
    {
        MemoryCost = Mathf.Max(0, MemoryCost);
    }
}
