using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ManaBarUI : MonoBehaviour, IStatBar
{
    [SerializeField] Image manaFillImage;
    [SerializeField] TMP_Text manaText;

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

        // set current mana bar's fill amount
        if (manaFillImage != null)
        {
            manaFillImage.fillAmount = Mathf.Clamp01((float)_data.currentMana / Mathf.Max(1, _data.maxMana));
        }

        // set current mana text
        if (manaText != null)
        {
            manaText.text = _data.currentMana.ToString();
        }
    }
}
