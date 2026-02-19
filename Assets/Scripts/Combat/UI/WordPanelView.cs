using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class WordPanelView : MonoBehaviour
{
    [SerializeField] private WordSlotView[] _slots;

    public int VisibleCount { get; private set; }

    public void Bind<TWord>(IReadOnlyList<TWord> words, Func<TWord, string> labelProvider)
    {
        VisibleCount = Mathf.Min(words.Count, _slots.Length);

        for (int i = 0; i < _slots.Length; i++)
        {
            if (i < VisibleCount)
            {
                _slots[i].Configure(i + 1, labelProvider(words[i]));
                continue;
            }

            _slots[i].Hide();
        }
    }

    public void SetPanelInteractable(bool interactable)
    {
        for (int i = 0; i < VisibleCount; i++)
            _slots[i].SetInteractable(interactable);
    }
}