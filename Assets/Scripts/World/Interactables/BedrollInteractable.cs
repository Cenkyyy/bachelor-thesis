using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(BoxCollider2D), typeof(Rigidbody2D), typeof(MineableNode))]
public sealed class BedrollInteractable : InteractableBase
{
    [Header("Rules")]
    [SerializeField] private bool _requireNight = true;
    [SerializeField] private bool _allowDaytimeTesting = false;
    [SerializeField] private LayerMask _enemyLayerMask;
    [SerializeField] private float _enemyCheckRadius = 5f;

    public override bool CanInteract()
    {
        if (!IsPlayerInside || GameStateManager.IsGamePaused)
            return false;
        if (Event.current != null && EventSystem.current.IsPointerOverGameObject())
            return false;
        if (_requireNight && !_allowDaytimeTesting && !NightTimeFlag.IsNight)
            return false;
        if (HasEnemiesNearby())
            return false;

        return true;
    }

    public override void Interact()
    {
        if (!CanInteract())
            return;

        // Load Parallel World from SceneLoader
    }

    private bool HasEnemiesNearby()
    {
        if (_enemyCheckRadius <= 0f)
            return false;

        var hit = Physics2D.OverlapCircle(transform.position, _enemyCheckRadius, _enemyLayerMask);
        return hit != null && hit.transform != transform;
    }
}
