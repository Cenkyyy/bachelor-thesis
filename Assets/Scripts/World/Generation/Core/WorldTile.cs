public struct WorldTile
{
    public WorldBiomeType Biome;
    public WorldTileType TileType;

    public WorldTile(WorldBiomeType biome, WorldTileType tileType)
    {
        Biome = biome;
        TileType = tileType;
    }
}
