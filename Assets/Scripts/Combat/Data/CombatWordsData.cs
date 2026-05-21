using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Combat/Word Data", fileName = "CombatWordsData")]
public sealed class CombatWordsData : ScriptableObject
{
    [SerializeField] private List<ModifierWordData> _allModifiers = new();
    [SerializeField] private List<ElementWordData> _allElements = new();
    [SerializeField] private List<FormWordData> _allForms = new();

    private Dictionary<ModifierWordType, ModifierWordData> _modifiersByType;
    private Dictionary<ElementWordType, ElementWordData> _elementsByType;
    private Dictionary<FormWordType, FormWordData> _formsByType;

    public IReadOnlyList<ModifierWordData> AllModifiers => _allModifiers;
    public IReadOnlyList<ElementWordData> AllElements => _allElements;
    public IReadOnlyList<FormWordData> AllForms => _allForms;

    public ModifierWordData GetModifier(ModifierWordType word)
    {
        EnsureLookups();
        return _modifiersByType.TryGetValue(word, out var data) ? data : null;
    }

    public ElementWordData GetElement(ElementWordType word)
    {
        EnsureLookups();
        return _elementsByType.TryGetValue(word, out var data) ? data : null;
    }

    public FormWordData GetForm(FormWordType word)
    {
        EnsureLookups();
        return _formsByType.TryGetValue(word, out var data) ? data : null;
    }

    public void GetAvailableWords(SpellWordInventory inventory, List<WordData> resultBuffer)
    {
        resultBuffer.Clear();

        AddAvailableWords(_allModifiers, inventory, resultBuffer);
        AddAvailableWords(_allElements, inventory, resultBuffer);
        AddAvailableWords(_allForms, inventory, resultBuffer);
    }

    private void OnValidate()
    {
        BuildWordLookups();
    }

    private void OnEnable()
    {
        BuildWordLookups();
    }

    private void BuildWordLookups()
    {
        _modifiersByType = BuildLookup(_allModifiers, data => data.Modifier);
        _elementsByType = BuildLookup(_allElements, data => data.Element);
        _formsByType = BuildLookup(_allForms, data => data.Form);
    }

    private void EnsureLookups()
    {
        if (_modifiersByType == null || _elementsByType == null || _formsByType == null)
            BuildWordLookups();
    }

    private static void AddAvailableWords<TData>(IReadOnlyList<TData> words, SpellWordInventory inventory, List<WordData> resultBuffer)
        where TData : WordData
    {
        if (words == null)
            return;

        for (var i = 0; i < words.Count; i++)
        {
            var word = words[i];
            if (word == null || !word.IsValid || inventory == null || inventory.IsUnlocked(word))
                continue;

            resultBuffer.Add(word);
        }
    }

    private static Dictionary<TWord, TData> BuildLookup<TData, TWord>(IReadOnlyList<TData> source, Func<TData, TWord> keySelector)
    {
        var lookup = new Dictionary<TWord, TData>();
        for (var i = 0; i < source.Count; i++)
        {
            if (source[i] == null)
                continue;

            lookup[keySelector(source[i])] = source[i];
        }

        return lookup;
    }
}
