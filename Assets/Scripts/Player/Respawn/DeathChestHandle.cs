using UnityEngine;

public sealed class DeathChestHandle
{
    public string DeathChestId { get; }
    public Vector3 WorldPosition { get; }
    public DeathChestInventory Inventory { get; }
    public TemporaryDeathChestController Controller { get; }

    public DeathChestHandle(string deathChestId, Vector3 worldPosition, DeathChestInventory inventory, TemporaryDeathChestController controller)
    {
        DeathChestId = deathChestId;
        WorldPosition = worldPosition;
        Inventory = inventory;
        Controller = controller;
    }
}
