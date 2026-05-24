public interface IMapMarkerView
{
    void AddMarker(MapMarkerRuntimeState marker);
    void UpdateMarker(MapMarkerRuntimeState marker);
    void RemoveMarker(string markerId);
}
