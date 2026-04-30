using UnityEngine;

[CreateAssetMenu(menuName = "Items/Material Item")]
public class MaterialItemData : ItemData
{
    [field: SerializeField] public MaterialType Kind { get; private set; } = MaterialType.Generic;
    [field: SerializeField] public ToolTier TierTag { get; private set; } = ToolTier.None;

    protected override ItemType? ExpectedCategory => ItemType.Material;

    protected override void OnValidate()
    {
        base.OnValidate();
    }
}
