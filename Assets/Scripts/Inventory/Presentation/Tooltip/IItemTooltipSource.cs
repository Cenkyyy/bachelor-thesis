using UnityEngine;

public interface IItemTooltipSource
{
    RectTransform TooltipAnchor { get; }

    bool TryGetTooltipData(out Slot slotContext, out InventoryItem inventoryItem);
}
