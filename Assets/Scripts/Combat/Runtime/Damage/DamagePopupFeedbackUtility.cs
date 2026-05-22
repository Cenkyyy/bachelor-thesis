using UnityEngine;

public static class DamagePopupFeedbackUtility
{
    public static void ShowForTarget(ICombatTarget target, int damage, float multiplier, DamagePopupFeedbackSettings settings)
    {
        if (target is not Component targetComponent || targetComponent == null)
            return;

        ShowForGameObject(targetComponent.gameObject, damage, multiplier, settings);
    }

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
