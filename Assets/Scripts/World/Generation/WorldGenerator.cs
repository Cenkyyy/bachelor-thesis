using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Generates a world using Voronoi noise based biome regions.
/// </summary>
public class WorldGenerator
{
    public struct BiomeCenter
    {
        public Vector2 Position;
        public BiomeType Biome;

        public BiomeCenter(Vector2 position, BiomeType biome)
        {
            Position = position;
            Biome = biome;
        }
    }

    public struct Settings
    {
        public int Width;
        public int Height;

        public float PlayableRadius;
        public float BorderThickness;
        public int Seed;
        public List<BiomeCenter> BiomeCenters;
    }

    private readonly Settings _settings;

    public WorldGenerator(Settings settings)
    {
        _settings = settings;
    }

    public WorldData Generate()
    {
        var data = new WorldData(_settings.Width, _settings.Height);

        var worldCenter = new Vector2(_settings.Width * 0.5f, _settings.Height * 0.5f);
        var playableRadiusSq = _settings.PlayableRadius * _settings.PlayableRadius;
        var borderOuterRadius = _settings.PlayableRadius + Mathf.Max(0f, _settings.BorderThickness);
        var borderOuterRadiusSq = borderOuterRadius * borderOuterRadius;

        // Prepare biome centers
        var centers = _settings.BiomeCenters;
        if (centers == null || centers.Count == 0)
        {
            throw new System.Exception("At least one biome center is required to generate the world.");
        }

        // Fill tiles
        for (int y = 0; y < _settings.Height; y++)
        {
            for (int x = 0; x < _settings.Width; x++)
            {
                // Center of this tile
                var tileCenter = new Vector2(x + 0.5f, y + 0.5f);

                // Check if inside circular world
                var offset = tileCenter - worldCenter;
                var distSq = offset.sqrMagnitude;

                if (distSq > borderOuterRadiusSq)
                {
                    // Outside border ring: empty tiles
                    data.Tiles[x, y] = new WorldTile(BiomeType.None, TileType.None);
                    continue;
                }

                if (distSq > playableRadiusSq)
                {
                    // Border ring: void tiles
                    data.Tiles[x, y] = new WorldTile(BiomeType.None, TileType.Void);
                    continue;
                }

                // Inside circle: choose biome via Voronoi
                var nearest = FindNearestCenter(tileCenter, centers);
                var biome = nearest.Biome;

                // Inside that biome, decide concrete tile type
                var tileType = ChooseTileForBiome(biome);

                data.Tiles[x, y] = new WorldTile(biome, tileType);
            }
        }

        // Compute spawn tile near world center
        data.SpawnTile = FindSpawnTile(data, worldCenter);

        return data;
    }

    private BiomeCenter FindNearestCenter(Vector2 position, List<BiomeCenter> centers)
    {
        var best = centers[0];
        var bestDistSq = (position - best.Position).sqrMagnitude;

        for (int i = 1; i < centers.Count; i++)
        {
            var distSq = (position - centers[i].Position).sqrMagnitude;
            if (distSq < bestDistSq)
            {
                bestDistSq = distSq;
                best = centers[i];
            }
        }

        return best;
    }

    private TileType ChooseTileForBiome(BiomeType biome)
    {
        switch (biome)
        {
            case BiomeType.Grassland:
                return TileType.GrasslandBase;
            case BiomeType.IceTundra:
                return TileType.IceTundraBase;
            case BiomeType.Desert:
                return TileType.DesertBase;
            case BiomeType.AmethystRift:
                return TileType.AmethystRift;
            default:
                return TileType.Void;
        }
    }

    private Vector2Int FindSpawnTile(WorldData data, Vector2 desiredPosition)
    {
        int bestX = Mathf.Clamp(Mathf.RoundToInt(desiredPosition.x), 0, data.Width - 1);
        int bestY = Mathf.Clamp(Mathf.RoundToInt(desiredPosition.y), 0, data.Height - 1);

        if (IsTileWalkable(data, bestX, bestY))
        {
            return new Vector2Int(bestX, bestY);
        }

        int maxRadius = Mathf.Max(data.Width, data.Height);

        // Spiral outwards to find the nearest walkable tile
        for (int r = 1; r < maxRadius; r++)
        {
            for (int dy = -r; dy <= r; dy++)
            {
                for (int dx = -r; dx <= r; dx++)
                {
                    int x = bestX + dx;
                    int y = bestY + dy;

                    if (x < 0 || x >= data.Width || y < 0 || y >= data.Height)
                        continue;

                    if (IsTileWalkable(data, x, y))
                    {
                        return new Vector2Int(x, y);
                    }
                }
            }
        }

        return new Vector2Int(bestX, bestY);
    }

    private bool IsTileWalkable(WorldData data, int x, int y) => data.Tiles[x, y].TileType != TileType.Void;
}
