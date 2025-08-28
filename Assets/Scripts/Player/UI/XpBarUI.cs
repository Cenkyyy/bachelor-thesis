using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class XpBarUI : MonoBehaviour, IStatBar, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] Image xpFillImage;
    [SerializeField] TMP_Text xpLevelText;

    private PlayerDataSO _data;
    private bool _onHovered;

    public void Initialize(PlayerDataSO data)
    {
        _data = data;
        UpdateBar();
    }

    public void UpdateBar()
    {
        if (_data == null)
            return;

        // set current xp progress
        if (xpFillImage != null) 
        {
            xpFillImage.fillAmount = Mathf.Clamp01((float)_data.currentXP / Mathf.Max(1, _data.maxXP));
        }

        // set current level text
        if (xpLevelText != null)
        {
            if (_onHovered)
            {
                xpLevelText.text = $"{_data.currentXP} / {_data.maxXP}";
            }
            else
            {
                xpLevelText.text = _data.currentLevel.ToString();
            }
        }
    }

    public void ShowOnHover()
    {
        if (_data == null || xpLevelText == null || xpFillImage == null) 
            return;
        xpLevelText.text = $"{_data.currentXP} / {_data.maxXP}";
        _onHovered = true;
    }

    public void HideOnHover()
    {
        if (_data == null || xpLevelText == null || xpFillImage == null) 
            return;
        xpLevelText.text = _data.currentLevel.ToString();
        _onHovered = false;
    }

    public void OnPointerEnter(PointerEventData eventData) => ShowOnHover();
    public void OnPointerExit(PointerEventData eventData) => HideOnHover();
}
