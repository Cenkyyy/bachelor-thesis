using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public sealed class WorldItem : MonoBehaviour
{
    [Header("Visuals")]
    [SerializeField] private SpriteRenderer _iconRenderer;

    public InventoryItem Item { get; private set; } = InventoryItem.Empty;


    public void Initialize(InventoryItem item) 
    {
        Item = item;
        Render();
    }

    public void SetItem(InventoryItem item)
    {
        Item = item;
        Render();
    }

    public bool TryGetRigidbody(out Rigidbody2D body)
    {
        return TryGetComponent(out body);
    }

    public bool TryGetCollider(out Collider2D col)
    {
        return TryGetComponent(out col);
    }

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
}
