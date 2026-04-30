/// <summary>
/// Defines a contract for retrieving spell word effectiveness data, including biome affinity and enemy role tags, for a target.
/// </summary>
public interface ISpellWordEffectivenessTarget
{
    bool TryGetSpellWordEffectivenessData(out ItemBiomeAffinity biome, out EnemyRoleTag roleTags);
}
