/// <summary>
/// Contract for HUD stat bars that bind to player runtime data.
/// </summary>
public interface IStatBar
{
    void Initialize(PlayerRuntimeData data);
}
