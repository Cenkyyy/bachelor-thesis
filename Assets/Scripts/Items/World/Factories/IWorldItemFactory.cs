using UnityEngine;

public interface IWorldItemFactory
{
    WorldItem Create(InventoryItem item, Vector3 worldPosition, Transform parent = null);
}
