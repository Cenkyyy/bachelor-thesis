using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(WorldItem))]
public sealed class WorldItemSpawnController : MonoBehaviour
{
    [Header("Pickup")]
    [SerializeField] private float _pickupDelay = 0.5f; // seconds before it can be picked up (avoid instant re-pickup)

    [Header("Lifetime")]
    [SerializeField] private float _lifetimeSeconds = 300f;

    [Header("Merge Animation")]
    [SerializeField] private float _mergeDuration = 0.18f;
    [SerializeField] private float _mergeShrink = 0.8f;
    [SerializeField] private float _leftoverItemNudgeImpulse = 0.7f;

    private WorldItem _worldItem;
    private Rigidbody2D _body;
    private Collider2D _col;
    private float _spawnTime;
    private bool _isMerging;

    private bool CanBePickedUp => Time.time >= _spawnTime + _pickupDelay;

    private void Awake()
    {
        _worldItem = GetComponent<WorldItem>();
        _worldItem.TryGetRigidbody(out _body);
        _worldItem.TryGetCollider(out _col);
        _spawnTime = Time.time;
    }

    private void Update()
    {
        HandleLifetime();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.TryGetComponent<WorldItemSpawnController>(out var otherController) || otherController == null)
            return;

        TryStartVisualMerge(otherController);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!CanBePickedUp || !other.CompareTag("Player"))
            return;

        var player = other.GetComponent<Player>() ?? other.GetComponentInParent<Player>();
        if (player == null)
            return;

        TryPickupInto(player.Inventory);
    }

    private void HandleLifetime()
    {
        if (_lifetimeSeconds <= 0f)
            return;

        if (Time.time - _spawnTime < _lifetimeSeconds)
            return;

        Destroy(gameObject);
    }

    private void TryStartVisualMerge(WorldItemSpawnController other)
    {
        if (_isMerging || other._isMerging)
            return;

        var myItem = _worldItem.Item;
        var otherItem = other._worldItem.Item;

        if (myItem.IsEmpty || otherItem.IsEmpty || myItem.Item != otherItem.Item || !myItem.Item.IsStackable)
            return;

        var receivingController = _spawnTime <= other._spawnTime ? this : other;
        var incomingController = receivingController == this ? other : this;

        if (receivingController._worldItem.Item.Amount >= receivingController._worldItem.Item.Item.MaxStackSize)
            return;

        incomingController.StartCoroutine(incomingController.AnimateMergeInto(receivingController));
    }

    private IEnumerator AnimateMergeInto(WorldItemSpawnController receiver)
    {
        _isMerging = true;

        if (_body != null)
        {
            _body.linearVelocity = Vector2.zero;
            _body.angularVelocity = 0f;
            _body.bodyType = RigidbodyType2D.Kinematic;
        }

        if (_col != null)
            _col.enabled = false;

        var startPos = transform.position;
        var startScale = transform.localScale;
        var elapsed = 0f;

        while (elapsed < _mergeDuration)
        {
            if (receiver == null)
                break;

            elapsed += Time.deltaTime;
            var t = Mathf.Clamp01(elapsed / _mergeDuration);
            var smoothT = Mathf.SmoothStep(0f, 1f, t);

            transform.position = Vector3.Lerp(startPos, receiver.transform.position, smoothT);
            transform.localScale = Vector3.Lerp(startScale, startScale * _mergeShrink, smoothT);
            yield return null;
        }

        if (receiver == null)
        {
            RestoreAfterMerge(startScale, false);
            yield break;
        }

        var receiverItem = receiver._worldItem.Item;
        var selfItem = _worldItem.Item;

        var freeSpace = Mathf.Max(0, receiverItem.Item.MaxStackSize - receiverItem.Amount);
        var toMove = Mathf.Min(freeSpace, selfItem.Amount);

        if (toMove > 0)
        {
            receiver._worldItem.SetItem(receiverItem.WithAmount(receiverItem.Amount + toMove));
            _worldItem.SetItem(selfItem.WithAmount(selfItem.Amount - toMove));
        }

        if (_worldItem.Item.IsEmpty)
        {
            Destroy(gameObject);
            yield break;
        }

        RestoreAfterMerge(startScale, true);
    }

    private void RestoreAfterMerge(Vector3 startScale, bool nudge)
    {
        transform.localScale = startScale;

        if (_col != null)
            _col.enabled = true;

        if (_body != null)
        {
            _body.bodyType = RigidbodyType2D.Dynamic;

            if (nudge)
            {
                var dir = Random.insideUnitCircle.normalized;
                _body.AddForce(dir * _leftoverItemNudgeImpulse, ForceMode2D.Impulse);
            }
        }

        _isMerging = false;
    }

    private void TryPickupInto(PlayerInventory inventory)
    {
        var currentItem = _worldItem.Item;
        if (currentItem.IsEmpty)
        {
            Destroy(gameObject);
            return;
        }

        inventory.TryAddItemToRange(currentItem, new SlotRange(0, inventory.Capacity), out var leftoverItem);
        ItemPickupFeedReporter.ReportAddedToInventory(currentItem, leftoverItem);

        if (leftoverItem.IsEmpty)
            Destroy(gameObject);
        else
            _worldItem.SetItem(leftoverItem);
    }
}
