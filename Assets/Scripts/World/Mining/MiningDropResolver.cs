using System.Collections.Generic;
using UnityEngine;

public static class MiningDropResolver
{
    public static void ResolveDrops(IReadOnlyList<MiningDropEntry> drops, Player player, ItemDropSpawner dropSpawner, Vector3 dropPosition)
    {
        if (drops == null || drops.Count == 0 || player == null)
            return;

        dropPosition.z = 0f;

        for (int i = 0; i < drops.Count; i++)
        {
            var entry = drops[i];
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
}
