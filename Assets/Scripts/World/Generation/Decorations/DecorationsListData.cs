using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "World/Decorations/Decorations List Data")]
public sealed class DecorationsListData : ScriptableObject
{
    [SerializeField] private List<DecorationEntryData> _entries = new();

    public IReadOnlyList<DecorationEntryData> Entries => _entries;
}
