using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class WordPanelView : MonoBehaviour
{
    [SerializeField] private WordSlotView[] slots;

    public int VisibleCount { get; private set; }

    public void Bind<TWord>(IReadOnlyList<TWord> words, Func<TWord, string> labelProvider)
    {
        VisibleCount = Mathf.Min(words.Count, slots.Length);

        for (int i = 0; i < slots.Length; i++)
        {
            if (i < VisibleCount)
            {
                slots[i].Configure(i + 1, labelProvider(words[i]));
                continue;
            }

            slots[i].Hide();
        }
    }

    public void SetPanelInteractable(bool interactable)
    {
        for (int i = 0; i < VisibleCount; i++)
            slots[i].SetInteractable(interactable);
    }
}