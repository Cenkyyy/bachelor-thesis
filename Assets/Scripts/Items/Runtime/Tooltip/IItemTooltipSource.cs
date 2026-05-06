using UnityEngine;

/// <summary>
/// Contract for UI elements that can provide item data to the shared tooltip controller.
/// </summary>
public interface IItemTooltipSource
{
    RectTransform TooltipAnchor { get; }

    bool TryGetTooltipData(out InventorySlotView slotContext, out InventoryItem inventoryItem);
}
