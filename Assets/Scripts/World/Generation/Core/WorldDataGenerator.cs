using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Generates a world using Voronoi noise based biome regions.
/// </summary>
public class WorldDataGenerator
{
    public struct BiomeCenter
    {
        public Vector2 Position;
        public WorldBiomeType Biome;

        public BiomeCenter(Vector2 position, WorldBiomeType biome)
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

    public WorldDataGenerator(Settings settings)
    {
        _settings = settings;
    }

    public WorldRuntimeData Generate()
    {
        var data = new WorldRuntimeData(_settings.Width, _settings.Height);
        GenerateInto(data, 0, 0, _settings.Width, _settings.Height);
        return data;
    }

    public void GenerateInto(WorldRuntimeData data, int startX, int startY, int width, int height)
    {
        if (data == null)
            return;

        int minX = Mathf.Max(0, startX);
        int minY = Mathf.Max(0, startY);
        int maxX = Mathf.Min(data.Width, startX + Mathf.Max(0, width));
        int maxY = Mathf.Min(data.Height, startY + Mathf.Max(0, height));

        if (minX >= maxX || minY >= maxY)
            return;

        // Prepare biome centers
        var centers = _settings.BiomeCenters;
        if (centers == null || centers.Count == 0)
        {
            throw new System.Exception("At least one biome center is required to generate the world.");
        }

        // Fill tiles
        for (int y = minY; y < maxY; y++)
        {
            for (int x = minX; x < maxX; x++)
            {
                if (data.IsTileGenerated(x, y))
                    continue;

                // Center of this tile
                var tileCenter = new Vector2(x + 0.5f, y + 0.5f);

                if (!_settings.WorldShape.IsInsideBorder(tileCenter))
                {
                    // Outside border ring: skip
                    data.MarkTileGenerated(x, y);
                    continue;
                }

                if (!_settings.WorldShape.IsInsidePlayable(tileCenter))
                {
                    // Border ring: void tiles
                    data.SetTile(x, y, new WorldTile(WorldBiomeType.None, WorldTileType.BorderBase));
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

    private WorldTileType ChooseVisualTileForBiomeTransition(WorldBiomeType biome, WorldBiomeType neighbouringBiome, float nearestDistance, float neighbouringDistance, int x, int y)
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
        float displacement = WorldSeedUtility.SampleSignedPerlinNoise(x, y, noiseScale, _settings.Seed) * displacementAmplitude;

        // Neutral push-pull: border is warped both directions by signed displacement.
        float warpedSignedDistance = borderSignedDistance - displacement;

        if (Mathf.Abs(warpedSignedDistance) > bandWidth)
            return ChooseTileForBiome(biome);

        // If the warped border passes over this tile, borrow neighbouring biome visual tile.
        if (warpedSignedDistance <= 0f)
            return ChooseTileForBiome(neighbouringBiome);

        return ChooseTileForBiome(biome);
    }

    private WorldTileType ChooseTileForBiome(WorldBiomeType biome)
    {
        switch (biome)
        {
            case WorldBiomeType.Grassland:
                return WorldTileType.GrasslandBase;
            case WorldBiomeType.IceTundra:
                return WorldTileType.IceTundraBase;
            case WorldBiomeType.Desert:
                return WorldTileType.DesertBase;
            case WorldBiomeType.AmethystRift:
                return WorldTileType.AmethystRiftBase;
            default:
                return WorldTileType.BorderBase;
        }
    }
}
