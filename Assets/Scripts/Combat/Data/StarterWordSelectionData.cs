using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Defines the fixed and selectable spell words granted during starter word selection.
/// </summary>
[CreateAssetMenu(fileName = "StarterWordSelectionData", menuName = "Game/Combat/Starter Word Selection Data")]
public sealed class StarterWordSelectionData : ScriptableObject
{
    [field: Header("Selection Rules")]
    [field: SerializeField] public ModifierSelectionRule ModifierRule { get; private set; } = new();
    [field: SerializeField] public ElementSelectionRule ElementRule { get; private set; } = new();
    [field: SerializeField] public FormSelectionRule FormRule { get; private set; } = new();

    /// <summary>
    /// Starter selection rule for modifier words.
    /// </summary>
    [Serializable]
    public sealed class ModifierSelectionRule : SelectionRuleBase<ModifierWordData>
    {
    }

    /// <summary>
    /// Starter selection rule for element words.
    /// </summary>
    [Serializable]
    public sealed class ElementSelectionRule : SelectionRuleBase<ElementWordData>
    {
    }

    /// <summary>
    /// Starter selection rule for form words.
    /// </summary>
    [Serializable]
    public sealed class FormSelectionRule : SelectionRuleBase<FormWordData>
    {
    }

    /// <summary>
    /// Defines fixed words, selectable choices, and the required selection count for one starter word category.
    /// </summary>
    [Serializable]
    public abstract class SelectionRuleBase<TWordData> where TWordData : WordData
    {
        [field: Header("Requirement")]
        [field: SerializeField, Min(0)] public int RequiredCount { get; private set; } = 1;

        [Header("Words")]
        [SerializeField] private List<TWordData> _fixedWords = new();
        [SerializeField] private List<TWordData> _choiceWords = new();

        public IReadOnlyList<TWordData> FixedWords => _fixedWords;
        public IReadOnlyList<TWordData> ChoiceWords => _choiceWords;
    }
}
