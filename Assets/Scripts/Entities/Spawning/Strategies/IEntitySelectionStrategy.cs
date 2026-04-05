public interface IEntitySelectionStrategy<TData> where TData : EntityData
{
    bool TrySelect(BiomeAffinity biome, out TData data);
}
