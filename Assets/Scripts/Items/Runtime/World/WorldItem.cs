using UnityEngine;

/// <summary>
/// Runtime world representation of an <see cref="InventoryItem"/> stack item.
/// This component stores the item type and its amount that has been dropped into the world,
/// and visually represents it by rendering the item's icon sprite.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D), typeof(Rigidbody2D), typeof(SpriteRenderer))]
public sealed class WorldItem : MonoBehaviour
{
    [field: Header("Component references")]
    [field: SerializeField] public Collider2D Collider { get; private set; }
    [field: SerializeField] public Rigidbody2D Rigidbody { get; private set; }
    [field: SerializeField] public SpriteRenderer SpriteRenderer { get; private set; }

    public InventoryItem Item { get; private set; } = InventoryItem.Empty;

    public void SetItem(InventoryItem item)
    {
        Item = item;
        Render();
    }

    private void Render()
    {
        if (Item.IsEmpty || Item.Item == null)
        {
            SpriteRenderer.enabled = false;
            return;
        }

        SpriteRenderer.enabled = true;
        SpriteRenderer.sprite = Item.Item.Icon;
    }
}
