using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class ItemStatusEffectEntryView : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Image _iconImage;
    [SerializeField] private Image _cooldownOverlay;

    public void SetVisual(Sprite icon, float cooldownFill01)
    {
        if (_iconImage != null)
            ImageIconUtility.SetIcon(_iconImage, icon);

        if (_cooldownOverlay == null)
            return;

        var clampedFill = Mathf.Clamp01(cooldownFill01);
        var showOverlay = clampedFill > 0f;

        _cooldownOverlay.gameObject.SetActive(showOverlay);
        if (showOverlay)
            _cooldownOverlay.fillAmount = 1f - clampedFill;
    }
}
