using UnityEngine;

/// <summary>
/// Reports successful inventory gains to the pickup feed.
/// </summary>
public static class ItemPickupFeedReporter
{
    /// <summary>
    /// Displays a pickup feed entry for the portion of an attempted inventory add that was accepted.
    /// </summary>
    public static void ReportAddedToInventory(InventoryItem attemptedAdd, InventoryItem leftover)
    {
        if (attemptedAdd.IsEmpty || attemptedAdd.Item == null || attemptedAdd.Amount <= 0)
            return;

        var leftoverAmount = Mathf.Max(0, leftover.Amount);
        var addedAmount = attemptedAdd.Amount - leftoverAmount;
        if (addedAmount <= 0)
            return;

        ItemPickupFeed.Instance?.ShowPickup(attemptedAdd.Item, addedAmount);
    }
}