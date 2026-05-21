using System;
using System.Collections.Generic;
using UnityEngine;

public class SpellWordInventory : MonoBehaviour
{
    [Header("Unlocked Words")]
    [SerializeField] private List<ModifierWordType> _unlockedModifiers = new();
    [SerializeField] private List<ElementWordType> _unlockedElements = new();
    [SerializeField] private List<FormWordType> _unlockedForms = new();

    public IReadOnlyList<ModifierWordType> UnlockedModifiers => _unlockedModifiers;
    public IReadOnlyList<ElementWordType> UnlockedElements => _unlockedElements;
    public IReadOnlyList<FormWordType> UnlockedForms => _unlockedForms;

    public event Action OnWordsInitialized;
    public event Action OnWordsChanged;

    private bool _hasInitializedWords;

    private void OnValidate()
    {
        SortUnlockedWords();
    }

    private void OnEnable()
    {
        SortUnlockedWords();
    }

    public void SetUnlockedWords(IReadOnlyList<ModifierWordType> modifiers, IReadOnlyList<ElementWordType> elements, IReadOnlyList<FormWordType> forms)
    {
        _unlockedModifiers.Clear();
        _unlockedElements.Clear();
        _unlockedForms.Clear();

        if (modifiers != null)
            _unlockedModifiers.AddRange(modifiers);

        if (elements != null)
            _unlockedElements.AddRange(elements);

        if (forms != null)
            _unlockedForms.AddRange(forms);

        SortUnlockedWords();
        if (!_hasInitializedWords)
        {
            _hasInitializedWords = true;
            OnWordsInitialized?.Invoke();
        }

        OnWordsChanged?.Invoke();
    }

    public bool Unlock(ModifierWordType word)
    {
        if (_unlockedModifiers.Contains(word))
            return false;

        _unlockedModifiers.Add(word);
        SortUnlockedWords();
        OnWordsChanged?.Invoke();
        return true;
    }

    public bool Unlock(ElementWordType word)
    {
        if (_unlockedElements.Contains(word))
            return false;

        _unlockedElements.Add(word);
        SortUnlockedWords();
        OnWordsChanged?.Invoke();
        return true;
    }

    public bool Unlock(FormWordType word)
    {
        if (_unlockedForms.Contains(word))
            return false;

        _unlockedForms.Add(word);
        SortUnlockedWords();
        OnWordsChanged?.Invoke();
        return true;
    }

    public bool IsUnlocked(WordData word)
    {
        return word switch
        {
            ModifierWordData modifier => _unlockedModifiers.Contains(modifier.Modifier),
            ElementWordData element => _unlockedElements.Contains(element.Element),
            FormWordData form => _unlockedForms.Contains(form.Form),
            _ => false
        };
    }

    public bool TryUnlock(WordData word)
    {
        return word switch
        {
            ModifierWordData modifier => Unlock(modifier.Modifier),
            ElementWordData element => Unlock(element.Element),
            FormWordData form => Unlock(form.Form),
            _ => false
        };
    }

    private void SortUnlockedWords()
    {
        _unlockedModifiers.Sort();
        _unlockedElements.Sort();
        _unlockedForms.Sort();
    }
}
