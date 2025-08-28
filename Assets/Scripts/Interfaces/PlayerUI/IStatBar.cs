/// <summary>
/// Interface for UI elements that display player stats, such as health, mana, or xp bars.
/// </summary>
public interface IStatBar
{
    /// <summary>
    /// Initializes the stat bar with the given player stats.
    /// </summary>
    /// <param name="data">The PlayerStats ScriptableObject to link to the bar.</param>
    void Initialize(PlayerData data);
}
