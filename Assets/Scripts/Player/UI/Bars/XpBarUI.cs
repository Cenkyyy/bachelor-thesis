using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class XpBarUI : StatBarBase, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] Image xpFillImage;
    [SerializeField] TMP_Text xpLevelText;

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
        // set current xp progress
        if (xpFillImage != null)
            xpFillImage.fillAmount = Mathf.Clamp01((float)currentXP / Mathf.Max(1, maxXP));

        // set current level text
        if (xpLevelText != null)
        {
            if (_onHovered)
            {
                xpLevelText.text = $"{currentXP} / {maxXP}";
            }
            else
            {
                xpLevelText.text = level.ToString();
            }
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _onHovered = true;
        if (Data != null) OnXPChanged(Data.CurrentXP, Data.MaxXP, Data.CurrentLevel);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _onHovered = false;
        if (Data != null) OnXPChanged(Data.CurrentXP, Data.MaxXP, Data.CurrentLevel);
    }
}
