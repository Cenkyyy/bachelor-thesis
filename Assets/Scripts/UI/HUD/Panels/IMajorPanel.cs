/// <summary>
/// Contract for primary HUD panels that can control game pause and gameplay input blocking.
/// </summary>
public interface IMajorPanel : IPanel
{
    PanelId Id { get; }
    bool PausesGame { get; }
    bool BlocksGameplayInput { get; }
}
