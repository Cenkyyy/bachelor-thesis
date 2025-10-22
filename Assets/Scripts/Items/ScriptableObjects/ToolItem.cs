using UnityEngine;

[CreateAssetMenu(menuName = "Items/Tool")]
public class ToolItem : Item, IUsable
{
    [field: SerializeField] public int Durability { get; private set; }
    [field: SerializeField] public string ToolType { get; private set; }

    public ToolItem()
    {
        Category = ItemType.Tool;
    }

    public void Use(GameObject user)
    {
        if (Durability > 0)
        {
            Debug.Log($"{ItemName} used by {user.name}. Type: {ToolType}. Remaining durability: {Durability}.");
            Durability--;
        }
        else
        {
            Debug.Log($"{ItemName} is broken and cannot be used.");
        }
    }

    public void Repair(int amount)
    {
        Durability += amount;
        Debug.Log($"{ItemName} repaired. New durability: {Durability}.");
    }
}