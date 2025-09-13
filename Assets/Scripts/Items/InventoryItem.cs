using UnityEngine;

[System.Serializable]
public struct InventoryItem
{
    [field: SerializeField] public ItemBaseSO ItemSO { get; private set; }
    [field: SerializeField] public int Amount { get; private set; }

    public bool IsEmpty => ItemSO == null || Amount <= 0;
    public static InventoryItem Empty => new InventoryItem(null, 0);

    public InventoryItem(ItemBaseSO itemSO, int amount = 1)
    {
        ItemSO = itemSO;
        Amount = Mathf.Max(0, amount);
    }

    public InventoryItem WithAmount(int newAmount)
    {
        return newAmount <= 0 ? Empty : new InventoryItem(ItemSO, newAmount);
    }    
}