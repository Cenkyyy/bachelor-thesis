using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class DeathChestRegistry : MonoBehaviour
{
    private readonly Dictionary<string, DeathChestRuntimeData> _byId = new Dictionary<string, DeathChestRuntimeData>();
    private readonly Dictionary<IInventory, DeathChestRuntimeData> _byInventory = new Dictionary<IInventory, DeathChestRuntimeData>();

    public IReadOnlyCollection<DeathChestRuntimeData> ActiveChests => _byId.Values;

    public void Register(DeathChestRuntimeData handle)
    {
        if (handle == null || string.IsNullOrEmpty(handle.DeathChestId) || handle.Inventory == null || handle.Inventory.Inventory == null)
            return;

        _byId[handle.DeathChestId] = handle;
        _byInventory[handle.Inventory.Inventory] = handle;
    }

    public void Unregister(string deathChestId)
    {
        if (string.IsNullOrEmpty(deathChestId))
            return;

        if (!_byId.TryGetValue(deathChestId, out var handle))
            return;

        _byId.Remove(deathChestId);

        if (handle.Inventory != null && handle.Inventory.Inventory != null)
            _byInventory.Remove(handle.Inventory.Inventory);
    }

    public bool TryGetByInventory(IInventory inventory, out DeathChestRuntimeData handle)
    {
        return _byInventory.TryGetValue(inventory, out handle);
    }
}
