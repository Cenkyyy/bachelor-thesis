using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HungerBarUI : MonoBehaviour, IStatBar
{
    [SerializeField] Image hungerFillImage;
    [SerializeField] TMP_Text hungerText;

    private PlayerStatsSO _stats;

    public void Initialize(PlayerStatsSO stats)
    {
        _stats = stats;
        UpdateBar();
    }

    public void UpdateBar()
    {
        if (_stats == null)
            return;

        // set current hunger bar's fill amount
        if (hungerFillImage != null)
        {
            hungerFillImage.fillAmount = Mathf.Clamp01((float)_stats.currentHunger / Mathf.Max(1, _stats.maxHunger));
        }

        // set current hunger text
        if (hungerText != null)
        {
            hungerText.text = _stats.currentHunger.ToString();
        }
    }
}
