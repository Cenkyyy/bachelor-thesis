using System;

public static class WorldSeedUtils
{
    public const uint PRIME_FNV1_32 = 16777619u;

    public static int CreateRandomSeed()
    {
        var bytes = Guid.NewGuid().ToByteArray();
        return BitConverter.ToInt32(bytes, 0);
    }
}
