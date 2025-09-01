using UnityEngine;

[CreateAssetMenu(menuName = "Items/Consumable")]
public class ConsumableSO : ItemBaseSO, IUsable
{
    [Header(UIStrings.ItemSO_ConsumableInfo__Title)]
    [field: SerializeField] public int RestoreHealth { get; private set; }
    [field: SerializeField] public int RestoreMana { get; private set; }

    public ConsumableSO()
    {
        Category = ItemCategory.Consumable;
    }

    public void Use(GameObject user)
    {
        Debug.Log($"{ItemName} used by {user.name}. Restores {RestoreHealth} HP, {RestoreMana} Mana.");
    }
}