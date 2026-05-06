using System;

/// <summary>
/// Read-only inventory contract for systems that only need to inspect items and observe changes.
/// </summary>
public interface IReadOnlyInventory
{
    int Capacity { get; }
    event Action<int> OnItemChanged;
    InventoryItem GetItemAt(int index);
}
