using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HealthBarUI : StatBarBase
{
    [SerializeField] private Image _healthFillImage;
    [SerializeField] private TMP_Text _healthText;

    protected override void Subscribe()
    {
        if (Data != null)
        {
            Data.OnHealthChanged += OnHealthChanged;
        }
    }

    protected override void Unsubscribe()
    {
        if (Data != null)
        {
            Data.OnHealthChanged -= OnHealthChanged;
        }
    }

    protected override void DrawInitial()
    {
        if (Data != null)
        {
            OnHealthChanged(Data.CurrentHealth, Data.MaxHealth);
        }
    }

    private void OnHealthChanged(int current, int max)
    {
        // set current health bar's fill amount
        if (_healthFillImage != null)
        {
            _healthFillImage.fillAmount = Mathf.Clamp01((float)current / Mathf.Max(1, max));
        }

        // set current health text
        if (_healthText != null)
        {
            _healthText.text = current.ToString();
        }
    }
}
