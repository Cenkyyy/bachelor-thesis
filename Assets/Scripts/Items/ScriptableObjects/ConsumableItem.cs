using UnityEngine;

[CreateAssetMenu(menuName = "Items/Consumable")]
public class ConsumableItem : Item, IUsable
{
    [field: SerializeField] public int RestoreHealth { get; private set; }
    [field: SerializeField] public int RestoreMana { get; private set; }

    public ConsumableItem()
    {
        Category = ItemCategory.Consumable;
    }

    public void Use(GameObject user)
    {
        Debug.Log($"{ItemName} used by {user.name}. Restores {RestoreHealth} HP, {RestoreMana} Mana.");
    }
}