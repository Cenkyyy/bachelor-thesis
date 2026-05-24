using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Registers a static world object collider footprint into the shared navigation grid.
/// </summary>
[DisallowMultipleComponent]
public sealed class WorldNavigationObstacle : MonoBehaviour
{
    private static readonly LayerMask DefaultBlockingLayerMask = LayerMask.GetMask("Obstacle");

    [Header("Colliders")]
    [SerializeField] private LayerMask _blockingLayerMask = DefaultBlockingLayerMask;
    [SerializeField] private List<Collider2D> _blockingColliders = new();

    public IReadOnlyList<Collider2D> BlockingColliders => _blockingColliders;

    private void Awake()
    {
        EnsureColliders();
    }

    private void OnEnable()
    {
        EnsureColliders();
        WorldChunkNavigationController.Instance?.RegisterObstacle(this);
    }

    private void OnDisable()
    {
        WorldChunkNavigationController.Instance?.UnregisterObstacle(this);
    }

    public static void AttachTo(GameObject instance)
    {
        if (instance == null || !HasBlockingCollider(instance))
            return;

        var obstacle = instance.GetComponent<WorldNavigationObstacle>();
        if (obstacle == null)
            obstacle = instance.AddComponent<WorldNavigationObstacle>();

        obstacle.Refresh();
    }

    public void Refresh()
    {
        EnsureColliders();
        WorldChunkNavigationController.Instance?.RefreshObstacle(this);
    }

    private void EnsureColliders()
    {
        _blockingColliders.RemoveAll(collider => !IsBlockingCollider(collider, _blockingLayerMask));
        if (_blockingColliders.Count > 0)
            return;

        GetComponentsInChildren(includeInactive: false, _blockingColliders);
        _blockingColliders.RemoveAll(collider => !IsBlockingCollider(collider, _blockingLayerMask));
    }

    private static bool HasBlockingCollider(GameObject instance)
    {
        var colliders = instance.GetComponentsInChildren<Collider2D>(includeInactive: false);
        for (var i = 0; i < colliders.Length; i++)
        {
            var collider = colliders[i];
            if (IsBlockingCollider(collider, DefaultBlockingLayerMask))
                return true;
        }

        return false;
    }

    private static bool IsBlockingCollider(Collider2D collider, LayerMask layerMask)
    {
        if (collider == null || collider.isTrigger)
            return false;

        var colliderLayerMask = 1 << collider.gameObject.layer;
        return (layerMask.value & colliderLayerMask) != 0;
    }
}
