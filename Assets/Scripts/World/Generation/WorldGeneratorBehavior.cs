using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;
using System;

public class WorldGeneratorBehaviour : MonoBehaviour
{
    [Header("Tilemaps")]
    [SerializeField] private Tilemap _groundTilemap;

    [Header("Tiles")]
    [SerializeField] private TileBase _voidTile;
    [SerializeField] private TileBase[] _winterTiles;
    [SerializeField] private TileBase[] _grassTiles;

    [Header("World Settings")]
    [SerializeField] private int _worldRadius = 128;
    [SerializeField] private int _worldPadding = 4;
    [SerializeField] private int _defaultSeed = 12345;
    [SerializeField] private bool _randomizeSeedOnPlay = false; // should be true when creating new game

    [Header("Player")]
    [SerializeField] private Transform _playerTransform;

    [Header("Minimap")]
    [SerializeField] private MinimapController _minimap;

    public int CurrentSeed { get; private set; }

    private void Start()
    {
        CurrentSeed = ResolveSeed();
        GenerateAndRender(CurrentSeed);
    }

    public void GenerateAndRender(int seedUsed)
    {
        var diameter = _worldRadius * 2 + _worldPadding * 2;

        var settings = new WorldGenerator.Settings
        {
            Width = diameter,
            Height = diameter,
            Radius = _worldRadius,
            Seed = seedUsed,
            BiomeCenters = CreateBiomeCenters(diameter)
        };

        var generator = new WorldGenerator(settings);
        var data = generator.Generate();

        RenderWorld(data, seedUsed);
        PositionPlayer(data);
        _minimap.Initialize(data);
    }
    private int ResolveSeed()
    {
        if (_randomizeSeedOnPlay)
        {
            return WorldSeedUtils.CreateRandomSeed();
        }

        return _defaultSeed;
    }

    private List<WorldGenerator.BiomeCenter> CreateBiomeCenters(int diameter)
    {
        var centers = new List<WorldGenerator.BiomeCenter>();

        Vector2 worldCenter = new Vector2(diameter * 0.5f, diameter * 0.5f);

        // Grassland in the middle
        centers.Add(new WorldGenerator.BiomeCenter(worldCenter, BiomeType.Grassland));

        // A few Winter centers around, so outer ring becomes wintery
        float ringRadius = _worldRadius * 0.7f;

        centers.Add(new WorldGenerator.BiomeCenter(worldCenter + new Vector2(ringRadius, 0), BiomeType.Winter));
        centers.Add(new WorldGenerator.BiomeCenter(worldCenter + new Vector2(-ringRadius, 0), BiomeType.Winter));
        centers.Add(new WorldGenerator.BiomeCenter(worldCenter + new Vector2(0, ringRadius), BiomeType.Winter));
        centers.Add(new WorldGenerator.BiomeCenter(worldCenter + new Vector2(0, -ringRadius), BiomeType.Winter));

        return centers;
    }

    private void RenderWorld(WorldData data, int seedUsed)
    {
        _groundTilemap.ClearAllTiles();

        var offsetX = -data.Width / 2;
        var offsetY = -data.Height / 2;

        for (int y = 0; y < data.Height; y++)
        {
            for (int x = 0; x < data.Width; x++)
            {
                var tile = data.Tiles[x, y];

                var tileAsset = GetTileAsset(tile.TileType, x, y, seedUsed);
                if (tileAsset == null)
                    continue;

                var tilePos = data.DataToCell(x, y);
                _groundTilemap.SetTile(tilePos, tileAsset);
            }
        }
    }

    private TileBase GetTileAsset(TileType tileType, int x, int y, int seedUsed)
    {
        switch (tileType)
        {
            case TileType.Void:
                return _voidTile;

            case TileType.Snow:
                return GetDeterministicTileAsset(_winterTiles, seedUsed, x, y, tileType: (int)tileType);

            case TileType.Grass:
                return GetDeterministicTileAsset(_grassTiles, seedUsed, x, y, tileType: (int)tileType);

            default:
                return null;
        }
    }

    private TileBase GetDeterministicTileAsset(TileBase[] array, int seedUsed, int x, int y, int tileType)
    {
        if (array == null || array.Length == 0)
            return null;

        int idx = DeterministicIndex(seedUsed, x, y, tileType, array.Length);
        return array[idx];
    }

    private int DeterministicIndex(int seedUsed, int x, int y, int tileType, int length)
    {
        unchecked
        {
            uint hash = (uint)seedUsed;
            hash = hash * WorldSeedUtils.PRIME_FNV1_32 ^ (uint)x;
            hash = hash * WorldSeedUtils.PRIME_FNV1_32 ^ (uint)y;
            hash = hash * WorldSeedUtils.PRIME_FNV1_32 ^ (uint)tileType;

            return (int)(hash % length);
        }
    }

    private void PositionPlayer(WorldData data)
    {
        if (_playerTransform == null || _groundTilemap == null)
            return;

        var cellPos = data.DataToCell(data.SpawnTile.x, data.SpawnTile.y);

        var cellWorldPos = _groundTilemap.CellToWorld(cellPos);
        var worldPos = cellWorldPos + new Vector3(0.5f, 0.5f, 0f);

        _playerTransform.position = worldPos;
    }
}
