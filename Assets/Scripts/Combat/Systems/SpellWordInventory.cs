using System;
using System.Collections.Generic;
using UnityEngine;

public class SpellWordInventory : MonoBehaviour
{
    [Header("Unlocked Words")]
    [SerializeField] private List<ModifierWord> unlockedModifiers = new();
    [SerializeField] private List<ElementWord> unlockedElements = new();
    [SerializeField] private List<FormWord> unlockedForms = new();

    public IReadOnlyList<ModifierWord> UnlockedModifiers => unlockedModifiers;
    public IReadOnlyList<ElementWord> UnlockedElements => unlockedElements;
    public IReadOnlyList<FormWord> UnlockedForms => unlockedForms;

    public event Action OnWordsChanged;

    public bool Unlock(ModifierWord word)
    {
        if (unlockedModifiers.Contains(word))
            return false;

        unlockedModifiers.Add(word);
        OnWordsChanged?.Invoke();
        return true;
    }

    public bool Unlock(ElementWord word)
    {
        if (unlockedElements.Contains(word))
            return false;

        unlockedElements.Add(word);
        OnWordsChanged?.Invoke();
        return true;
    }

    public bool Unlock(FormWord word)
    {
        if (unlockedForms.Contains(word))
            return false;

        unlockedForms.Add(word);
        OnWordsChanged?.Invoke();
        return true;
    }
}