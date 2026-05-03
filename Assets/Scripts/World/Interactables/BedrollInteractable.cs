using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(BoxCollider2D), typeof(Rigidbody2D), typeof(PrefabMineableRuntimeData))]
public sealed class BedrollInteractable : InteractableBase
{
    [Header("Spawn Point")]
    [SerializeField] private LayerMask _spawnObstacleMask = ~0;
    [SerializeField, Min(0.01f)] private float _spawnProbeRadius = 0.25f;
    [SerializeField, Min(1)] private int _spawnSearchRadius = 3;

    [Header("Rules")]
    [SerializeField] private bool _requireNight = true;
    [SerializeField] private bool _allowDaytimeTesting = false;
    [SerializeField] private LayerMask _enemyLayerMask;
    [SerializeField] private float _enemyCheckRadius = 5f;

    [Header("Feedback")]
    [SerializeField] private WorldTextPopupEmitter _feedbackPopup;
    [SerializeField] private string _spawnPointSetMessage = "New spawn point has been set";
    [SerializeField] private string _cannotSleepMessage = "Cannot sleep, enemies nearby";

    private Player _playerInRange;

    private void Awake()
    {
        if (_feedbackPopup == null)
            _feedbackPopup = GetComponent<WorldTextPopupEmitter>();

        if (_feedbackPopup == null)
            _feedbackPopup = gameObject.AddComponent<WorldTextPopupEmitter>();
    }

    public override bool CanInteract()
    {
        if (!IsPlayerInside || GameStateManager.IsGamePaused)
            return false;
        if (Event.current != null && EventSystem.current.IsPointerOverGameObject())
            return false;

        return true;
    }

    public override void Interact()
    {
        if (!CanInteract())
            return;

        var hasEnemiesNearby = HasEnemiesNearby();
        UpdatePlayerSpawnPoint(hasEnemiesNearby);

        if (!CanOpenWordShop(hasEnemiesNearby))
        {
            if (hasEnemiesNearby)
                _feedbackPopup.ShowMessage(_cannotSleepMessage);

            return;
        }

        if (PanelManager.Instance != null)
            PanelManager.Instance.OpenMajorPanel(PanelId.WordShop);
    }

    protected override void OnPlayerEnterTrigger(Collider2D playerCollider)
    {
        _playerInRange = playerCollider.GetComponentInParent<Player>();
    }

    protected override void OnPlayerExitTrigger(Collider2D playerCollider)
    {
        if (_playerInRange != null && playerCollider.transform.IsChildOf(_playerInRange.transform))
            _playerInRange = null;
    }

    private void UpdatePlayerSpawnPoint(bool hasEnemiesNearby)
    {
        if (_playerInRange == null || hasEnemiesNearby)
            return;

        var spawnPosition = SpawnPointPlacementUtility.ResolveNearestFreePosition(transform.position, _spawnObstacleMask, _spawnProbeRadius, _spawnSearchRadius);
        _playerInRange.Data.SetSpawnPoint(spawnPosition);
        _feedbackPopup.ShowMessage(_spawnPointSetMessage);
    }

    private bool CanOpenWordShop(bool hasEnemiesNearby)
    {
        if (_requireNight && !_allowDaytimeTesting && (DayNightSystem.Instance == null || !DayNightSystem.Instance.IsNight))
            return false;
        if (hasEnemiesNearby)
            return false;

        return true;
    }

    private bool HasEnemiesNearby()
    {
        if (_enemyCheckRadius <= 0f)
            return false;

        var hit = Physics2D.OverlapCircle(transform.position, _enemyCheckRadius, _enemyLayerMask);
        return hit != null && hit.transform != transform;
    }
}
