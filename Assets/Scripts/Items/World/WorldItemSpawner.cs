using UnityEngine;

/// <summary>
/// Spawns WorldItem prefabs when the player drops items.
/// </summary>
public sealed class WorldItemSpawner : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private WorldItem worldItemPrefab;
    [SerializeField] private Transform dropParent;

    [Header("Spawn Settings")]
    [SerializeField] private float scatterImpulse = 1.2f;

    /// <summary>
    /// Spawns a WorldItem in the world at the given position, with optional toss direction.
    /// </summary>
    /// <param name="item">Item to spawn.</param>
    /// <param name="worldPos">Item's position in the world.</param>
    /// <param name="tossDirection">Direction to toss the item to, if null, choose randomly.</param>
    /// <returns></returns>
    public WorldItem Spawn(InventoryItem item, Vector3 worldPos, Vector2? tossDirection = null)
    {
        if (item.IsEmpty || worldItemPrefab == null) 
            return null;

        worldPos.z = 0f;

        // instantiate and initialize
        var inst = Instantiate(worldItemPrefab, worldPos, Quaternion.identity, dropParent);
        inst.Initialize(item);

        if (inst.TryGetComponent<Rigidbody2D>(out var body))
        {
            // choose random direction if none provided
            Vector2 direction = tossDirection ?? (Random.insideUnitCircle.normalized);

            // in case of clicking on the player, just drop to the right
            if (direction.sqrMagnitude < 0.0001f)
                direction = Vector2.right;

            // apply impulse
            body.AddForce(direction * scatterImpulse, ForceMode2D.Impulse);
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
    /// <returns></returns>
    public WorldItem Spawn(ItemBaseSO itemSO, int amount, Vector3 worldPos, Vector2? tossDirection = null) =>
        Spawn(new InventoryItem(itemSO, amount), worldPos, tossDirection);
}
