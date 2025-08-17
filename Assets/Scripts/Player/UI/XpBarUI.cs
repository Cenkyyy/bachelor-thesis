using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class XpBarUI : MonoBehaviour, IStatBar, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] Image xpFillImage;
    [SerializeField] TMP_Text xpLevelText;

    private PlayerStatsSO _stats;
    private bool _onHovered;

    public void Initialize(PlayerStatsSO stats)
    {
        _stats = stats;
        UpdateBar();
    }

    public void UpdateBar()
    {
        if (_stats == null)
            return;

        // set current xp progress
        if (xpFillImage != null) 
        {
            xpFillImage.fillAmount = Mathf.Clamp01((float)_stats.currentXP / Mathf.Max(1, _stats.maxXP));
        }

        // set current level text
        if (xpLevelText != null)
        {
            if (_onHovered)
            {
                xpLevelText.text = $"{_stats.currentXP} / {_stats.maxXP}";
            }
            else
            {
                xpLevelText.text = _stats.currentLevel.ToString();
            }
        }
    }

    public void ShowOnHover()
    {
        if (_stats == null || xpLevelText == null || xpFillImage == null) 
            return;
        xpLevelText.text = $"{_stats.currentXP} / {_stats.maxXP}";
        _onHovered = true;
    }

    public void HideOnHover()
    {
        if (_stats == null || xpLevelText == null || xpFillImage == null) 
            return;
        xpLevelText.text = _stats.currentLevel.ToString();
        _onHovered = false;
    }

    public void OnPointerEnter(PointerEventData eventData) => ShowOnHover();
    public void OnPointerExit(PointerEventData eventData) => HideOnHover();
}
