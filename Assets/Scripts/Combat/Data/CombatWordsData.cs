using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Registry asset containing every spell word available in the game.
/// </summary>
[CreateAssetMenu(menuName = "Combat/Word Data", fileName = "CombatWordsData")]
public sealed class CombatWordsData : ScriptableObject
{
    [Header("Words")]
    [SerializeField] private List<ModifierWordData> _allModifiers = new();
    [SerializeField] private List<ElementWordData> _allElements = new();
    [SerializeField] private List<FormWordData> _allForms = new();

    public IReadOnlyList<ModifierWordData> AllModifiers => _allModifiers;
    public IReadOnlyList<ElementWordData> AllElements => _allElements;
    public IReadOnlyList<FormWordData> AllForms => _allForms;
}
