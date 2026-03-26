using UnityEngine;

[DisallowMultipleComponent]
public sealed class DeathChestFactory : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject _deathChestPrefab;
    [SerializeField] private BoxCollider2D _deathChestPrefabCollider;
    [SerializeField] private Transform _deathChestParent;

    [Header("Placement")]
    [SerializeField] private LayerMask _placementObstacleMask = ~0;
    [SerializeField, Min(0.05f)] private float _placementStepDistance = 0.25f;
    [SerializeField, Min(0)] private int _placementSearchRadius = 3;

    public DeathChestHandle Create(string deathChestId, Vector3 worldPosition)
    {
        if (_deathChestPrefab == null)
        {
            Debug.LogWarning("Death chest prefab is missing.");
            return null;
        }

        var spawnPosition = ResolveSpawnPosition(worldPosition);
        var deathChestObject = Instantiate(_deathChestPrefab, spawnPosition, Quaternion.identity, _deathChestParent);
        var deathChestInventory = deathChestObject.GetComponent<DeathChestInventory>();

        if (deathChestInventory == null)
        {
            Debug.LogWarning("Death chest prefab must have a ChestInventory component.");
            Destroy(deathChestObject);
            return null;
        }

        var deathChestController = deathChestObject.GetComponent<TemporaryDeathChestController>();
        if (deathChestController == null)
            deathChestController = deathChestObject.AddComponent<TemporaryDeathChestController>();

        return new DeathChestHandle(deathChestId, deathChestObject.transform.position, deathChestInventory, deathChestController);
    }

    private Vector3 ResolveSpawnPosition(Vector3 desiredPosition)
    {
        if (!IsBlockedAt(desiredPosition))
            return desiredPosition;

        for (var radius = 1; radius <= _placementSearchRadius; radius++)
        {
            for (var y = -radius; y <= radius; y++)
            {
                for (var x = -radius; x <= radius; x++)
                {
                    if (x == 0 && y == 0)
                        continue;

                    var candidate = desiredPosition + new Vector3(x * _placementStepDistance, y * _placementStepDistance, 0f);
                    if (!IsBlockedAt(candidate))
                        return candidate;
                }
            }
        }

        return desiredPosition;
    }

    private bool IsBlockedAt(Vector3 center)
    {
        if (_deathChestPrefab == null)
            return true;

        if (_deathChestPrefabCollider != null)
        {
            var prefabScale = _deathChestPrefab.transform.lossyScale;
            var scaledSize = Vector2.Scale(_deathChestPrefabCollider.size, prefabScale);
            var scaledOffset = Vector2.Scale(_deathChestPrefabCollider.offset, prefabScale);
            var colliderCenter = (Vector2)center + scaledOffset;
            return Physics2D.OverlapBox(colliderCenter, scaledSize, 0f, _placementObstacleMask) != null;
        }

        return Physics2D.OverlapCircle(center, _placementStepDistance * 0.5f, _placementObstacleMask) != null;
    }
}
