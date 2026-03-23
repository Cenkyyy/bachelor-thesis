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

        string chestId = Guid.NewGuid().ToString("N");
        var chestHandle = _deathChestFactory.Create(chestId, deathWorldPosition);
        if (chestHandle == null)
            return;

        _deathChestDropper.MoveBackpackItemsToChest(player.Inventory, chestHandle.Inventory.Inventory);

        chestHandle.Controller.Initialize(chestId, chestHandle.Inventory.Inventory, _deathMarkerController, _deathChestRegistry);
        _deathChestRegistry?.Register(chestHandle);
        _deathMarkerController?.AddDeathMarker(chestId, chestHandle.WorldPosition);
    }
}
