using UnityEngine;

[CreateAssetMenu(menuName = "Items/Tool")]
public class ToolSO : ItemBaseSO, IUsable
{
    [Header(UIStrings.ItemSO_ToolInfo__Title)]
    public int durability;
    public string toolType;

    public ToolSO()
    {
        category = ItemCategory.Tool;
    }

    public void Use(GameObject user)
    {
        if (durability > 0)
        {
            Debug.Log($"{itemName} used by {user.name}. Type: {toolType}. Remaining durability: {durability}.");
            durability--;
        }
        else
        {
            Debug.Log($"{itemName} is broken and cannot be used.");
        }
    }

    public void Repair(int amount)
    {
        durability += amount;
        Debug.Log($"{itemName} repaired. New durability: {durability}.");
    }
}