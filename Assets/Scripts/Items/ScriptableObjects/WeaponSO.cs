using UnityEngine;

[CreateAssetMenu(menuName = "Items/Weapon")]
public class WeaponSO : ItemBaseSO
{
    [Header(UIStrings.ItemSO_WeaponInfo__Title)]

    [field: SerializeField] public int Damage { get; private set; }
    [field: SerializeField] public float Range { get; private set; }
    [field: SerializeField] public float AttackSpeed { get; private set; }

    public WeaponSO()
    {
        Category = ItemCategory.Weapon;
    }
}