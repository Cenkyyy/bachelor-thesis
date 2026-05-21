using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Combat/Word Data", fileName = "CombatWordsData")]
public sealed class CombatWordsData : ScriptableObject
{
    [SerializeField] private List<ModifierWordData> _allModifiers = new();
    [SerializeField] private List<ElementWordData> _allElements = new();
    [SerializeField] private List<FormWordData> _allForms = new();

    public IReadOnlyList<ModifierWordData> AllModifiers => _allModifiers;
    public IReadOnlyList<ElementWordData> AllElements => _allElements;
    public IReadOnlyList<FormWordData> AllForms => _allForms;
}
