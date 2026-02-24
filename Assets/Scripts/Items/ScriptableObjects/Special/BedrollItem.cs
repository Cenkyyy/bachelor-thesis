using UnityEngine;

[CreateAssetMenu(menuName = "Items/Special/Bedroll Item")]
public sealed class BedrollItem : Item, IPlaceableItem
{
    [field: Header("Placement")]
    [field: SerializeField] public GameObject BedrollPrefab { get; private set; }
    [field: SerializeField] public Vector2 PlacementCheckSize { get; private set; } = Vector2.one;

    public GameObject PlacementPrefab => BedrollPrefab;

    protected override ItemType? ExpectedCategory => ItemType.Special;

    protected override void OnValidate()
    {
        base.OnValidate();

        if (PlacementCheckSize.x <= 0f || PlacementCheckSize.y <= 0f)
            PlacementCheckSize = Vector2.one;
    }
}
