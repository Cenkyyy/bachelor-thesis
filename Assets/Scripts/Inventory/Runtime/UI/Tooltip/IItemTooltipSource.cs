using UnityEngine;

public interface IItemTooltipSource
{
    RectTransform TooltipAnchor { get; }

    bool TryGetTooltipData(out InventorySlotView slotContext, out InventoryItem inventoryItem);
}
