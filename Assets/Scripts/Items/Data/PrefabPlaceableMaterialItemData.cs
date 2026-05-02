using UnityEngine;

[CreateAssetMenu(menuName = "Items/Material/Prefab Placeable Material Item")]
public sealed class PrefabPlaceableMaterialItemData : MaterialItemData, IPrefabPlaceableItem
{
    [field: Header("Placement Settings")]
    [field: SerializeField] public GameObject Prefab { get; private set; }

    [field: Tooltip("Placement size used for placement collision checks is in world units (for PPU 32 and 32px tiles, 1 = one tile).")]
    [field: SerializeField] public Vector2 PlacementCheckSize { get; private set; } = Vector2.one; 

    protected override void OnValidate()
    {
        base.OnValidate();

        if (PlacementCheckSize.x <= 0f || PlacementCheckSize.y <= 0f)
            PlacementCheckSize = Vector2.one;
    }
}
