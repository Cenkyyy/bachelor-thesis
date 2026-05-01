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

    [field: Header("Minimap")]
    [field: SerializeField] public Color32 MinimapInnerColor { get; private set; } = new Color32(35, 35, 35, 255);
    [field: SerializeField] public Color32 MinimapBorderColor { get; private set; } = new Color32(120, 120, 120, 255);

    [Header("Wall Ore Pool")]
    [SerializeField] private List<DecorationEntryData> _oreDecorations = new();
    public IReadOnlyList<DecorationEntryData> OreDecorations => _oreDecorations;
}
