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

        public IWorldShape WorldShape;
        public int Seed;

        public float TransitionBandWidthTiles;
        public float TransitionNoiseScale;
        public float TransitionDisplacementTiles;

        public List<BiomeCenter> BiomeCenters;
    }

    private readonly Settings _settings;

    public WorldGenerator(Settings settings)
    {
        _settings = settings;
    }

    public WorldRuntimeData Generate()
    {
        var data = new WorldRuntimeData(_settings.Width, _settings.Height);

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

                if (!_settings.WorldShape.IsInsideBorder(tileCenter))
                {
                    // Outside border ring: skip
                    continue;
                }

                if (!_settings.WorldShape.IsInsidePlayable(tileCenter))
                {
                    // Border ring: void tiles
                    data.SetTile(x, y, new WorldTile(BiomeType.None, TileType.Void));
                    continue;
                }

                // Inside circle: choose biome via Voronoi and in case of tile transitions happening, get the nearest different biome for transition blending effects
                FindNearestCenters(tileCenter, centers, out var nearest, out var nearestDifferentBiome, out var nearestDistance, out var nearestDifferentBiomeDistance);
                var biome = nearest.Biome;

                // Inside that biome, decide concrete tile type
                var tileType = ChooseVisualTileForBiomeTransition(biome, nearestDifferentBiome.Biome, nearestDistance, nearestDifferentBiomeDistance, x, y);

                data.SetTile(x, y, new WorldTile(biome, tileType));
            }
        }

        return data;
    }

    private void FindNearestCenters(Vector2 position, List<BiomeCenter> centers, out BiomeCenter nearest, out BiomeCenter nearestDifferentBiome, out float nearestDistance, out float nearestDifferentBiomeDistance)
    {
        nearest = centers[0];
        float bestDistSq = (position - nearest.Position).sqrMagnitude;

        for (int i = 1; i < centers.Count; i++)
        {
            var center = centers[i];
            var distSq = (position - center.Position).sqrMagnitude;
            if (distSq < bestDistSq)
            {
                bestDistSq = distSq;
                nearest = center;
            }
        }

        nearestDifferentBiome = nearest;
        float bestDifferentBiomeDistSq = float.MaxValue;
        for (int i = 0; i < centers.Count; i++)
        {
            var center = centers[i];
            if (center.Biome == nearest.Biome)
                continue;

            var distSq = (position - center.Position).sqrMagnitude;
            if (distSq < bestDifferentBiomeDistSq)
            {
                bestDifferentBiomeDistSq = distSq;
                nearestDifferentBiome = center;
            }
        }

        nearestDistance = Mathf.Sqrt(bestDistSq);
        nearestDifferentBiomeDistance = bestDifferentBiomeDistSq < float.MaxValue ? Mathf.Sqrt(bestDifferentBiomeDistSq) : float.MaxValue;
    }

    private TileType ChooseVisualTileForBiomeTransition(BiomeType biome, BiomeType neighbouringBiome, float nearestDistance, float neighbouringDistance, int x, int y)
    {
        if (neighbouringBiome == biome)
            return ChooseTileForBiome(biome);

        float bandWidth = Mathf.Max(0f, _settings.TransitionBandWidthTiles);
        if (bandWidth <= Mathf.Epsilon || neighbouringDistance == float.MaxValue)
            return ChooseTileForBiome(biome);

        // Signed distance from current tile to the Voronoi border line between two competing biome centers.
        // Positive means the tile is on the current biome side, negative means neighbour side.
        float borderSignedDistance = (neighbouringDistance - nearestDistance) * 0.5f;
        if (Mathf.Abs(borderSignedDistance) > bandWidth)
            return ChooseTileForBiome(biome);

        float noiseScale = Mathf.Max(0.001f, _settings.TransitionNoiseScale);
        float displacementAmplitude = Mathf.Max(0f, _settings.TransitionDisplacementTiles);
        float displacement = WorldSeedUtils.SampleSignedPerlinNoise(x, y, noiseScale, _settings.Seed) * displacementAmplitude;

        // Neutral push-pull: border is warped both directions by signed displacement.
        float warpedSignedDistance = borderSignedDistance - displacement;

        if (Mathf.Abs(warpedSignedDistance) > bandWidth)
            return ChooseTileForBiome(biome);

        // If the warped border passes over this tile, borrow neighbouring biome visual tile.
        if (warpedSignedDistance <= 0f)
            return ChooseTileForBiome(neighbouringBiome);

        return ChooseTileForBiome(biome);
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
}
