using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// HUD bar view for player XP that switches between level and progress text on hover.
/// </summary>
public class XpBarView : StatBarViewBase, IPointerEnterHandler, IPointerExitHandler
{
    [Header("View References")]
    [SerializeField] private Image _xpFillImage;
    [SerializeField] private TMP_Text _xpLevelText;

    private bool _onHovered;

    protected override void Subscribe()
    {
        if (Data != null)
            Data.OnXPChanged += OnXPChanged;
    }

    protected override void Unsubscribe()
    {
        if (Data != null)
            Data.OnXPChanged -= OnXPChanged;
    }

    protected override void DrawInitial()
    {
        if (Data != null)
            OnXPChanged(Data.CurrentXP, Data.MaxXP, Data.CurrentLevel);
    }

    private void OnXPChanged(int currentXP, int maxXP, int level)
    {
        if (_xpFillImage != null)
            _xpFillImage.fillAmount = Mathf.Clamp01((float)currentXP / Mathf.Max(1, maxXP));

        if (_xpLevelText != null)
        {
            if (_onHovered)
                _xpLevelText.text = $"{currentXP} / {maxXP}";
            else
                _xpLevelText.text = level.ToString();
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (GameStateManager.Instance != null && GameStateManager.IsGamePaused)
            return;

        _onHovered = true;
        if (Data != null)
            OnXPChanged(Data.CurrentXP, Data.MaxXP, Data.CurrentLevel);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (GameStateManager.Instance != null && GameStateManager.IsGamePaused)
            return;

        _onHovered = false;
        if (Data != null)
            OnXPChanged(Data.CurrentXP, Data.MaxXP, Data.CurrentLevel);
    }
}
