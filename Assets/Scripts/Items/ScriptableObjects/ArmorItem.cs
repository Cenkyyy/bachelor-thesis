using UnityEngine;

[CreateAssetMenu(menuName = "Items/Armor")]
public class ArmorItem : Item, IEquippable
{
    [field: SerializeField] public int DefensePoints { get; private set; }
    [field: SerializeField] public string ArmorType { get; private set; } // Jezek video

    public ArmorItem()
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