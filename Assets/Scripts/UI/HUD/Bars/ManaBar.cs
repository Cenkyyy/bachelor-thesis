using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ManaBarUI : StatBarBase
{
    [SerializeField] private Image _manaFillImage;
    [SerializeField] private TMP_Text _manaText;

    protected override void Subscribe()
    {
        if (Data != null)
        {
            Data.OnManaChanged += OnManaChanged;
        }
    }

    protected override void Unsubscribe()
    {
        if (Data != null)
        {
            Data.OnManaChanged -= OnManaChanged;
        }
    }

    protected override void DrawInitial()
    {
        if (Data != null)
        {
            OnManaChanged(Data.CurrentMana, Data.MaxMana);
        }
    }

    private void OnManaChanged(int current, int max)
    {
        // set current mana bar's fill amount
        if (_manaFillImage != null)
        {
            _manaFillImage.fillAmount = Mathf.Clamp01((float)current / Mathf.Max(1, max));
        }

        // set current mana text
        if (_manaText != null)
        {
            _manaText.text = current.ToString();
        }
    }
}
