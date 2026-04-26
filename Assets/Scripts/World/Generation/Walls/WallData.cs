using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(menuName = "World/Walls/Wall Data")]
public sealed class WallData : ScriptableObject
{
    [field: Header("Identity")]
    [field: SerializeField] public string WallId { get; private set; }

    [field: Header("Tile")]
    [field: SerializeField] public TileBase RuleTile { get; private set; }
    [field: SerializeField] public MineableNodeData MineableData { get; private set; }

    [Header("Wall Ore Pool")]
    [SerializeField] private List<DecorationEntryData> _oreDecorations = new();
    public IReadOnlyList<DecorationEntryData> OreDecorations => _oreDecorations;
}
