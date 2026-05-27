using System;
using UnityEngine;

/// <summary>
/// Defines one possible item stack that can be rolled from shared loot data.
/// </summary>
[Serializable]
public sealed class DropEntry
{
    [SerializeField] private ItemData _item;
    [SerializeField, Range(0f, 1f)] private float _dropChance = 1f;
    [SerializeField] private int _minAmount;
    [SerializeField] private int _maxAmount;

    public ItemData Item => _item;
    public float DropChance => Mathf.Clamp01(_dropChance);

    public int RollAmount()
    {
        if (_item == null)
            return 0;

        if (UnityEngine.Random.value > DropChance)
            return 0;

        var min = Mathf.Max(0, _minAmount);
        var max = Mathf.Max(min, _maxAmount);
        return UnityEngine.Random.Range(min, max + 1);
    }
}
