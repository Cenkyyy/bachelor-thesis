/// <summary>
/// Basic contract for UI panels that can be opened and closed by panel systems.
/// </summary>
public interface IPanel
{
    bool IsOpen { get; }
    void Open();
    void Close();
}
