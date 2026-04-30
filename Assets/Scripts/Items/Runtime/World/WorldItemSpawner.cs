using UnityEngine;

public sealed class WorldItemSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] private GameObject _worldItemPrefab;
    [SerializeField] private Transform _dropParent;
    [SerializeField] private float _scatterImpulse = 1.2f;

    private void OnValidate()
    {
        if (_worldItemPrefab != null && !_worldItemPrefab.TryGetComponent<WorldItem>(out _))
            _worldItemPrefab = null;
    }

    public WorldItem Spawn(InventoryItem item, Vector3 worldPos, Vector2? direction = null)
    {
        if (item.IsEmpty || _worldItemPrefab == null)
            return null;

        worldPos.z = 0f;

        var instance = Instantiate(_worldItemPrefab, worldPos, Quaternion.identity, _dropParent);
        if (!instance.TryGetComponent<WorldItem>(out var worldItem))
        {
            Destroy(instance);
            return null;
        }

        worldItem.SetItem(item);

        if (worldItem.Rigidbody != null)
        {
            var dir = direction ?? Random.insideUnitCircle.normalized;
            if (dir.sqrMagnitude < 0.0001f)
                dir = Vector2.right;

            worldItem.Rigidbody.AddForce(dir * _scatterImpulse, ForceMode2D.Impulse);
        }

        return worldItem;
    }
}
