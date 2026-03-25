public interface IMajorPanel : IPanel
{
    PanelId Id { get; }
    bool PausesGame { get; }
    bool BlocksGameplayInput { get; }
}
