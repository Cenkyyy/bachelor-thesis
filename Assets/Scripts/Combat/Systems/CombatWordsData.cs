using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Combat/Word Data", fileName = "CombatWordsData")]
public sealed class CombatWordsData : ScriptableObject
{
    [SerializeField] private List<ModifierWord> _allModifiers = new();
    [SerializeField] private List<ElementWord> _allElements = new();
    [SerializeField] private List<FormWord> _allForms = new();

    public IReadOnlyList<ModifierWord> AllModifiers => _allModifiers;
    public IReadOnlyList<ElementWord> AllElements => _allElements;
    public IReadOnlyList<FormWord> AllForms => _allForms;
}
