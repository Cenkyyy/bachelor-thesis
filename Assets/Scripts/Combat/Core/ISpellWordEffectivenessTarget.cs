/// <summary>
/// Contract for targets that expose biome affinity and role tags used by spell word effectiveness rules.
/// </summary>
public interface ISpellWordEffectivenessTarget
{
    bool TryGetSpellWordEffectivenessData(out ItemBiomeAffinity biome, out EnemyRoleTag roleTags);
}
