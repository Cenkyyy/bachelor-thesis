using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;
using System;

public class WorldGeneratorBehaviour : MonoBehaviour
{
    [Header("Tilemaps")]
    [SerializeField] private Tilemap groundTilemap;

    [Header("Tiles")]
    [SerializeField] private TileBase voidTile;
    [SerializeField] private TileBase[] winterTiles;
    [SerializeField] private TileBase[] grassTiles;

    [Header("World Settings")]
    [SerializeField] private int worldRadius = 128;
    [SerializeField] private int worldPadding = 4;
    [SerializeField] private int seed = 12345;

    [Header("Player")]
    [SerializeField] private Transform playerTransform;

    private void Start()
    {
        GenerateAndRender();
    }

    public void GenerateAndRender()
    {
        var diameter = worldRadius * 2 + worldPadding * 2;

        var settings = new WorldGenerator.Settings
        {
            Width = diameter,
            Height = diameter,
            Radius = worldRadius,
            Seed = seed,
            BiomeCenters = CreateBiomeCenters(diameter)
        };

        var generator = new WorldGenerator(settings);
        var data = generator.Generate();

        RenderWorld(data);
        PositionPlayer(data);
    }

    private List<WorldGenerator.BiomeCenter> CreateBiomeCenters(int diameter)
    {
        var centers = new List<WorldGenerator.BiomeCenter>();

        Vector2 worldCenter = new Vector2(diameter * 0.5f, diameter * 0.5f);

        // Grassland in the middle
        centers.Add(new WorldGenerator.BiomeCenter(worldCenter, BiomeType.Grassland));

        // A few Winter centers around, so outer ring becomes wintery
        float ringRadius = worldRadius * 0.7f;

        centers.Add(new WorldGenerator.BiomeCenter(worldCenter + new Vector2(ringRadius, 0), BiomeType.Winter));
        centers.Add(new WorldGenerator.BiomeCenter(worldCenter + new Vector2(-ringRadius, 0), BiomeType.Winter));
        centers.Add(new WorldGenerator.BiomeCenter(worldCenter + new Vector2(0, ringRadius), BiomeType.Winter));
        centers.Add(new WorldGenerator.BiomeCenter(worldCenter + new Vector2(0, -ringRadius), BiomeType.Winter));

        return centers;
    }

    private void RenderWorld(WorldData data)
    {
        groundTilemap.ClearAllTiles();

        var offsetX = -data.Width / 2;
        var offsetY = -data.Height / 2;

        for (int y = 0; y < data.Height; y++)
        {
            for (int x = 0; x < data.Width; x++)
            {
                var tile = data.Tiles[x, y];

                var tileAsset = GetTileAsset(tile.TileType);
                if (tileAsset == null)
                    continue;

                var tilePos = new Vector3Int(x + offsetX, y + offsetY, 0);
                groundTilemap.SetTile(tilePos, tileAsset);
            }
        }
    }

    private TileBase GetTileAsset(TileType tileType)
    {
        switch (tileType)
        {
            case TileType.Void:
                return voidTile;

            case TileType.Snow:
                return GetRandomTileAsset(winterTiles);

            case TileType.Grass:
                return GetRandomTileAsset(grassTiles);

            default:
                return null;
        }
    }

    private TileBase GetRandomTileAsset(TileBase[] array)
    {
        if (array == null || array.Length == 0)
            return null;

        // Uniform distribution
        return array[UnityEngine.Random.Range(0, array.Length)];
    }

    private void PositionPlayer(WorldData data)
    {
        if (playerTransform == null || groundTilemap == null)
            return;

        var spawnTile = data.SpawnTile;

        var offsetX = -data.Width / 2;
        var offsetY = -data.Height / 2;

        var cellPos = new Vector3Int(spawnTile.x + offsetX, spawnTile.y + offsetY, 0);

        var cellWorldPos = groundTilemap.CellToWorld(cellPos);
        var worldPos = cellWorldPos + new Vector3(0.5f, 0.5f, 0f);

        playerTransform.position = worldPos;
    }
}
