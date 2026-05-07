using UnityEngine;

[DisallowMultipleComponent]
public sealed class TemporaryDeathChestController : MonoBehaviour
{
    private string _deathChestId;
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
            PanelManager.Instance.OnDeathChestClosed -= HandleDeathChestClosed;

        _isSubscribed = false;
    }

    public void Initialize(string deathChestId, IInventory inventory, DeathMarkerController markerService, DeathChestRegistry registry)
    {
        _deathChestId = deathChestId;
        _inventory = inventory;
        _markerService = markerService;
        _registry = registry;
    }

    public void ForceDespawn()
    {
        _markerService?.RemoveDeathMarker(_deathChestId);
        _registry?.Unregister(_deathChestId);
        Destroy(gameObject);
    }

    private void HandleDeathChestClosed(IInventory inventory)
    {
        if (!ReferenceEquals(inventory, _inventory))
            return;

        ForceDespawn();
    }

    private void TrySubscribeToPanelManager()
    {
        if (_isSubscribed || PanelManager.Instance == null)
            return;

        PanelManager.Instance.OnDeathChestClosed += HandleDeathChestClosed;
        _isSubscribed = true;
    }
}
