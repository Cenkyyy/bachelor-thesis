using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HealthBarUI : MonoBehaviour, IStatBar
{
    [SerializeField] Image healthFillImage;
    [SerializeField] TMP_Text healthText;

    private PlayerDataSO _data;

    public void Initialize(PlayerDataSO data)
    {
        _data = data;
        UpdateBar();
    }

    public void UpdateBar()
    {
        if (_data == null)
            return;

        // set current health bar's fill amount
        if (healthFillImage != null)
        {
            healthFillImage.fillAmount = Mathf.Clamp01((float)_data.currentHealth / Mathf.Max(1, _data.maxHealth));
        }

        // set current health text
        if (healthText != null)
        {
            healthText.text = _data.currentHealth.ToString();
        }
    }
}
