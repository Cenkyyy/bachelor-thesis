using System;
using UnityEngine;

[Serializable]
public struct ItemStatModifier
{
    [field: SerializeField] public ItemStatType Stat { get; private set; }
    [field: SerializeField] public float Value { get; private set; }

    public ItemStatModifier(ItemStatType stat, float value)
    {
        Stat = stat;
        Value = value;
    }
}
