using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "StarterWordSelectionData", menuName = "Game/Combat/Starter Word Selection Data")]
public sealed class StarterWordSelectionData : ScriptableObject
{
    [Serializable]
    public sealed class ModifierSelectionRule : SelectionRuleBase<ModifierWordType> { }

    [Serializable]
    public sealed class ElementSelectionRule : SelectionRuleBase<ElementWordType> { }

    [Serializable]
    public sealed class FormSelectionRule : SelectionRuleBase<FormWordType> { }

    [Serializable]
    public abstract class SelectionRuleBase<TWord> where TWord : struct, Enum
    {
        [field: SerializeField, Min(0)] public int RequiredCount { get; private set; } = 1;
        
        [SerializeField] private List<TWord> _fixedWords = new();
        [SerializeField] private List<TWord> _choiceWords = new();

        public IReadOnlyList<TWord> FixedWords => _fixedWords;
        public IReadOnlyList<TWord> ChoiceWords => _choiceWords;
    }

    [field: Header("Selection Rules")]
    [field: SerializeField] public ModifierSelectionRule ModifierRule { get; private set; } = new();
    [field: SerializeField] public ElementSelectionRule ElementRule { get; private set; } = new();
    [field: SerializeField] public FormSelectionRule FormRule { get; private set; } = new();
}
