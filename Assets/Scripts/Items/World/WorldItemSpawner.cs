using UnityEngine;

/// <summary>
/// Spawns WorldItem prefabs when the player drops items.
/// </summary>
public sealed class WorldItemSpawner : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private WorldItem _worldItemPrefab;
    [SerializeField] private Transform _dropParent;

    [Header("Spawn Settings")]
    [SerializeField] private float _scatterImpulse = 1.2f;

    /// <summary>
    /// Spawns a WorldItem in the world at the given position, with optional toss direction.
    /// </summary>
    /// <param name="item">Item to spawn.</param>
    /// <param name="worldPos">Item's position in the world.</param>
    /// <param name="direction">Direction to toss the item to, if null, choose randomly.</param>
    /// <returns>The instance of WorldItem.</returns>
    public WorldItem Spawn(InventoryItem item, Vector3 worldPos, Vector2? direction = null)
    {
        if (item.IsEmpty || _worldItemPrefab == null) 
            return null;

        worldPos.z = 0f;

        // instantiate and initialize
        var inst = Instantiate(_worldItemPrefab, worldPos, Quaternion.identity, _dropParent);
        inst.Initialize(item);

        if (inst.TryGetComponent<Rigidbody2D>(out var body))
        {
            // choose random direction if none provided
            var dir = direction ?? (Random.insideUnitCircle.normalized);

            // in case of clicking on the player, just drop to the right
            if (dir.sqrMagnitude < 0.0001f)
                dir = Vector2.right;

            // apply impulse
            body.AddForce(dir * _scatterImpulse, ForceMode2D.Impulse);
        }
        return inst;
    }

    /// <summary>
    /// Overload for spawning by ItemBaseSO + amount.
    /// </summary>
    /// <param name="itemSO">ItemSO to spawn.</param>
    /// <param name="amount">Amount to spawn.</param>
    /// <param name="worldPos">Current player's position</param>
    /// <param name="tossDirection">Direction to toss the item to, if null, choose randomly.</param>
    /// <returns>The instance of WorldItem.</returns>
    public WorldItem Spawn(ItemBaseSO itemSO, int amount, Vector3 worldPos, Vector2? tossDirection = null) =>
        Spawn(new InventoryItem(itemSO, amount), worldPos, tossDirection);
}
