using System.Collections;
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
    [SerializeField] private SpriteRenderer _iconRenderer;

    [Header("Pickup")]
    [SerializeField] private float _pickupDelay = 0.50f; // seconds before it can be picked up (avoid instant re-pickup)

    [Header("Lifetime")]
    [SerializeField] private float _lifetimeSeconds = 300f;

    [Header("Merge Animation")]
    [SerializeField] private float _mergeDuration = 0.18f;
    [SerializeField] private float _mergeShrink = 0.8f;
    [SerializeField] private float _leftoverItemNudgeImpulse = 0.7f;

    /// <summary> The item stack represented by the dropped item in the world (world item). </summary>
    public InventoryItem Item { get; private set; } = InventoryItem.Empty;

    private float _spawnTime;
    private bool _isMerging;
    private Rigidbody2D _body;
    private Collider2D _col;

    /// <summary> Property indicating if the item can currently be picked up. </summary>
    private bool CanBePickedUp => Time.time >= _spawnTime + _pickupDelay;

    private void Awake()
    {
        _spawnTime = Time.time;
        TryGetComponent(out _body);
        TryGetComponent(out _col);
    }

    private void Update()
    {
        HandleLifetime();
    }

    /// <summary>
    /// If lifetime is set, counts down and destroys the object when time is up.
    /// </summary>
    private void HandleLifetime()
    {
        // lifetimeSeconds = 0 means the item isn't despawnable
        if (_lifetimeSeconds <= 0f)
            return;

        var itemsTimeAlive = Time.time - _spawnTime;
        if (itemsTimeAlive >= _lifetimeSeconds)
        {
            Destroy(gameObject);
            return;
        }
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
        if (Item.IsEmpty || Item.Item == null)
        {
            _iconRenderer.enabled = false;
            return;
        }

        _iconRenderer.enabled = true;
        _iconRenderer.sprite = Item.Item.Icon;
    }

    /// <summary>
    /// Tries to merge with another world item when they collide.
    /// </summary>
    /// <param name="other">Other collider the world item came into contact with.</param>
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.TryGetComponent<WorldItem>(out var otherWorldItem) && otherWorldItem != null)
        {
            TryStartVisualMerge(otherWorldItem);
        }
    }

    /// <summary>
    /// Tries to pick up the item when the player is in range and the pickup delay has passed.
    /// </summary>
    /// <param name="other">Other collider the world item came into contact with.</param>
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

    private void TryStartVisualMerge(WorldItem other)
    {
        // basic guards
        if (_isMerging || other._isMerging)
            return;
        if (Item.IsEmpty || other.Item.IsEmpty) 
            return;
        if (Item.Item != other.Item.Item) 
            return;
        if (!Item.Item.IsStackable)
            return;

        // choose the item that stays on the ground and which is the donor
        (var receivingItem, var incomingItem) = _spawnTime <= other._spawnTime ? (this, other) : (other, this);

        // if anchor already full, don’t start an animation
        if (receivingItem.Item.Amount >= receivingItem.Item.Item.MaxStackSize) 
            return;

        // only the donor runs the animation
        if (incomingItem == this)
            StartCoroutine(AnimateMergeInto(receivingItem));
        else
            other.StartCoroutine(other.AnimateMergeInto(receivingItem));
    }

    private IEnumerator AnimateMergeInto(WorldItem receivingItem)
    {
        // lock self
        _isMerging = true;

        // freeze physics and disable collider while animating
        if (_body)
        {
            _body.linearVelocity = Vector2.zero;
            _body.angularVelocity = 0f;
            _body.bodyType = RigidbodyType2D.Kinematic;
        }
        if (_col) 
            _col.enabled = false;

        var startPos = transform.position;
        var startScale = transform.localScale;

        var elapsed = 0f;
        while (elapsed < _mergeDuration)
        {
            // item is picked up, stop animating
            if (receivingItem == null) 
                break;

            elapsed += Time.deltaTime;

            // progress is 0 to 1 over the duration
            var progress = Mathf.Clamp01(elapsed / _mergeDuration);

            // smoothstep easing
            var easedProgress = Mathf.SmoothStep(0f, 1f, progress);

            transform.position = Vector3.Lerp(startPos, receivingItem.transform.position, easedProgress);
            transform.localScale = Vector3.Lerp(startScale, startScale * _mergeShrink, easedProgress);

            yield return null;
        }

        // if anchor disappeared mid-animation, just restore and exit
        if (receivingItem == null)
        {
            if (_col) 
                _col.enabled = true;
            if (_body) 
                _body.bodyType = RigidbodyType2D.Dynamic;
            
            transform.localScale = startScale;
            _isMerging = false;
            yield break;
        }

        // apply transfer with current capacity to avoid overfill when multiple donors arrive
        var freeSpace = Mathf.Max(0, receivingItem.Item.Item.MaxStackSize - receivingItem.Item.Amount);
        var toMove = Mathf.Min(freeSpace, Item.Amount);

        if (toMove > 0)
        {
            receivingItem.Item = receivingItem.Item.WithAmount(receivingItem.Item.Amount + toMove);
            receivingItem.Render();

            Item = Item.WithAmount(Item.Amount - toMove);
        }

        if (Item.IsEmpty)
        {
            Destroy(gameObject);
            yield break;
        }

        // anchor reached its max stack size, update the leftover item
        transform.localScale = startScale;
        if (_col)
        {
            _col.enabled = true;
        }
        if (_body)
        {
            _body.bodyType = RigidbodyType2D.Dynamic;
            var dir = Random.insideUnitCircle.normalized;
            _body.AddForce(dir * _leftoverItemNudgeImpulse, ForceMode2D.Impulse);
        }

        Render();
        _isMerging = false;
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

        var pickupAttempt = Item;

        // add across the whole inventory and get leftover (if any).
        inventory.TryAddItemToRange(Item, new SlotRange(0, inventory.Capacity), out var leftoverItem);
        ItemPickupFeedReporter.ReportAddedToInventory(pickupAttempt, leftoverItem);

        if (leftoverItem.IsEmpty)
        {
            Destroy(gameObject);
        }
        else
        {
            Item = leftoverItem;
            Render();
        }
    }
}
