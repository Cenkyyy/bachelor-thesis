using UnityEngine;

[CreateAssetMenu(menuName = "Items/Weapon")]
public class WeaponSO : ItemBaseSO
{
    [Header(UIStrings.ItemSO_WeaponInfo__Title)]
    /// <summary>
    /// The damage dealt by the weapon.
    /// </summary>
    public int damage;
    /// <summary>
    /// The range of the weapon.
    /// </summary>
    public float range;
    /// <summary>
    /// The attack speed of the weapon.
    /// </summary>
    public float attackSpeed;

    /// <summary>
    /// Initializes a new instance of the WeaponSO class.
    /// </summary>
    public WeaponSO()
    {
        category = ItemCategory.Weapon;
    }
}