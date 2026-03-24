using UnityEngine;

public static class DamageWordTextPopupUtility
{
    public static void ShowForTarget(ISpellTarget target, int damage, float multiplier, DamageWordTextPopupSettings settings)
    {
        if (target is not Component targetComponent)
            return;

        ShowForGameObject(targetComponent.gameObject, damage, multiplier, settings);
    }

    public static void ShowForGameObject(GameObject owner, int damage, float multiplier, DamageWordTextPopupSettings settings)
    {
        if (owner == null || settings == null || damage <= 0)
            return;

        var popupEmitter = owner.GetComponent<WorldTextPopupEmitter>();
        if (popupEmitter == null)
            popupEmitter = owner.AddComponent<WorldTextPopupEmitter>();

        popupEmitter.ShowMessage(settings.BuildMessage(damage, multiplier), settings.ResolveColor(multiplier), settings.CooldownSeconds);
    }
}
