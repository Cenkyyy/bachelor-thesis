public interface IEntitySelectionStrategy<TData> where TData : EntityData
{
    bool TrySelect(ItemBiomeAffinity biome, out TData data);
}
