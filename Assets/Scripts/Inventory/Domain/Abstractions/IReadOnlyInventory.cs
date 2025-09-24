using System;

public interface IReadOnlyInventory
{
    int Capacity { get; }
    event Action<int> OnItemChanged;
    InventoryItem GetItemAt(int index);
}