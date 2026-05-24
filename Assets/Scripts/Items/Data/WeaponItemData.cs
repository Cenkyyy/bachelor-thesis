using UnityEngine;

/// <summary>
/// Item data for weapons, including combat values and weapon progression tier.
/// </summary>
[CreateAssetMenu(menuName = "Items/Weapon Item")]
public class WeaponItemData : ItemData
{
    [field: Header("Weapon Settings")]
    [field: SerializeField] public WeaponType Archetype { get; private set; } = WeaponType.Staff;
    [field: SerializeField] public WeaponTier Tier { get; private set; } = WeaponTier.Wooden;

    [field: Header("Combat Settings")]
    [field: SerializeField] public int Damage { get; private set; } = 5;
    [field: SerializeField] public float Range { get; private set; } = 4f;
    [field: SerializeField] public float AttackSpeed { get; private set; } = 1f;
    [field: SerializeField] public int ManaCost { get; private set; }

    protected override ItemType? ExpectedCategory => ItemType.Weapon;

    protected override void OnValidate()
    {
        base.OnValidate();

        if (Damage < 0)
            Damage = 0;

        if (Range < 0f)
            Range = 0f;

        if (AttackSpeed < 0f)
            AttackSpeed = 0f;

        if (ManaCost < 0)
            ManaCost = 0;
    }
}
