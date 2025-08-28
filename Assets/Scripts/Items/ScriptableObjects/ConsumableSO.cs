using UnityEngine;

[CreateAssetMenu(menuName = "Items/Consumable")]
public class ConsumableSO : ItemBaseSO, IUsable
{
    [Header(UIStrings.ItemSO_ConsumableInfo__Title)]
    public int restoreHealth;
    public int restoreMana;

    public ConsumableSO()
    {
        category = ItemCategory.Consumable;
    }

    public void Use(GameObject user)
    {
        Debug.Log($"{itemName} used by {user.name}. Restores {restoreHealth} HP, {restoreMana} Mana.");
    }
}