using UnityEngine;

[CreateAssetMenu(menuName = "Items/Armor")]
public class ArmorSO : ItemBaseSO, IEquippable
{
    [Header(UIStrings.ItemSO_ArmorInfo__Title)]
    [field: SerializeField] public int DefensePoints { get; private set; }
    [field: SerializeField] public string ArmorType { get; private set; } // Jezek video

    public ArmorSO()
    {
        Category = ItemCategory.Armor;
    }

    public void Equip(GameObject user)
    {
        Debug.Log($"{ItemName} equipped by {user.name}. Provides {DefensePoints} defense points.");
    }

    public void Unequip(GameObject user)
    {
        Debug.Log($"{ItemName} unequipped by {user.name}.");
    }
}