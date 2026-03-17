using UnityEngine;

[DisallowMultipleComponent]
public sealed class TemporaryDeathChestController : MonoBehaviour
{
    private string _chestId;
    private IInventory _inventory;
    private DeathMarkerController _markerService;
    private DeathChestRegistry _registry;
    private bool _isSubscribed;

    private void OnEnable()
    {
        TrySubscribeToPanelManager();
    }

    private void Start()
    {
        TrySubscribeToPanelManager();
    }

    private void OnDisable()
    {
        if (_isSubscribed && PanelManager.Instance != null)
            PanelManager.Instance.OnChestClosed -= HandleChestClosed;

        _isSubscribed = false;
    }

    public void Initialize(string chestId, IInventory inventory, DeathMarkerController markerService, DeathChestRegistry registry)
    {
        _chestId = chestId;
        _inventory = inventory;
        _markerService = markerService;
        _registry = registry;
    }

    public void ForceDespawn()
    {
        _markerService?.RemoveDeathMarker(_chestId);
        _registry?.Unregister(_chestId);
        Destroy(gameObject);
    }

    private void HandleChestClosed(IInventory inventory)
    {
        if (!ReferenceEquals(inventory, _inventory))
            return;

        ForceDespawn();
    }

    private void TrySubscribeToPanelManager()
    {
        if (_isSubscribed || PanelManager.Instance == null)
            return;

        PanelManager.Instance.OnChestClosed += HandleChestClosed;
        _isSubscribed = true;
    }
}
