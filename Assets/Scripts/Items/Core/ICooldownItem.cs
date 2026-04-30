using System.Collections.Generic;

public interface ICooldownItem
{
    float CooldownSeconds { get; } 
    IReadOnlyList<ItemData> CooldownBlockedItems { get; }
    float GetCooldownSeconds();
}
