using UnityEngine;

/// <summary>
/// Utility for emitting formatted damage popup feedback from combat targets or GameObjects.
/// </summary>
public static class DamagePopupFeedbackUtility
{
    /// <summary>
    /// Shows a damage popup on the GameObject that owns the combat target component.
    /// </summary>
    public static void ShowForTarget(ICombatTarget target, int damage, float multiplier, DamagePopupFeedbackSettings settings)
    {
        if (target is not Component targetComponent || targetComponent == null)
            return;

        ShowForGameObject(targetComponent.gameObject, damage, multiplier, settings);
    }

    /// <summary>
    /// Shows a damage popup on the provided GameObject, adding a popup controller if needed.
    /// </summary>
    public static void ShowForGameObject(GameObject owner, int damage, float multiplier, DamagePopupFeedbackSettings settings)
    {
        if (owner == null || settings == null || damage <= 0)
            return;

        var popupEmitter = owner.GetComponent<WorldTextPopupController>();
        if (popupEmitter == null)
            popupEmitter = owner.AddComponent<WorldTextPopupController>();

        popupEmitter.ShowMessage(settings.BuildMessage(damage, multiplier), settings.ResolveColor(multiplier), settings.CooldownSeconds);
    }
}
