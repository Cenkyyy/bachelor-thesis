using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Combat/Word Data", fileName = "CombatWordsData")]
public sealed class CombatWordsData : ScriptableObject
{
    [SerializeField] private List<ModifierWordData> _allModifiers = new();
    [SerializeField] private List<ElementWordData> _allElements = new();
    [SerializeField] private List<FormWordData> _allForms = new();

    private Dictionary<ModifierWord, ModifierWordData> _modifiersByType;
    private Dictionary<ElementWord, ElementWordData> _elementsByType;
    private Dictionary<FormWord, FormWordData> _formsByType;

    public IReadOnlyList<ModifierWordData> AllModifiers => _allModifiers;
    public IReadOnlyList<ElementWordData> AllElements => _allElements;
    public IReadOnlyList<FormWordData> AllForms => _allForms;

    public ModifierWordData GetModifier(ModifierWord word) => _modifiersByType.TryGetValue(word, out var data) ? data : null;
    public ElementWordData GetElement(ElementWord word) => _elementsByType.TryGetValue(word, out var data) ? data : null;
    public FormWordData GetForm(FormWord word) => _formsByType.TryGetValue(word, out var data) ? data : null;

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
