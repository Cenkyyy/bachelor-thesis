using System;
using UnityEngine;

[Serializable]
public struct ItemStatusEffect
{
    [field: SerializeField] public ItemStatusEffectType StatusEffectType { get; private set; }
    [field: SerializeField] public float Value { get; private set; }

    public ItemStatusEffect(ItemStatusEffectType statusEffectType, float value)
    {
        StatusEffectType = statusEffectType;
        Value = value;
    }
}
