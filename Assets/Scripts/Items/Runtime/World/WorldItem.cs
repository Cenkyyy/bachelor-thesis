using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D), typeof(Rigidbody2D), typeof(SpriteRenderer))]
public sealed class WorldItem : MonoBehaviour
{
    [field: Header("Prefab component references")]
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
