using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Authored visual data for a generated world tile type.
/// </summary>
[CreateAssetMenu(menuName = "World/Tiles/Tile", fileName = "NewWorldTileData")]
public sealed class WorldTileData : ScriptableObject
{
    [field: Header("Identity")]
    [field: SerializeField] public WorldTileType TileType { get; private set; }

    [field: Header("Tilemap Visual")]
    [field: SerializeField] public TileBase Tile { get; private set; }

    [field: Header("Map Visual")]
    [field: SerializeField] public Color32 MapColor { get; private set; } = new(0, 0, 0, 255);
}
