using System;

[Flags]
public enum EquipmentTier
{
    Wooden = 1 << 0,
    Copper = 1 << 1,
    Celestine = 1 << 2,
    Topaz = 1 << 3,
    Amethyst = 1 << 4
}
