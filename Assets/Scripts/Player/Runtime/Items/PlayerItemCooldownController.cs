using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tracks item cooldown end times for player-used items and linked blocked items.
/// </summary>
[DisallowMultipleComponent]
public sealed class PlayerItemCooldownController : MonoBehaviour
{
    private readonly Dictionary<ItemData, float> _itemCooldownEndTimes = new();

    public void ClearAllCooldowns()
    {
        _itemCooldownEndTimes.Clear();
    }

    public bool IsOnCooldown(ItemData item)
    {
        if (!TryGetCooldownDuration(item, out _))
            return false;

        if (!_itemCooldownEndTimes.TryGetValue(item, out var cooldownEndTime))
            return false;

        if (cooldownEndTime <= Time.time)
        {
            _itemCooldownEndTimes.Remove(item);
            return false;
        }

        return true;
    }

    public bool TryStartCooldown(ItemData item)
    {
        if (!TryGetCooldownDuration(item, out var cooldownSeconds))
            return false;

        var cooldownEndTime = Time.time + cooldownSeconds;
        SetCooldown(item, cooldownEndTime);

        if (item is not ICooldownItem cooldownItem || cooldownItem.CooldownBlockedItems == null)
            return true;

        var blockedItems = cooldownItem.CooldownBlockedItems;
        for (int i = 0; i < blockedItems.Count; i++)
        {
            var blockedItem = blockedItems[i];
            if (blockedItem == null)
                continue;

            SetCooldown(blockedItem, cooldownEndTime);
        }

        return true;
    }

    public bool TryGetItemCooldown01(ItemData item, out float remainingNormalized)
    {
        remainingNormalized = 0f;
        if (!TryGetCooldownDuration(item, out var cooldownSeconds))
            return false;

        if (!_itemCooldownEndTimes.TryGetValue(item, out var cooldownEndTime))
            return false;

        var remainingSeconds = cooldownEndTime - Time.time;
        if (remainingSeconds <= 0f)
        {
            _itemCooldownEndTimes.Remove(item);
            return false;
        }

        remainingNormalized = Mathf.Clamp01(remainingSeconds / cooldownSeconds);
        return true;
    }

    private void SetCooldown(ItemData item, float cooldownEndTime)
    {
        if (_itemCooldownEndTimes.TryGetValue(item, out var existingEndTime) && existingEndTime > cooldownEndTime)
            return;

        _itemCooldownEndTimes[item] = cooldownEndTime;
    }

    private static bool TryGetCooldownDuration(ItemData item, out float cooldownSeconds)
    {
        cooldownSeconds = 0f;
        if (item is not ICooldownItem cooldownSource)
            return false;

        cooldownSeconds = cooldownSource.GetCooldownSeconds();
        return cooldownSeconds > 0f;
    }
}
