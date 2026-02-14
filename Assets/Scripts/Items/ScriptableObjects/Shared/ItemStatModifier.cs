using System;
using UnityEngine;

[Serializable]
public struct ItemStatModifier
{
    [field: SerializeField] public ItemStatType Stat { get; private set; }
    [field: SerializeField] public ItemModifierMode Mode { get; private set; }
    [field: SerializeField] public float Value { get; private set; }

    public ItemStatModifier(ItemStatType stat, ItemModifierMode mode, float value)
    {
        Stat = stat;
        Mode = mode;
        Value = value;
    }
}