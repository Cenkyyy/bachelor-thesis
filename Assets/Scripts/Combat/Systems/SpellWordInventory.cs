using System;
using System.Collections.Generic;
using UnityEngine;

public class SpellWordInventory : MonoBehaviour
{
    [Header("Unlocked Words")]
    [SerializeField] private List<ModifierWord> _unlockedModifiers = new();
    [SerializeField] private List<ElementWord> _unlockedElements = new();
    [SerializeField] private List<FormWord> _unlockedForms = new();

    public IReadOnlyList<ModifierWord> UnlockedModifiers => _unlockedModifiers;
    public IReadOnlyList<ElementWord> UnlockedElements => _unlockedElements;
    public IReadOnlyList<FormWord> UnlockedForms => _unlockedForms;

    public event Action OnWordsChanged;

    private void Awake()
    {
        SortUnlockedWords();
    }

    public bool Unlock(ModifierWord word)
    {
        if (_unlockedModifiers.Contains(word))
            return false;

        _unlockedModifiers.Add(word);
        SortUnlockedWords();
        OnWordsChanged?.Invoke();
        return true;
    }

    public bool Unlock(ElementWord word)
    {
        if (_unlockedElements.Contains(word))
            return false;

        _unlockedElements.Add(word);
        SortUnlockedWords();
        OnWordsChanged?.Invoke();
        return true;
    }

    public bool Unlock(FormWord word)
    {
        if (_unlockedForms.Contains(word))
            return false;

        _unlockedForms.Add(word);
        SortUnlockedWords();
        OnWordsChanged?.Invoke();
        return true;
    }

    private void SortUnlockedWords()
    {
        _unlockedModifiers.Sort();
        _unlockedElements.Sort();
        _unlockedForms.Sort();
    }
}