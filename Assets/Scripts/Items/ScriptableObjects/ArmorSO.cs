using UnityEngine;

[CreateAssetMenu(menuName = "Items/Armor")]
public class ArmorSO : ItemBaseSO, IEquippable
{
    [Header(UIStrings.ItemSO_ArmorInfo__Title)]
    public int defensePoints;
    public string armorType; // watch Jezek video

    public ArmorSO()
    {
        category = ItemCategory.Armor;
    }

    public void Equip(GameObject user)
    {
        Debug.Log($"{itemName} equipped by {user.name}. Provides {defensePoints} defense points.");
    }

    public void Unequip(GameObject user)
    {
        Debug.Log($"{itemName} unequipped by {user.name}.");
    }
}