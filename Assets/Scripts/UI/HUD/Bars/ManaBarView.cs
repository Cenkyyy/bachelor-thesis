using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// HUD bar view that displays the player's current mana value.
/// </summary>
public class ManaBarView : StatBarViewBase
{
    [Header("View References")]
    [SerializeField] private Image _manaFillImage;
    [SerializeField] private TMP_Text _manaText;

    protected override void Subscribe()
    {
        if (Data != null)
            Data.OnManaChanged += OnManaChanged;
    }

    protected override void Unsubscribe()
    {
        if (Data != null)
            Data.OnManaChanged -= OnManaChanged;
    }

    protected override void DrawInitial()
    {
        if (Data != null)
            OnManaChanged(Data.CurrentMana, Data.MaxMana);
    }

    private void OnManaChanged(int current, int max)
    {
        if (_manaFillImage != null)
            _manaFillImage.fillAmount = Mathf.Clamp01((float)current / Mathf.Max(1, max));

        if (_manaText != null)
            _manaText.text = current.ToString();
    }
}
