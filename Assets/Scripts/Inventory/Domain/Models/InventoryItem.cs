using UnityEngine;

[System.Serializable]
public struct InventoryItem
{
    [field: SerializeField] public ItemData Item { get; private set; }
    [field: SerializeField] public int Amount { get; private set; }

    public bool IsEmpty => Item == null || Amount <= 0;
    public static InventoryItem Empty => new InventoryItem(null, 0);

    public InventoryItem(ItemData itemSO, int amount = 1)
    {
        Item = itemSO;
        Amount = Mathf.Max(0, amount);
    }

    public InventoryItem WithAmount(int newAmount)
    {
        return newAmount <= 0 ? Empty : new InventoryItem(Item, newAmount);
    }    
}
