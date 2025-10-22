using UnityEngine;

[CreateAssetMenu(menuName = "Items/Weapon")]
public class WeaponItem : Item
{

    [field: SerializeField] public int Damage { get; private set; }
    [field: SerializeField] public float Range { get; private set; }
    [field: SerializeField] public float AttackSpeed { get; private set; }

    public WeaponItem()
    {
        Category = ItemType.Weapon;
    }
}