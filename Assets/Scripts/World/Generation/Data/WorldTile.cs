public struct WorldTile
{
    public BiomeType Biome;
    public TileType TileType;

    public WorldTile(BiomeType biome, TileType tileType)
    {
        Biome = biome;
        TileType = tileType;
    }
}
