using UnityEngine;

/// <summary>
/// Utility for resolving combat target capabilities and applying direct spell damage.
/// </summary>
public static class SpellCombatTargetUtility
{
    /// <summary>
    /// Resolves a combat target from a collider or one of its parents.
    /// </summary>
    public static bool TryGetCombatTarget(Collider2D collider, out ICombatTarget target)
    {
        target = collider != null ? collider.GetComponentInParent<ICombatTarget>() : null;
        return target != null && target.IsAlive;
    }

    /// <summary>
    /// Resolves a status-effect target from a combat target component.
    /// </summary>
    public static IStatusEffectTarget GetStatusEffectTarget(ICombatTarget target)
    {
        if (target is not Component targetComponent || targetComponent == null)
            return null;

        return targetComponent.GetComponentInParent<IStatusEffectTarget>();
    }

    /// <summary>
    /// Applies rounded damage to a target through IDamageable when the target can receive damage.
    /// </summary>
    public static bool TryApplyDamage(ICombatTarget target, float amount, object source, out int appliedDamage)
    {
        appliedDamage = 0;

        if (!TryGetDamageable(target, out IDamageable damageable))
            return false;

        int roundedDamage = Mathf.RoundToInt(amount);
        if (roundedDamage <= 0)
            roundedDamage = 1;

        damageable.ReceiveDamage(roundedDamage, source);
        appliedDamage = roundedDamage;
        return true;
    }

    private static bool TryGetDamageable(ICombatTarget target, out IDamageable damageable)
    {
        damageable = null;

        if (target is not Component targetComponent || targetComponent == null)
            return false;

        damageable = targetComponent.GetComponentInParent<IDamageable>();
        return damageable != null && damageable.CanReceiveDamage;
    }
}
