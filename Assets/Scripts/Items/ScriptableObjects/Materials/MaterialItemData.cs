using UnityEngine;

[CreateAssetMenu(menuName = "Items/Material Item")]
public class MaterialItemData : ItemData
{
    [field: SerializeField] public MaterialKind Kind { get; private set; } = MaterialKind.Generic;
    [field: SerializeField] public ToolTier TierTag { get; private set; } = ToolTier.None;

    protected override ItemType? ExpectedCategory => ItemType.Material;

    protected override void OnValidate()
    {
        base.OnValidate();
    }
}
