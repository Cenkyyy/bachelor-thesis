public interface IEnemySelectionStrategy
{
    bool TrySelect(BiomeAffinity biome, out EnemyData enemyData);
}
