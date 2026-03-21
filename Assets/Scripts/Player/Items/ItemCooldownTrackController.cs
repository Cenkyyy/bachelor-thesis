using System.Collections.Generic;
using UnityEngine;

public sealed class ItemCooldownTrackController : MonoBehaviour
{
    private readonly Dictionary<ItemData, float> _itemCooldownEndTimes = new();

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

        _itemCooldownEndTimes[item] = Time.time + cooldownSeconds;
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

    private static bool TryGetCooldownDuration(ItemData item, out float cooldownSeconds)
    {
        cooldownSeconds = 0f;
        if (item is not ICooldownItem cooldownSource)
            return false;

        cooldownSeconds = cooldownSource.GetCooldownSeconds();
        return cooldownSeconds > 0f;
    }
}
