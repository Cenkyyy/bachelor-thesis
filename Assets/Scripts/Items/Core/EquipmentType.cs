using System;

/// <summary>
/// Defines equipment slot flags used by equipment items and slot validation.
/// </summary>
[Flags]
public enum EquipmentType
{
    Helmet = 1 << 0,
    Chest = 1 << 1,
    Legs = 1 << 2,
    Boots = 1 << 3,
    Necklace = 1 << 4,
    RingLeft = 1 << 5,
    RingRight = 1 << 6,
    Amulet = 1 << 7
}
