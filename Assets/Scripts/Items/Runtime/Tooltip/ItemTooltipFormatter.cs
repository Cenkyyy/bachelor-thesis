using System;
using System.Globalization;

/// <summary>
/// Formats item values into short readable text for tooltips.
/// </summary>
public static class ItemTooltipFormatter
{
    public static string FormatRarity(ItemRarity rarity)
    {
        return InsertWordBoundaries(rarity.ToString());
    }

    public static string FormatStatName(ItemStatusEffectType statType)
    {
        return statType switch
        {
            ItemStatusEffectType.MaxHealth => "Max Health",
            ItemStatusEffectType.HealthRegen => "Health Regen",
            ItemStatusEffectType.MaxMana => "Max Mana",
            ItemStatusEffectType.ManaRegen => "Mana Regen",
            ItemStatusEffectType.SpellDamage => "Spell Damage",
            ItemStatusEffectType.MoveSpeed => "Move Speed",
            ItemStatusEffectType.Defence => "Defence",
            _ => InsertWordBoundaries(statType.ToString())
        };
    }

    public static string FormatModifierValue(float value)
    {
        var rounded = Math.Round(value, 2);
        var prefix = rounded >= 0f ? "+" : string.Empty;
        return $"{prefix}{rounded.ToString(CultureInfo.InvariantCulture)}";
    }

    public static string FormatFloat(float value)
    {
        var rounded = Math.Round(value, 2);
        return rounded.ToString(CultureInfo.InvariantCulture);
    }

    public static string FormatInt(int value)
    {
        return value.ToString(CultureInfo.InvariantCulture);
    }

    public static string FormatSeconds(float seconds)
    {
        var rounded = Math.Round(seconds, 2);
        return $"{rounded.ToString(CultureInfo.InvariantCulture)}s";
    }

    public static string FormatEnumValue(Enum enumValue)
    {
        return InsertWordBoundaries(enumValue.ToString());
    }

    private static string InsertWordBoundaries(string value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        return System.Text.RegularExpressions.Regex.Replace(value, "([a-z])([A-Z])", "$1 $2");
    }
}
