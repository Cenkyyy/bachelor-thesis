using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HungerBarUI : MonoBehaviour, IStatBar
{
    [SerializeField] Image hungerFillImage;
    [SerializeField] TMP_Text hungerText;

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

        // set current hunger bar's fill amount
        if (hungerFillImage != null)
        {
            hungerFillImage.fillAmount = Mathf.Clamp01((float)_data.currentHunger / Mathf.Max(1, _data.maxHunger));
        }

        // set current hunger text
        if (hungerText != null)
        {
            hungerText.text = _data.currentHunger.ToString();
        }
    }
}
