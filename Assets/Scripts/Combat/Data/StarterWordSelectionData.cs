using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "StarterWordSelectionData", menuName = "Game/Combat/Starter Word Selection Data")]
public sealed class StarterWordSelectionData : ScriptableObject
{
    [Serializable]
    public sealed class ModifierSelectionRule : SelectionRuleBase<ModifierWordData> { }

    [Serializable]
    public sealed class ElementSelectionRule : SelectionRuleBase<ElementWordData> { }

    [Serializable]
    public sealed class FormSelectionRule : SelectionRuleBase<FormWordData> { }

    [Serializable]
    public abstract class SelectionRuleBase<TWordData> where TWordData : WordData
    {
        [field: SerializeField, Min(0)] public int RequiredCount { get; private set; } = 1;

        [SerializeField] private List<TWordData> _fixedWords = new();
        [SerializeField] private List<TWordData> _choiceWords = new();

        public IReadOnlyList<TWordData> FixedWords => _fixedWords;
        public IReadOnlyList<TWordData> ChoiceWords => _choiceWords;
    }

    [field: Header("Selection Rules")]
    [field: SerializeField] public ModifierSelectionRule ModifierRule { get; private set; } = new();
    [field: SerializeField] public ElementSelectionRule ElementRule { get; private set; } = new();
    [field: SerializeField] public FormSelectionRule FormRule { get; private set; } = new();
}
