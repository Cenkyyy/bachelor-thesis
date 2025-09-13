using UnityEngine;

/// <summary>
/// A dropped item visible in the world. Shows icon + amount, can be picked up,
/// and despawns after a lifetime (fading near the end).
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public sealed class WorldItem : MonoBehaviour
{
    [Header("Rendering")]
    [SerializeField] private SpriteRenderer iconRenderer;

    [Header("Pickup")]
    [SerializeField] private float pickupDelay = 0.50f; // seconds before it can be picked up (avoid instant re-pickup)

    [Header("Lifetime")]
    [SerializeField] private float lifetimeSeconds = 300f;

    /// <summary> The item stack represented by the dropped item in the world (world item). </summary>
    public InventoryItem Item { get; private set; } = InventoryItem.Empty;

    /// <summary> Item's spawn time, used for pickup delay and lifetime. /// </summary>
    private float _spawnTime;

    /// <summary> Property indicating if the item can currently be picked up. </summary>
    private bool CanBePickedUp => Time.time >= _spawnTime + pickupDelay;

    private void Awake()
    {
        _spawnTime = Time.time;
        if (iconRenderer == null) 
            iconRenderer = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        HandleLifetime();
    }

    /// <summary>
    /// Initializes the world item with the given item stack.
    /// </summary>
    /// <param name="item">The item stack.</param>
    public void Initialize(InventoryItem item)
    {
        Item = item;
        Render();
    }

    /// <summary>
    /// Assigns the icon based on the current item.
    /// </summary>
    private void Render()
    {
        if (Item.IsEmpty || Item.ItemSO == null)
        {
            iconRenderer.enabled = false;
            return;
        }

        iconRenderer.enabled = true;
        iconRenderer.sprite = Item.ItemSO.Icon;
    }

    /// <summary>
    /// If lifetime is set, counts down and destroys the object when time is up.
    /// </summary>
    private void HandleLifetime()
    {
        if (lifetimeSeconds <= 0f)
            return;

        float itemsTimeAlive = Time.time - _spawnTime;
        if (itemsTimeAlive >= lifetimeSeconds)
        {
            Destroy(gameObject);
            return;
        }
    }

    /// <summary>
    /// Tries to pick up the item when the player is in range and the pickup delay has passed.
    /// </summary>
    /// <param name="other">Other collider the world item can into contact with.</param>
    private void OnTriggerStay2D(Collider2D other)
    {
        if (!CanBePickedUp) 
            return;
        if (!other.CompareTag("Player"))
            return;

        var player = other.GetComponent<Player>() ?? other.GetComponentInParent<Player>();
        if (player == null)
            return;

        TryPickupInto(player.Inventory);
    }

    /// <summary>
    /// Attempts to move this stack into the inventory, updating or destroying the object.
    /// </summary>
    /// <param name="inventory">The player's inventory to pick up the world item into.</param>
    private void TryPickupInto(PlayerInventory inventory)
    {
        if (Item.IsEmpty) 
        {
            Destroy(gameObject); 
            return;
        }

        // add across the whole inventory and get leftover (if any).
        var remainingItem = inventory.TryAddItem(Item, 0, inventory.TotalSize);

        if (remainingItem.IsEmpty)
        {
            Destroy(gameObject);
        }
        else
        {
            Item = remainingItem;
            Render();
        }
    }
}
