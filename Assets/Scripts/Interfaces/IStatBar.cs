/// <summary>
/// Interface for UI elements that display player stats, such as health, mana, or stamina bars.
/// </summary>
public interface IStatBar
{
    /// <summary>
    /// Initializes the stat bar with the given player stats.
    /// </summary>
    /// <param name="stats">The PlayerStats ScriptableObject to link to the bar.</param>
    void Initialize(PlayerStatsSO stats);

    /// <summary>
    /// Updates the stat bar to reflect the current value of the associated stat.
    /// </summary>
    void UpdateBar();
}
