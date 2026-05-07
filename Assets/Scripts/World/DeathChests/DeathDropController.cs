using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class DeathDropController : MonoBehaviour
{
    [SerializeField] private DeathChestFactory _deathChestFactory;
    [SerializeField] private DeathChestRegistry _deathChestRegistry;
    [SerializeField] private DeathMarkerController _deathMarkerController;

    private DeathChestDropper _deathChestDropper;

    private void Awake()
    {
        _deathChestDropper = new DeathChestDropper();
    }

    public void CreateDeathChestFromBackpack(Player player, Vector3 deathWorldPosition)
    {
        if (player == null || player.Inventory == null)
            return;

        if (_deathChestFactory == null)
            return;

        string deathChestId = Guid.NewGuid().ToString("N");
        var deathChestHandle = _deathChestFactory.Create(deathChestId, deathWorldPosition);
        if (deathChestHandle == null)
            return;

        _deathChestDropper.MoveBackpackItemsToDeathChest(player.Inventory, deathChestHandle.Inventory.Inventory);

        deathChestHandle.Controller.Initialize(deathChestId, deathChestHandle.Inventory.Inventory, _deathMarkerController, _deathChestRegistry);
        _deathChestRegistry?.Register(deathChestHandle);
        _deathMarkerController?.AddDeathMarker(deathChestId, deathChestHandle.WorldPosition);
    }
}
