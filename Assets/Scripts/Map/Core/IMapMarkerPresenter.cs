public interface IMapMarkerPresenter
{
    void AddMarker(MapMarkerRuntimeData marker);
    void UpdateMarker(MapMarkerRuntimeData marker);
    void RemoveMarker(string markerId);
}
