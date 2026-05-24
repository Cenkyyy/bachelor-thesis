/// <summary>
/// Defines a map UI surface that can display runtime markers.
/// </summary>
public interface IMapMarkerView
{
    /// <summary>
    /// Adds a marker to the view.
    /// </summary>
    void AddMarker(MapMarkerRuntimeState marker);

    /// <summary>
    /// Updates an existing marker in the view.
    /// </summary>
    void UpdateMarker(MapMarkerRuntimeState marker);

    /// <summary>
    /// Removes a marker from the view.
    /// </summary>
    void RemoveMarker(string markerId);
}
