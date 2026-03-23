using UnityEngine;

[DisallowMultipleComponent]
public sealed class WorldItemFactory : MonoBehaviour, IWorldItemFactory
{
    [SerializeField] private WorldItem _worldItemPrefab;

    public WorldItem Create(InventoryItem item, Vector3 worldPosition, Transform parent = null)
    {
        if (item.IsEmpty || _worldItemPrefab == null)
            return null;

        worldPosition.z = 0f;

        var worldItem = Instantiate(_worldItemPrefab, worldPosition, Quaternion.identity, parent);
        worldItem.Initialize(item);

        return worldItem;
    }
}
