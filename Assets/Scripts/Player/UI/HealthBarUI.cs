using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HealthBarUI : MonoBehaviour, IStatBar
{
    [SerializeField] Image healthFillImage;
    [SerializeField] TMP_Text healthText;

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

        // set current health bar's fill amount
        if (healthFillImage != null)
        {
            healthFillImage.fillAmount = Mathf.Clamp01((float)_stats.currentHealth / Mathf.Max(1, _stats.maxHealth));
        }

        // set current health text
        if (healthText != null)
        {
            healthText.text = _stats.currentHealth.ToString();
        }
    }
}
