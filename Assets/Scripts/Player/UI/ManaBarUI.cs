using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ManaBarUI : MonoBehaviour, IStatBar
{
    [SerializeField] Image manaFillImage;
    [SerializeField] TMP_Text manaText;

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

        // set current mana bar's fill amount
        if (manaFillImage != null)
        {
            manaFillImage.fillAmount = Mathf.Clamp01((float)_stats.currentMana / Mathf.Max(1, _stats.maxMana));
        }

        // set current mana text
        if (manaText != null)
        {
            manaText.text = _stats.currentMana.ToString();
        }
    }
}
