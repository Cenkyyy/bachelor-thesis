using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tracks the player's unlocked spell words at runtime and exposes available locked words for purchasing.
/// </summary>
public sealed class PlayerSpellWordInventory : MonoBehaviour
{
    [Header("Definitions")]
    [SerializeField] private CombatWordsData _combatWordsData;

    [Header("Unlocked Words")]
    [SerializeField] private List<ModifierWordData> _unlockedModifiers = new();
    [SerializeField] private List<ElementWordData> _unlockedElements = new();
    [SerializeField] private List<FormWordData> _unlockedForms = new();

    public IReadOnlyList<ModifierWordData> UnlockedModifiers => _unlockedModifiers;
    public IReadOnlyList<ElementWordData> UnlockedElements => _unlockedElements;
    public IReadOnlyList<FormWordData> UnlockedForms => _unlockedForms;
    public bool HasInitializedWords => _hasInitializedWords;

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

    private void Start()
    {
        InitializeUnlockedWords();
    }

    public void InitializeUnlockedWords()
    {
        SortUnlockedWords();

        if (_hasInitializedWords)
            return;

        _hasInitializedWords = true;
        OnWordsInitialized?.Invoke();
        OnWordsChanged?.Invoke();
    }

    public void SetUnlockedWords(IReadOnlyList<ModifierWordData> modifiers, IReadOnlyList<ElementWordData> elements, IReadOnlyList<FormWordData> forms)
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

    public bool Unlock(ModifierWordData word)
    {
        if (word == null || IsUnlocked(word))
            return false;

        _unlockedModifiers.Add(word);
        SortWordsByDisplayName(_unlockedModifiers);
        OnWordsChanged?.Invoke();
        return true;
    }

    public bool Unlock(ElementWordData word)
    {
        if (word == null || IsUnlocked(word))
            return false;

        _unlockedElements.Add(word);
        SortWordsByDisplayName(_unlockedElements);
        OnWordsChanged?.Invoke();
        return true;
    }

    public bool Unlock(FormWordData word)
    {
        if (word == null || IsUnlocked(word))
            return false;

        _unlockedForms.Add(word);
        SortWordsByDisplayName(_unlockedForms);
        OnWordsChanged?.Invoke();
        return true;
    }

    public bool IsUnlocked(WordData word)
    {
        return word switch
        {
            ModifierWordData modifier => ContainsModifier(modifier),
            ElementWordData element => ContainsElement(element),
            FormWordData form => ContainsForm(form),
            _ => false
        };
    }

    public void GetAvailableWords(List<WordData> resultBuffer)
    {
        resultBuffer.Clear();

        if (_combatWordsData == null)
            return;

        AddAvailableWords(_combatWordsData.AllModifiers, resultBuffer);
        AddAvailableWords(_combatWordsData.AllElements, resultBuffer);
        AddAvailableWords(_combatWordsData.AllForms, resultBuffer);
    }

    public bool TryUnlock(WordData word)
    {
        return word switch
        {
            ModifierWordData modifier => Unlock(modifier),
            ElementWordData element => Unlock(element),
            FormWordData form => Unlock(form),
            _ => false
        };
    }

    private void AddAvailableWords<TWordData>(IReadOnlyList<TWordData> words, List<WordData> resultBuffer)
        where TWordData : WordData
    {
        if (words == null)
            return;

        for (var i = 0; i < words.Count; i++)
        {
            TWordData word = words[i];
            if (word == null || IsUnlocked(word))
                continue;

            resultBuffer.Add(word);
        }
    }

    private bool ContainsModifier(ModifierWordData word)
    {
        if (word == null)
            return false;

        for (var i = 0; i < _unlockedModifiers.Count; i++)
        {
            ModifierWordData unlockedWord = _unlockedModifiers[i];
            if (unlockedWord != null && unlockedWord.Type == word.Type)
                return true;
        }

        return false;
    }

    private bool ContainsElement(ElementWordData word)
    {
        if (word == null)
            return false;

        for (var i = 0; i < _unlockedElements.Count; i++)
        {
            ElementWordData unlockedWord = _unlockedElements[i];
            if (unlockedWord != null && unlockedWord.Type == word.Type)
                return true;
        }

        return false;
    }

    private bool ContainsForm(FormWordData word)
    {
        if (word == null)
            return false;

        for (var i = 0; i < _unlockedForms.Count; i++)
        {
            FormWordData unlockedWord = _unlockedForms[i];
            if (unlockedWord != null && unlockedWord.Type == word.Type)
                return true;
        }

        return false;
    }

    private void SortUnlockedWords()
    {
        SortWordsByDisplayName(_unlockedModifiers);
        SortWordsByDisplayName(_unlockedElements);
        SortWordsByDisplayName(_unlockedForms);
    }

    private static void SortWordsByDisplayName<TWordData>(List<TWordData> words)
        where TWordData : WordData
    {
        words.Sort(CompareWordsByDisplayName);
    }

    private static int CompareWordsByDisplayName(WordData first, WordData second)
    {
        if (ReferenceEquals(first, second))
            return 0;

        if (first == null)
            return 1;

        if (second == null)
            return -1;

        int nameComparison = string.Compare(first.DisplayName, second.DisplayName, StringComparison.Ordinal);
        if (nameComparison != 0)
            return nameComparison;

        return string.Compare(first.name, second.name, StringComparison.Ordinal);
    }
}
