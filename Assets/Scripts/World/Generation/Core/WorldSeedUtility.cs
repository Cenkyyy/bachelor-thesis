using System;
using UnityEngine;

public static class WorldSeedUtility
{
    public const uint PRIME_FNV1_32 = 16777619u;

    // Large offsets to avoid sampling around zero
    private const float PerlinBaseOffsetX = 137.42f;
    private const float PerlinBaseOffsetY = 911.73f;

    // Seed multipliers that deterministically shift the Perlin sampling domain per world seed
    private const float PerlinSeedScaleX = 0.01713f;
    private const float PerlinSeedScaleY = 0.00971f;

    public static string SeedText { get; private set; }
    public static bool HasCustomSeed => !string.IsNullOrEmpty(SeedText);

    public static int CreateRandomSeed()
    {
        var bytes = Guid.NewGuid().ToByteArray();
        return BitConverter.ToInt32(bytes, 0);
    }

    public static void SetSeedText(string seedText)
    {
        SeedText = seedText ?? string.Empty;
    }

    public static bool TryGetCustomSeed(out int seed)
    {
        seed = default;
        if (!HasCustomSeed)
            return false;

        return int.TryParse(SeedText, out seed);
    }

    /// <summary>
    /// Samples deterministic seed-shifted Perlin noise and remaps it from [0,1] to [-1,1] using * 2f - 1f.
    /// Useful for neutral push-pull displacement fields in world generation so both biomes can push-pull tiles.
    /// </summary>
    public static float SampleSignedPerlinNoise(int x, int y, float scale, int seed)
    {
        float safeScale = Mathf.Max(Mathf.Epsilon, scale);
        float sampleX = (x + 0.5f) * safeScale + PerlinBaseOffsetX + seed * PerlinSeedScaleX;
        float sampleY = (y + 0.5f) * safeScale + PerlinBaseOffsetY + seed * PerlinSeedScaleY;

        return Mathf.PerlinNoise(sampleX, sampleY) * 2f - 1f;
    }

    public static int CombineSeed(int seed, int x, int y)
    {
        unchecked
        {
            uint hash = (uint)seed;
            hash = (hash ^ (uint)x) * PRIME_FNV1_32;
            hash = (hash ^ (uint)y) * PRIME_FNV1_32;
            return (int)hash;
        }
    }
}
