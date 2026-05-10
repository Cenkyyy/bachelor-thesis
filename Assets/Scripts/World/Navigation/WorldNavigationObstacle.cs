using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Registers a static world object collider footprint into the shared navigation grid.
/// </summary>
[DisallowMultipleComponent]
public sealed class WorldNavigationObstacle : MonoBehaviour
{
    [Header("Colliders")]
    [SerializeField] private List<Collider2D> _colliders = new();

    public IReadOnlyList<Collider2D> Colliders => _colliders;

    private void Awake()
    {
        EnsureColliders();
    }

    private void OnEnable()
    {
        EnsureColliders();
        ChunkWorldNavigationController.Instance?.RegisterObstacle(this);
    }

    private void OnDisable()
    {
        ChunkWorldNavigationController.Instance?.UnregisterObstacle(this);
    }

    public static void AttachTo(GameObject instance)
    {
        if (instance == null || instance.GetComponentInChildren<Collider2D>(includeInactive: false) == null)
            return;

        var obstacle = instance.GetComponent<WorldNavigationObstacle>();
        if (obstacle == null)
            obstacle = instance.AddComponent<WorldNavigationObstacle>();

        obstacle.Refresh();
    }

    public void Refresh()
    {
        EnsureColliders();
        ChunkWorldNavigationController.Instance?.RefreshObstacle(this);
    }

    private void EnsureColliders()
    {
        _colliders.RemoveAll(collider => collider == null);
        if (_colliders.Count > 0)
            return;

        GetComponentsInChildren(includeInactive: false, _colliders);
    }
}
