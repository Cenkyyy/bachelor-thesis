using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WordSlotView : MonoBehaviour
{
    [SerializeField] private TMP_Text indexLabel;
    [SerializeField] private TMP_Text wordLabel;
    [SerializeField] private Image background;
    [SerializeField] private Color enabledColor = Color.white;
    [SerializeField] private Color disabledColor = new(0.4f, 0.4f, 0.4f, 1f);

    public void Configure(int oneBasedIndex, string label)
    {
        gameObject.SetActive(true);

        indexLabel.text = oneBasedIndex.ToString();
        wordLabel.text = label;
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    public void SetInteractable(bool interactable)
    {
        background.color = interactable ? enabledColor : disabledColor;
    }
}