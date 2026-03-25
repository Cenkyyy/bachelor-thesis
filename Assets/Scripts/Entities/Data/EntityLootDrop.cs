using System;
using UnityEngine;

[Serializable]
public struct EntityLootDrop
{
    [SerializeField] private ItemData _item;
    [SerializeField, Range(0f, 1f)] private float _dropChance;
    [SerializeField] private int _minAmount;
    [SerializeField] private int _maxAmount;

    public ItemData Item => _item;

    public int RollAmount()
    {
        if (_item == null)
            return 0;
        if (UnityEngine.Random.value > Mathf.Clamp01(_dropChance))
            return 0;

        return UnityEngine.Random.Range(_minAmount, _maxAmount + 1);
    }
}
