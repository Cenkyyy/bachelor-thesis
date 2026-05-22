using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Displays one selectable spell word slot in the casting UI.
/// </summary>
public sealed class SpellWordSlotView : MonoBehaviour
{
    [Header("Labels")]
    [SerializeField] private TMP_Text _indexLabel;
    [SerializeField] private TMP_Text _wordLabel;

    [Header("Visuals")]
    [SerializeField] private Image _background;
    [SerializeField] private Color _enabledColor = Color.white;
    [SerializeField] private Color _disabledColor = new(0.4f, 0.4f, 0.4f, 1f);

    public void Configure(int oneBasedIndex, string label)
    {
        gameObject.SetActive(true);

        _indexLabel.text = oneBasedIndex.ToString();
        _wordLabel.text = label;
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    public void SetInteractable(bool interactable)
    {
        _background.color = interactable ? _enabledColor : _disabledColor;
    }
}
