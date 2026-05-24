using UnityEngine;

/// <summary>
/// Item data for materials used by crafting, drops, placement, and resource systems.
/// </summary>
[CreateAssetMenu(menuName = "Items/Material Item")]
public class MaterialItemData : ItemData
{
    [field: Header("Material Settings")]
    [field: SerializeField] public MaterialType Kind { get; private set; } = MaterialType.CraftingItem;

    protected override ItemType? ExpectedCategory => ItemType.Material;

    protected override void OnValidate()
    {
        base.OnValidate();
    }
}
