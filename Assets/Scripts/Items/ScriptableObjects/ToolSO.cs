using UnityEngine;

[CreateAssetMenu(menuName = "Items/Tool")]
public class ToolSO : ItemBaseSO, IUsable
{
    [Header(UIStrings.ItemSO_ToolInfo__Title)]
    [field: SerializeField] public int Durability { get; private set; }
    [field: SerializeField] public string ToolType { get; private set; }

    public ToolSO()
    {
        Category = ItemCategory.Tool;
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