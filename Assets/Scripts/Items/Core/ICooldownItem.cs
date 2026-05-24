using System.Collections.Generic;

/// <summary>
/// Contract for item data that will have some status effect and will require cooldown rules.
/// </summary>
public interface ICooldownItem
{
    float CooldownSeconds { get; } 
    IReadOnlyList<ItemData> CooldownBlockedItems { get; }
    float GetCooldownSeconds();
}
