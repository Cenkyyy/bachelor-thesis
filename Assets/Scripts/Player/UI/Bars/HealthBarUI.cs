using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HealthBarUI : StatBarBase
{
    [SerializeField] Image healthFillImage;
    [SerializeField] TMP_Text healthText;

    protected override void Subscribe()
    {
        if (Data != null) 
            Data.OnHealthChanged += OnHealthChanged;
    }

    protected override void Unsubscribe()
    {
        if (Data != null)
            Data.OnHealthChanged -= OnHealthChanged;
    }

    protected override void DrawInitial()
    {
        if (Data != null)
            OnHealthChanged(Data.CurrentHealth, Data.MaxHealth);
    }

    private void OnHealthChanged(int current, int max)
    {
        // set current health bar's fill amount
        if (healthFillImage != null)
        {
            healthFillImage.fillAmount = Mathf.Clamp01((float)current / Mathf.Max(1, max));
        }

        // set current health text
        if (healthText != null)
        {
            healthText.text = current.ToString();
        }
    }
}
