using UnityEngine;

public sealed class DeathChestHandle
{
    public string ChestId { get; }
    public Vector3 WorldPosition { get; }
    public ChestInventory Inventory { get; }
    public TemporaryDeathChestController Controller { get; }

    public DeathChestHandle(string chestId, Vector3 worldPosition, ChestInventory inventory, TemporaryDeathChestController controller)
    {
        ChestId = chestId;
        WorldPosition = worldPosition;
        Inventory = inventory;
        Controller = controller;
    }
}
