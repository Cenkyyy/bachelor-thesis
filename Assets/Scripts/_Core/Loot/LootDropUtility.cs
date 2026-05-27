using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Common helpers for resolving loot entries into gameplay item stacks.
/// </summary>
public static class LootDropUtility
{
    public static void AddToPlayerInventoryOrSpawnLeftovers(IReadOnlyList<DropEntry> drops, Player player, WorldItemSpawner dropSpawner, Vector3 dropPosition)
    {
        if (drops == null || drops.Count == 0 || player == null)
            return;

        dropPosition.z = 0f;

        for (var i = 0; i < drops.Count; i++)
        {
            var entry = drops[i];
            if (entry == null)
                continue;

            var amount = entry.RollAmount();
            if (amount <= 0 || entry.Item == null)
                continue;

            var dropItem = new InventoryItem(entry.Item, amount);
            player.Inventory.TryAddItemToRange(dropItem, new SlotRange(0, player.Inventory.Capacity), out var leftoverItem);
            ItemPickupFeedReporter.ReportAddedToInventory(dropItem, leftoverItem);

            if (!leftoverItem.IsEmpty && dropSpawner != null)
                dropSpawner.Spawn(leftoverItem, dropPosition);
        }
    }

    public static void SpawnInWorld(IReadOnlyList<DropEntry> drops, WorldItemSpawner dropSpawner, Vector3 dropPosition)
    {
        if (drops == null || drops.Count == 0 || dropSpawner == null)
            return;

        dropPosition.z = 0f;

        for (var i = 0; i < drops.Count; i++)
        {
            var entry = drops[i];
            if (entry == null)
                continue;

            var amount = entry.RollAmount();
            if (amount <= 0 || entry.Item == null)
                continue;

            dropSpawner.Spawn(new InventoryItem(entry.Item, amount), dropPosition);
        }
    }
}
