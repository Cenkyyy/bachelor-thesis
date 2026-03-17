using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class DeathChestRegistry : MonoBehaviour
{
    private readonly Dictionary<string, DeathChestHandle> _byId = new Dictionary<string, DeathChestHandle>();
    private readonly Dictionary<IInventory, DeathChestHandle> _byInventory = new Dictionary<IInventory, DeathChestHandle>();

    public IReadOnlyCollection<DeathChestHandle> ActiveChests => _byId.Values;

    public void Register(DeathChestHandle handle)
    {
        if (handle == null || string.IsNullOrEmpty(handle.ChestId) || handle.Inventory == null || handle.Inventory.Inventory == null)
            return;

        _byId[handle.ChestId] = handle;
        _byInventory[handle.Inventory.Inventory] = handle;
    }

    public void Unregister(string chestId)
    {
        if (string.IsNullOrEmpty(chestId))
            return;

        if (!_byId.TryGetValue(chestId, out var handle))
            return;

        _byId.Remove(chestId);

        if (handle.Inventory != null && handle.Inventory.Inventory != null)
            _byInventory.Remove(handle.Inventory.Inventory);
    }

    public bool TryGetByInventory(IInventory inventory, out DeathChestHandle handle)
    {
        return _byInventory.TryGetValue(inventory, out handle);
    }
}
