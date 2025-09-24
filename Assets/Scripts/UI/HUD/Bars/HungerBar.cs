using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HungerBarUI : StatBarBase
{
    [SerializeField] private Image _hungerFillImage;
    [SerializeField] private TMP_Text _hungerText;

    protected override void Subscribe()
    {
        if (Data != null)
        {
            Data.OnHungerChanged += OnHungerChanged;
        }
    }

    protected override void Unsubscribe()
    {
        if (Data != null)
        {
            Data.OnHungerChanged -= OnHungerChanged;
        }
    }

    protected override void DrawInitial()
    {
        if (Data != null)
        {
            OnHungerChanged(Data.CurrentHunger, Data.MaxHunger);
        }
    }

    private void OnHungerChanged(int current, int max)
    {
        // set current hunger bar's fill amount
        if (_hungerFillImage != null)
        {
            _hungerFillImage.fillAmount = Mathf.Clamp01((float)current / Mathf.Max(1, max));
        }

        // set current hunger text
        if (_hungerText != null)
        {
            _hungerText.text = current.ToString();
        }
    }
}
