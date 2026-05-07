using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MemoryXpBarView : StatBarViewBase, IPointerEnterHandler, IPointerExitHandler
{
    [Header("View References")]
    [SerializeField] private Image _memoryXpFillImage;
    [SerializeField] private TMP_Text _memoryXpLevelText;

    private bool _onHovered;

    protected override void Subscribe()
    {
        if (Data != null)
            Data.OnMemoryXPChanged += OnMemoryXPChanged;
    }

    protected override void Unsubscribe()
    {
        if (Data != null)
            Data.OnMemoryXPChanged -= OnMemoryXPChanged;
    }

    protected override void DrawInitial()
    {
        if (Data != null)
            OnMemoryXPChanged(Data.CurrentMemoryXP, Data.MaxMemoryXP, Data.CurrentMemoryLevel);
    }

    private void OnMemoryXPChanged(int currentXP, int maxXP, int level)
    {
        if (_memoryXpFillImage != null)
            _memoryXpFillImage.fillAmount = Mathf.Clamp01((float)currentXP / Mathf.Max(1, maxXP));

        if (_memoryXpLevelText != null)
        {
            if (_onHovered)
                _memoryXpLevelText.text = $"{currentXP} / {maxXP}";
            else
                _memoryXpLevelText.text = level.ToString();
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (GameStateManager.Instance != null && GameStateManager.IsGamePaused)
            return;

        _onHovered = true;
        if (Data != null)
            OnMemoryXPChanged(Data.CurrentMemoryXP, Data.MaxMemoryXP, Data.CurrentMemoryLevel);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (GameStateManager.Instance != null && GameStateManager.IsGamePaused)
            return;

        _onHovered = false;
        if (Data != null)
            OnMemoryXPChanged(Data.CurrentMemoryXP, Data.MaxMemoryXP, Data.CurrentMemoryLevel);
    }
}
